using System.Text;

namespace TicketSystem.Api.Features.UpdateDatabase;

public static class DatabaseMigrations
{
    public static int DatabaseVersion { get; } = 2;

    public static IReadOnlyList<DatabaseMigration> All { get; } =
    [
        new(1, CreateInitialTables()),
        new(2, CreateKnowledgeTable())
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
}
