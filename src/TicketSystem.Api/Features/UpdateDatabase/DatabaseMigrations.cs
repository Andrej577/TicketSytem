using System.Text;

namespace TicketSystem.Api.Features.UpdateDatabase;

public static class DatabaseMigrations
{
    public static int DatabaseVersion { get; } = 3;

    public static IReadOnlyList<DatabaseMigration> All { get; } =
    [
        new(1, CreateInitialTables()),
        new(2, CreateKnowledgeTable()),
        new(3, NormalizeDatabase())
    ];

    private static string CreateInitialTables()
    {
        var sql = new StringBuilder();

        sql.AppendLine(CreatePgCryptoExtension());
        sql.AppendLine(CreateAppUserTable());
        sql.AppendLine(CreateChatSessionTable());
        sql.AppendLine(CreateTicketTable());
        sql.AppendLine(CreateMessageTable());
        sql.AppendLine(CreateMessageReadTable());
        sql.AppendLine(CreateIndexes());

        return sql.ToString();
    }

    private static string CreatePgCryptoExtension()
    {
        return "CREATE EXTENSION IF NOT EXISTS pgcrypto;";
    }

    private static string CreateAppUserTable()
    {
        return """
            CREATE TABLE IF NOT EXISTS app_user (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                email varchar(320) NOT NULL UNIQUE,
                password_hash text NOT NULL,
                user_type_id integer NOT NULL,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now()
            );
            """;
    }

    private static string CreateChatSessionTable()
    {
        return """
            CREATE TABLE IF NOT EXISTS chat_session (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                customer_id uuid NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                operator_id uuid REFERENCES app_user(id) ON DELETE SET NULL,
                title varchar(200),
                status varchar(30) NOT NULL DEFAULT 'active',
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                closed_at timestamp with time zone,
                CONSTRAINT ck_chat_session_status CHECK (status IN ('active', 'closed'))
            );
            """;
    }

    private static string CreateTicketTable()
    {
        return """
            CREATE TABLE IF NOT EXISTS ticket (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                ticket_number bigserial UNIQUE,
                chat_session_id uuid NOT NULL REFERENCES chat_session(id) ON DELETE CASCADE,
                customer_id uuid NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                operator_id uuid REFERENCES app_user(id) ON DELETE SET NULL,
                title varchar(200) NOT NULL,
                content text NOT NULL,
                status varchar(30) NOT NULL DEFAULT 'open',
                priority varchar(30) NOT NULL DEFAULT 'normal',
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now(),
                closed_at timestamp with time zone,
                CONSTRAINT ck_ticket_status CHECK (status IN ('open', 'in_progress', 'resolved', 'closed')),
                CONSTRAINT ck_ticket_priority CHECK (priority IN ('low', 'normal', 'high', 'urgent'))
            );
            """;
    }

    private static string CreateMessageTable()
    {
        return """
            CREATE TABLE IF NOT EXISTS message (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                chat_session_id uuid NOT NULL REFERENCES chat_session(id) ON DELETE CASCADE,
                ticket_id uuid REFERENCES ticket(id) ON DELETE CASCADE,
                sender_id uuid NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                content text NOT NULL,
                sent_at timestamp with time zone NOT NULL DEFAULT now()
            );
            """;
    }

    private static string CreateMessageReadTable()
    {
        return """
            CREATE TABLE IF NOT EXISTS message_read (
                message_id uuid NOT NULL REFERENCES message(id) ON DELETE CASCADE,
                user_id uuid NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                read_at timestamp with time zone NOT NULL DEFAULT now(),
                PRIMARY KEY (message_id, user_id)
            );
            """;
    }

    private static string CreateIndexes()
    {
        return """
            CREATE INDEX IF NOT EXISTS ix_chat_session_customer_id ON chat_session(customer_id);
            CREATE INDEX IF NOT EXISTS ix_chat_session_operator_id ON chat_session(operator_id);
            CREATE INDEX IF NOT EXISTS ix_ticket_chat_session_id ON ticket(chat_session_id);
            CREATE INDEX IF NOT EXISTS ix_ticket_customer_id ON ticket(customer_id);
            CREATE INDEX IF NOT EXISTS ix_ticket_operator_id ON ticket(operator_id);
            CREATE INDEX IF NOT EXISTS ix_ticket_status ON ticket(status);
            CREATE INDEX IF NOT EXISTS ix_message_chat_session_id_sent_at ON message(chat_session_id, sent_at);
            CREATE INDEX IF NOT EXISTS ix_message_ticket_id_sent_at ON message(ticket_id, sent_at);
            CREATE INDEX IF NOT EXISTS ix_message_sender_id ON message(sender_id);
            CREATE INDEX IF NOT EXISTS ix_message_read_user_id ON message_read(user_id);
            """;
    }

    private static string CreateKnowledgeTable()
    {
        return """
            CREATE TABLE IF NOT EXISTS knowledge (
                id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                title varchar(200) NOT NULL,
                content text NOT NULL,
                category varchar(100),
                status varchar(30) NOT NULL DEFAULT 'draft',
                author_id uuid NOT NULL REFERENCES app_user(id) ON DELETE RESTRICT,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now(),
                published_at timestamp with time zone,
                CONSTRAINT ck_knowledge_status CHECK (status IN ('draft', 'published', 'archived'))
            );

            CREATE INDEX IF NOT EXISTS ix_knowledge_status ON knowledge(status);
            CREATE INDEX IF NOT EXISTS ix_knowledge_category ON knowledge(category);
            CREATE INDEX IF NOT EXISTS ix_knowledge_author_id ON knowledge(author_id);
            """;
    }

