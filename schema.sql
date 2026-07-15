CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE chat_session_status (
    id smallint PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE,
    name varchar(100) NOT NULL
);

INSERT INTO chat_session_status VALUES
    (1, 'active', 'Active'),
    (2, 'closed', 'Closed');

CREATE TABLE ticket_status (
    id smallint PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE,
    name varchar(100) NOT NULL
);

INSERT INTO ticket_status VALUES
    (1, 'open', 'Open'),
    (2, 'in_progress', 'In progress'),
    (3, 'resolved', 'Resolved'),
    (4, 'closed', 'Closed');

CREATE TABLE ticket_priority (
    id smallint PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE,
    name varchar(100) NOT NULL,
    sort_order smallint NOT NULL UNIQUE
);

INSERT INTO ticket_priority VALUES
    (1, 'low', 'Low', 1),
    (2, 'normal', 'Normal', 2),
    (3, 'high', 'High', 3),
    (4, 'urgent', 'Urgent', 4);

CREATE TABLE knowledge_status (
    id smallint PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE,
    name varchar(100) NOT NULL
);

INSERT INTO knowledge_status VALUES
    (1, 'draft', 'Draft'),
    (2, 'published', 'Published'),
    (3, 'archived', 'Archived');

CREATE TABLE knowledge_category (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL UNIQUE
);

CREATE TABLE app_user (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email varchar(320) NOT NULL UNIQUE,
    password_hash text NOT NULL,
    user_type_id integer NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE customer (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    app_user_id uuid NOT NULL UNIQUE REFERENCES app_user(id) ON DELETE CASCADE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE chat_session (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL REFERENCES customer(id) ON DELETE CASCADE,
    operator_id uuid REFERENCES app_user(id) ON DELETE SET NULL,
    title varchar(200),
    status_id smallint NOT NULL DEFAULT 1 REFERENCES chat_session_status(id) ON DELETE RESTRICT,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    closed_at timestamp with time zone
);

CREATE TABLE ticket (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_number bigserial UNIQUE,
    chat_session_id uuid NOT NULL REFERENCES chat_session(id) ON DELETE CASCADE,
    customer_id uuid NOT NULL REFERENCES customer(id) ON DELETE CASCADE,
    operator_id uuid REFERENCES app_user(id) ON DELETE SET NULL,
    title varchar(200) NOT NULL,
    content text NOT NULL,
    status_id smallint NOT NULL DEFAULT 1 REFERENCES ticket_status(id) ON DELETE RESTRICT,
    priority_id smallint NOT NULL DEFAULT 2 REFERENCES ticket_priority(id) ON DELETE RESTRICT,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    closed_at timestamp with time zone
);

CREATE TABLE message (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    chat_session_id uuid NOT NULL REFERENCES chat_session(id) ON DELETE CASCADE,
    ticket_id uuid REFERENCES ticket(id) ON DELETE CASCADE,
    sender_id uuid NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
    content text NOT NULL,
    sent_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE message_read (
    message_id uuid NOT NULL REFERENCES message(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
    read_at timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (message_id, user_id)
);

CREATE TABLE knowledge (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    title varchar(200) NOT NULL,
    content text NOT NULL,
    category_id uuid REFERENCES knowledge_category(id) ON DELETE SET NULL,
    status_id smallint NOT NULL DEFAULT 1 REFERENCES knowledge_status(id) ON DELETE RESTRICT,
    author_id uuid NOT NULL REFERENCES app_user(id) ON DELETE RESTRICT,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    published_at timestamp with time zone
);

CREATE INDEX ix_chat_session_customer_id ON chat_session(customer_id);

CREATE INDEX ix_chat_session_operator_id ON chat_session(operator_id);

CREATE INDEX ix_ticket_chat_session_id ON ticket(chat_session_id);

CREATE INDEX ix_ticket_customer_id ON ticket(customer_id);

CREATE INDEX ix_ticket_operator_id ON ticket(operator_id);

CREATE INDEX ix_ticket_status_id ON ticket(status_id);

CREATE INDEX ix_ticket_priority_id ON ticket(priority_id);

CREATE INDEX ix_message_chat_session_id_sent_at ON message(chat_session_id, sent_at);

CREATE INDEX ix_message_ticket_id_sent_at ON message(ticket_id, sent_at);

CREATE INDEX ix_message_sender_id ON message(sender_id);

CREATE INDEX ix_message_read_user_id ON message_read(user_id);

CREATE INDEX ix_knowledge_status_id ON knowledge(status_id);

CREATE INDEX ix_knowledge_category_id ON knowledge(category_id);

CREATE INDEX ix_knowledge_author_id ON knowledge(author_id);
