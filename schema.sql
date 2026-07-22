CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE SEQUENCE "TicketNumberSequence" AS bigint START WITH 1 INCREMENT BY 1;

CREATE TABLE "ChatSessionStatus" (
    "Id" smallint NOT NULL,
    "Code" varchar(30) NOT NULL,
    "Name" varchar(100) NOT NULL,
    CONSTRAINT "PK_ChatSessionStatus" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_ChatSessionStatusCode" UNIQUE ("Code")
);

CREATE TABLE "TicketStatus" (
    "Id" smallint NOT NULL,
    "Code" varchar(30) NOT NULL,
    "Name" varchar(100) NOT NULL,
    CONSTRAINT "PK_TicketStatus" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_TicketStatusCode" UNIQUE ("Code")
);

CREATE TABLE "TicketPriority" (
    "Id" smallint NOT NULL,
    "Name" varchar(30) NOT NULL,
    "DisplayName" varchar(100) NOT NULL,
    "Impact" smallint NOT NULL,
    CONSTRAINT "PK_TicketPriority" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_TicketPriorityName" UNIQUE ("Name"),
    CONSTRAINT "UQ_TicketPriorityImpact" UNIQUE ("Impact")
);

CREATE TABLE "KnowledgeStatus" (
    "Id" smallint NOT NULL,
    "Code" varchar(30) NOT NULL,
    "Name" varchar(100) NOT NULL,
    CONSTRAINT "PK_KnowledgeStatus" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_KnowledgeStatusCode" UNIQUE ("Code")
);

CREATE TABLE "KnowledgeCategory" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Name" varchar(100) NOT NULL,
    CONSTRAINT "PK_KnowledgeCategory" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_KnowledgeCategoryName" UNIQUE ("Name")
);

CREATE TABLE "AppUser" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Email" varchar(320) NOT NULL,
    "PasswordHash" text NOT NULL,
    "UserTypeId" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedByUserId" uuid NOT NULL,
    CONSTRAINT "PK_AppUser" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_AppUserEmail" UNIQUE ("Email")
);

CREATE TABLE "ChatSession" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "CustomerId" uuid NOT NULL,
    "OperatorId" uuid,
    "Title" varchar(200),
    "StatusId" smallint NOT NULL DEFAULT 1,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ClosedAt" timestamp with time zone,
    CONSTRAINT "PK_ChatSession" PRIMARY KEY ("Id")
);

CREATE TABLE "Ticket" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "TicketNumber" bigint NOT NULL DEFAULT nextval('"TicketNumberSequence"'),
    "ChatSessionId" uuid,
    "CustomerId" uuid NOT NULL,
    "OperatorId" uuid,
    "Title" varchar(200) NOT NULL,
    "Content" text NOT NULL,
    "StatusId" smallint NOT NULL DEFAULT 1,
    "PriorityId" smallint NOT NULL DEFAULT 2,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ClosedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "UpdatedByUserId" uuid NOT NULL,
    CONSTRAINT "PK_Ticket" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_TicketTicketNumber" UNIQUE ("TicketNumber")
);

CREATE TABLE "Message" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ChatSessionId" uuid NOT NULL,
    "TicketId" uuid,
    "SenderId" uuid NOT NULL,
    "Content" text NOT NULL,
    "SentAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_Message" PRIMARY KEY ("Id")
);

CREATE TABLE "MessageRead" (
    "MessageId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ReadAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_MessageRead" PRIMARY KEY ("MessageId", "UserId")
);

CREATE TABLE "Knowledge" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Title" varchar(200) NOT NULL,
    "Content" text NOT NULL,
    "CategoryId" uuid,
    "StatusId" smallint NOT NULL DEFAULT 1,
    "AuthorId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "PublishedAt" timestamp with time zone,
    CONSTRAINT "PK_Knowledge" PRIMARY KEY ("Id")
);

ALTER TABLE "AppUser"
ADD CONSTRAINT "FK_AppUserUpdatedByUserIdAppUser"
FOREIGN KEY ("UpdatedByUserId") REFERENCES "AppUser" ("Id") ON DELETE RESTRICT;