    private static string NormalizeDatabase()
    {
        return """
            CREATE TABLE user_type (id integer PRIMARY KEY, code varchar(30) NOT NULL UNIQUE, name varchar(100) NOT NULL);
            INSERT INTO user_type VALUES (1, 'customer', 'Customer'), (2, 'operator', 'Operator'), (3, 'administrator', 'Administrator');
            INSERT INTO user_type (id, code, name)
            SELECT DISTINCT user_type_id, 'legacy_' || user_type_id, 'Legacy ' || user_type_id FROM app_user
            WHERE user_type_id NOT IN (SELECT id FROM user_type);
            ALTER TABLE app_user ADD CONSTRAINT fk_app_user_user_type FOREIGN KEY (user_type_id) REFERENCES user_type(id) ON DELETE RESTRICT;

            CREATE TABLE chat_session_status (id smallint PRIMARY KEY, code varchar(30) NOT NULL UNIQUE, name varchar(100) NOT NULL);
            INSERT INTO chat_session_status VALUES (1, 'active', 'Active'), (2, 'closed', 'Closed');
            ALTER TABLE chat_session ADD COLUMN status_id smallint;
            UPDATE chat_session SET status_id = s.id FROM chat_session_status s WHERE s.code = chat_session.status;
            ALTER TABLE chat_session ALTER COLUMN status_id SET NOT NULL, ALTER COLUMN status_id SET DEFAULT 1;
            ALTER TABLE chat_session ADD CONSTRAINT fk_chat_session_status FOREIGN KEY (status_id) REFERENCES chat_session_status(id) ON DELETE RESTRICT;
            ALTER TABLE chat_session DROP CONSTRAINT ck_chat_session_status, DROP COLUMN status;

            CREATE TABLE ticket_status (id smallint PRIMARY KEY, code varchar(30) NOT NULL UNIQUE, name varchar(100) NOT NULL);
            INSERT INTO ticket_status VALUES (1, 'open', 'Open'), (2, 'in_progress', 'In progress'), (3, 'resolved', 'Resolved'), (4, 'closed', 'Closed');
            CREATE TABLE ticket_priority (id smallint PRIMARY KEY, code varchar(30) NOT NULL UNIQUE, name varchar(100) NOT NULL, sort_order smallint NOT NULL UNIQUE);
            INSERT INTO ticket_priority VALUES (1, 'low', 'Low', 1), (2, 'normal', 'Normal', 2), (3, 'high', 'High', 3), (4, 'urgent', 'Urgent', 4);
            ALTER TABLE ticket ADD COLUMN status_id smallint, ADD COLUMN priority_id smallint;
            UPDATE ticket SET status_id = s.id FROM ticket_status s WHERE s.code = ticket.status;
            UPDATE ticket SET priority_id = p.id FROM ticket_priority p WHERE p.code = ticket.priority;
            ALTER TABLE ticket ALTER COLUMN status_id SET NOT NULL, ALTER COLUMN status_id SET DEFAULT 1, ALTER COLUMN priority_id SET NOT NULL, ALTER COLUMN priority_id SET DEFAULT 2;
            ALTER TABLE ticket ADD CONSTRAINT fk_ticket_status FOREIGN KEY (status_id) REFERENCES ticket_status(id) ON DELETE RESTRICT;
            ALTER TABLE ticket ADD CONSTRAINT fk_ticket_priority FOREIGN KEY (priority_id) REFERENCES ticket_priority(id) ON DELETE RESTRICT;
            DROP INDEX ix_ticket_status;
            ALTER TABLE ticket DROP CONSTRAINT ck_ticket_status, DROP CONSTRAINT ck_ticket_priority, DROP COLUMN status, DROP COLUMN priority;
            CREATE INDEX ix_ticket_status_id ON ticket(status_id);
            CREATE INDEX ix_ticket_priority_id ON ticket(priority_id);

            CREATE TABLE knowledge_status (id smallint PRIMARY KEY, code varchar(30) NOT NULL UNIQUE, name varchar(100) NOT NULL);
            INSERT INTO knowledge_status VALUES (1, 'draft', 'Draft'), (2, 'published', 'Published'), (3, 'archived', 'Archived');
            CREATE TABLE knowledge_category (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name varchar(100) NOT NULL UNIQUE);
            INSERT INTO knowledge_category (name) SELECT DISTINCT category FROM knowledge WHERE category IS NOT NULL AND btrim(category) <> '';
            ALTER TABLE knowledge ADD COLUMN status_id smallint, ADD COLUMN category_id uuid;
            UPDATE knowledge SET status_id = s.id FROM knowledge_status s WHERE s.code = knowledge.status;
            UPDATE knowledge SET category_id = c.id FROM knowledge_category c WHERE c.name = knowledge.category;
            ALTER TABLE knowledge ALTER COLUMN status_id SET NOT NULL, ALTER COLUMN status_id SET DEFAULT 1;
            ALTER TABLE knowledge ADD CONSTRAINT fk_knowledge_status FOREIGN KEY (status_id) REFERENCES knowledge_status(id) ON DELETE RESTRICT;
            ALTER TABLE knowledge ADD CONSTRAINT fk_knowledge_category FOREIGN KEY (category_id) REFERENCES knowledge_category(id) ON DELETE SET NULL;
            DROP INDEX ix_knowledge_status;
            DROP INDEX ix_knowledge_category;
            ALTER TABLE knowledge DROP CONSTRAINT ck_knowledge_status, DROP COLUMN status, DROP COLUMN category;
            CREATE INDEX ix_knowledge_status_id ON knowledge(status_id);
            CREATE INDEX ix_knowledge_category_id ON knowledge(category_id);
            """;
    }
}