ALTER TABLE "ChatSession"
ADD CONSTRAINT "FK_ChatSessionCustomerIdAppUser"
FOREIGN KEY ("CustomerId") REFERENCES "AppUser" ("Id") ON DELETE CASCADE;

ALTER TABLE "ChatSession"
ADD CONSTRAINT "FK_ChatSessionOperatorIdAppUser"
FOREIGN KEY ("OperatorId") REFERENCES "AppUser" ("Id") ON DELETE SET NULL;

ALTER TABLE "ChatSession"
ADD CONSTRAINT "FK_ChatSessionStatusIdChatSessionStatus"
FOREIGN KEY ("StatusId") REFERENCES "ChatSessionStatus" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Ticket"
ADD CONSTRAINT "FK_TicketChatSessionIdChatSession"
FOREIGN KEY ("ChatSessionId") REFERENCES "ChatSession" ("Id") ON DELETE CASCADE;

ALTER TABLE "Ticket"
ADD CONSTRAINT "FK_TicketCustomerIdAppUser"
FOREIGN KEY ("CustomerId") REFERENCES "AppUser" ("Id") ON DELETE CASCADE;

ALTER TABLE "Ticket"
ADD CONSTRAINT "FK_TicketOperatorIdAppUser"
FOREIGN KEY ("OperatorId") REFERENCES "AppUser" ("Id") ON DELETE SET NULL;

ALTER TABLE "Ticket"
ADD CONSTRAINT "FK_TicketStatusIdTicketStatus"
FOREIGN KEY ("StatusId") REFERENCES "TicketStatus" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Ticket"
ADD CONSTRAINT "FK_TicketPriorityIdTicketPriority"
FOREIGN KEY ("PriorityId") REFERENCES "TicketPriority" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Ticket"
ADD CONSTRAINT "FK_TicketUpdatedByUserIdAppUser"
FOREIGN KEY ("UpdatedByUserId") REFERENCES "AppUser" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Message"
ADD CONSTRAINT "FK_MessageChatSessionIdChatSession"
FOREIGN KEY ("ChatSessionId") REFERENCES "ChatSession" ("Id") ON DELETE CASCADE;

ALTER TABLE "Message"
ADD CONSTRAINT "FK_MessageTicketIdTicket"
FOREIGN KEY ("TicketId") REFERENCES "Ticket" ("Id") ON DELETE CASCADE;

ALTER TABLE "Message"
ADD CONSTRAINT "FK_MessageSenderIdAppUser"
FOREIGN KEY ("SenderId") REFERENCES "AppUser" ("Id") ON DELETE CASCADE;

ALTER TABLE "MessageRead"
ADD CONSTRAINT "FK_MessageReadMessageIdMessage"
FOREIGN KEY ("MessageId") REFERENCES "Message" ("Id") ON DELETE CASCADE;

ALTER TABLE "MessageRead"
ADD CONSTRAINT "FK_MessageReadUserIdAppUser"
FOREIGN KEY ("UserId") REFERENCES "AppUser" ("Id") ON DELETE CASCADE;

ALTER TABLE "Knowledge"
ADD CONSTRAINT "FK_KnowledgeCategoryIdKnowledgeCategory"
FOREIGN KEY ("CategoryId") REFERENCES "KnowledgeCategory" ("Id") ON DELETE SET NULL;

ALTER TABLE "Knowledge"
ADD CONSTRAINT "FK_KnowledgeStatusIdKnowledgeStatus"
FOREIGN KEY ("StatusId") REFERENCES "KnowledgeStatus" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Knowledge"
ADD CONSTRAINT "FK_KnowledgeAuthorIdAppUser"
FOREIGN KEY ("AuthorId") REFERENCES "AppUser" ("Id") ON DELETE RESTRICT;

ALTER SEQUENCE "TicketNumberSequence" OWNED BY "Ticket"."TicketNumber";

CREATE INDEX "IXAppUserUserTypeId" ON "AppUser" ("UserTypeId");

CREATE INDEX "IXAppUserUpdatedByUserId" ON "AppUser" ("UpdatedByUserId");

CREATE INDEX "IXChatSessionCustomerId" ON "ChatSession" ("CustomerId");

CREATE INDEX "IXChatSessionOperatorId" ON "ChatSession" ("OperatorId");

CREATE INDEX "IXTicketChatSessionId" ON "Ticket" ("ChatSessionId");

CREATE INDEX "IXTicketCustomerId" ON "Ticket" ("CustomerId");

CREATE INDEX "IXTicketOperatorId" ON "Ticket" ("OperatorId");

CREATE INDEX "IXTicketStatusId" ON "Ticket" ("StatusId");

CREATE INDEX "IXTicketPriorityId" ON "Ticket" ("PriorityId");

CREATE INDEX "IXTicketUpdatedByUserId" ON "Ticket" ("UpdatedByUserId");

CREATE INDEX "IXMessageChatSessionIdSentAt" ON "Message" ("ChatSessionId", "SentAt");

CREATE INDEX "IXMessageTicketIdSentAt" ON "Message" ("TicketId", "SentAt");

CREATE INDEX "IXMessageSenderId" ON "Message" ("SenderId");

CREATE INDEX "IXMessageReadUserId" ON "MessageRead" ("UserId");

CREATE INDEX "IXKnowledgeStatusId" ON "Knowledge" ("StatusId");

CREATE INDEX "IXKnowledgeCategoryId" ON "Knowledge" ("CategoryId");

CREATE INDEX "IXKnowledgeAuthorId" ON "Knowledge" ("AuthorId");

INSERT INTO "ChatSessionStatus" ("Id", "Code", "Name")
VALUES
    (1, 'active', 'Active'),
    (2, 'closed', 'Closed');

INSERT INTO "TicketStatus" ("Id", "Code", "Name")
VALUES
    (1, 'open', 'Open'),
    (2, 'in_progress', 'In progress'),
    (3, 'resolved', 'Resolved'),
    (4, 'closed', 'Closed');

INSERT INTO "TicketPriority" ("Id", "Name", "DisplayName", "Impact")
VALUES
    (1, 'low', 'Low', 1),
    (2, 'normal', 'Normal', 2),
    (3, 'high', 'High', 3),
    (4, 'urgent', 'Urgent', 4);

INSERT INTO "KnowledgeStatus" ("Id", "Code", "Name")
VALUES
    (1, 'draft', 'Draft'),
    (2, 'published', 'Published'),
    (3, 'archived', 'Archived');

INSERT INTO "AppUser" ("Id", "Email", "PasswordHash", "UserTypeId", "CreatedAt", "UpdatedAt", "UpdatedByUserId")
VALUES (
    '2d6781ce-863a-4ca4-83c3-c4d521f8e23d',
    'admin@ticketsystem.local',
    'pbkdf2-sha256$100000$lgmjqMVW/xj4j8oNTZkmJQ==$FCDoM5xmI0/o5IoxEzoMhClT9sNIazb13MmNk6Ih05s=',
    3,
    now(),
    now(),
    '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'
);

INSERT INTO "AppUser" ("Email", "PasswordHash", "UserTypeId", "UpdatedByUserId")
VALUES
    ('customer1@ticketsystem.local', 'pbkdf2-sha256$100000$YUFcv4vEuLC/oP0DXytOfw==$qdLZall/krl8eqt8YauYy95IKdDayMHQcXCxjGhg3/0=', 1, '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('customer2@ticketsystem.local', 'pbkdf2-sha256$100000$YpD12bK6eZYl72w0OcEeXA==$ejIdWPs6McK0WIkuG5PzK9cw+LXvqCJ76F33XKWfiQ4=', 1, '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('support1@ticketsystem.local', 'pbkdf2-sha256$100000$bVeYhVewq6bgqstV7VxaYg==$sCeDxZbSVh9lZcMLZZfExy+4VLvAeZi8EekO/mcuWrM=', 2, '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('support2@ticketsystem.local', 'pbkdf2-sha256$100000$hGz/qId57Ox8lR2MHCv5Ag==$ca952K+yHQFEDAwalyXwtTmOuuAviwh5y94EPCJLWt4=', 2, '2d6781ce-863a-4ca4-83c3-c4d521f8e23d');
