# Ticket System

This repository contains a ticket system with PostgreSQL persistence, a Blazor Web application, an HTTP API, and a standalone SignalR host. The database schema is managed by ordered application migrations. PostgreSQL table and column identifiers use quoted PascalCase names that match their C# counterparts.

## Files

- `TicketSystem.sln` - Visual Studio solution file for the application.
- `src/TicketSystem.Api` - ASP.NET Core HTTP API that applies pending database migrations during startup.
- `src/TicketSystem.DAL/Database/DatabaseMigrations.cs` - ordered PostgreSQL schema migrations.
- `schema.sql` - standalone mirror of the current schema after all migrations for creating or inspecting an empty database.
- `src/TicketSystem.Shared` - DTOs and enums shared by the API and Web projects.
- `global.json` - pins the local SDK to .NET 8 for Visual Studio 2022 compatibility.
- `src/TicketSystem.Web` - Blazor Web App project configured for MudBlazor and server interactivity.
- `src/TicketSystem.Realtime` - standalone ASP.NET Core SignalR host used for chat communication.
- `TicketSystem.slnLaunch` - Visual Studio multi-project launch profile for starting Web and Realtime together.
- `deploy/README.md` - Docker configuration, standard startup, Web hot reload, and container operations.

## Application Stack

The generated solution currently targets `net8.0` because this machine has .NET 8 installed and it is the safest option for Visual Studio 2022 compatibility. MudBlazor 9.x supports .NET 8, .NET 9, and .NET 10, so the project can be upgraded later when the target development environment has a compatible SDK installed.

Current setup:

- `TicketSystem.Web` is a Blazor Web App with interactive server components.
- `TicketSystem.Web` references `MudBlazor` version `9.6.0`.
- `TicketSystem.Web` runs as the MudBlazor application.
- `TicketSystem.Realtime` contains the `ChatHub` SignalR hub and the `ChatMessage` message model.
- `TicketSystem.Realtime` maps the SignalR chat endpoint at `/hubs/chat`.
- `TicketSystem.Api` maps the authenticated Ticket notification hub at `/hubs/tickets`.
- SignalR groups are named with the `chat-session:{sessionId}` pattern, matching the database `ChatSession` concept.
- After a Ticket is created, the API broadcasts its ID and connected Ticket pages fetch it through the authorized API before adding it to the Kanban board.

## Visual Studio Launch

Open `TicketSystem.sln` in Visual Studio 2022 and select the `TicketSystem Web + Realtime` launch profile. Starting that profile launches both projects:

- `TicketSystem.Web` on `https://localhost:7097` and `http://localhost:5047`.
- `TicketSystem.Realtime` on `https://localhost:7197` and `http://localhost:5057`.

The SignalR hub URLs are:

```text
https://localhost:7197/hubs/chat
https://localhost:7280/hubs/tickets
```

## Docker

See [`deploy/README.md`](deploy/README.md) for every Docker environment setting and command, including complete stack startup, individual service rebuilds, database reset, and automatic Web reload with `dotnet watch`.

## Data Model

### AppUser

The `AppUser` table stores all customers, operators, and administrators. There is no separate `Customer` table; a customer is an `AppUser` whose `UserTypeId` is `1`.

- `Id` is a `uuid` primary key.
- `Email` is unique.
- `PasswordHash` stores the hashed password. Never store a plain-text password.
- `UserTypeId` identifies whether the user is a customer, operator, or administrator.
- `CreatedAt` and `UpdatedAt` store audit timestamps.
- `UpdatedByUserId` identifies the administrator responsible for the latest change and references `AppUser.Id`.

### Lookup tables

Normalized status, priority, and category values are stored in lookup tables:

- `ChatSessionStatus` contains `Id`, `Code`, and `Name`; its codes are `active` and `closed`.
- `TicketStatus` contains `Id`, `Code`, and `Name`; its codes are `open`, `in_progress`, `resolved`, and `closed`.
- `TicketPriority` contains `Id`, `Code`, `Name`, and `SortOrder`; its codes are `low`, `normal`, `high`, and `urgent`.
- `KnowledgeStatus` contains `Id`, `Code`, and `Name`; its codes are `draft`, `published`, and `archived`.
- `KnowledgeCategory` contains `Id` and a unique `Name`.

### ChatSession

The `ChatSession` table represents a conversation between a customer and an operator.

- `CustomerId` identifies the customer who started the conversation and references `AppUser.Id`.
- `OperatorId` identifies the operator who accepted the conversation and references `AppUser.Id`. It can be `NULL` until the conversation is assigned.
- `Title` is an optional conversation title.
- `StatusId` references `ChatSessionStatus.Id`.
- `CreatedAt` stores the creation time, while `ClosedAt` is set when the conversation is closed.

A chat session is a good boundary for a SignalR room/group. When a customer and operator connect through SignalR, they can be added to a group named, for example, `chat-session:{chatSessionId}`.

### Ticket

The `Ticket` table represents a concrete support request.

- `Id` is a `uuid` primary key.
- `TicketNumber` is an auto-incrementing business number that is useful for displaying tickets to users.
- `ChatSessionId` optionally links the ticket to `ChatSession.Id` after the first message creates a chat session.
- `CustomerId` identifies the customer who opened the ticket and references `AppUser.Id`.
- `OperatorId` identifies the assigned operator and references `AppUser.Id`.
- `Title` and `Content` store the request title and initial problem description.
- `StatusId` references `TicketStatus.Id`.
- `PriorityId` references `TicketPriority.Id`.
- `CreatedAt`, `UpdatedAt`, and nullable `ClosedAt` store lifecycle timestamps.
- `IsDeleted` marks a ticket as soft-deleted without removing its data.
- `UpdatedByUserId` identifies the user responsible for the latest change and references `AppUser.Id`.

In the simple flow, one ticket has one chat session. If multiple conversations are needed for the same ticket later, the relationship can be adjusted so that `ChatSession` contains a `TicketId` reference.

### Message

The `Message` table stores chat history.

- `ChatSessionId` is required because every message belongs to a conversation.
- `TicketId` is optional, but useful when loading all messages for a ticket.
- `SenderId` identifies the customer or operator who sent the message.
- `Content` is the message body.
- `SentAt` stores the send time.

### MessageRead

The `MessageRead` table stores per-user read receipts.

- `MessageId` identifies the message that was read.
- `UserId` identifies the user who read the message.
- `ReadAt` stores the read time.

This is more flexible than a single read flag on `Message`, because the same conversation can be read by a customer, an operator, and an administrator.

### Knowledge

The `Knowledge` table stores knowledge-base articles.

- `Title` and `Content` store the article contents.
- `CategoryId` optionally references `KnowledgeCategory.Id`.
- `StatusId` references `KnowledgeStatus.Id`.
- `AuthorId` references the author in `AppUser.Id`.
- `CreatedAt`, `UpdatedAt`, and nullable `PublishedAt` store lifecycle timestamps.

## Suggested Flow

1. A customer or technician creates a `Ticket` record without a chat session.
2. The first message creates a `ChatSession` and links it to the ticket through `ChatSessionId`.
3. The first message is stored as a `Message` with the same `ChatSessionId` and `TicketId`.
4. SignalR sends messages to the group connected to `ChatSessionId`.
5. Every sent message is stored in `Message`, so the chat history can be loaded from the database.
6. When a customer or operator opens the conversation, the application inserts rows into `MessageRead` for the seen messages.

## Potential UI Mock

The provided reference screens can be used as potential mocks for the operator/admin ticket workspace.

Preferred direction: use the Zoho-style ticket detail workspace as the primary UI inspiration. The queue screens are still useful references for filtering and list behavior, but the main operator experience should prioritize the ticket conversation, ticket status, and quick actions in one focused workspace.

### Detailed Ticket Workspace

Suggested layout ideas from the first mock:

- A left sidebar with navigation for tickets, dashboard, tasks, tags, knowledge base, chat, users, groups, and customers.
- A top bar with a primary `New Ticket` action, ticket search, chat status, office status, help, and sign out.
- Ticket list tabs such as `Open`, `My Groups`, `Subscribed`, `Flagged`, `Closed`, `All`, and `Queue`.
- A ticket grid with columns such as `Number`, `Name`, `Status`, `Severity`, `Type`, `Group`, and `Days Opened`.
- A selected ticket details panel below the grid with type, status, severity, assigned operator, group, product, and resolution metadata.
- A message/content preview area for the selected ticket.

This mock suggests that the main operator screen should prioritize fast ticket filtering, status visibility, and quick access to ticket details without leaving the list view.

### Incident List Workspace

Suggested layout ideas from the second mock:

- A compact top navigation bar with global actions such as home, search, create, notifications, and user profile.
- A left filter panel grouped by company, department, and status.
- Status filters such as `Open`, `Awaiting`, `Closed`, `Resolved`, and `Pending`.
- Department filters such as `Sales`, `Technical Support`, `Billing`, and `Marketing`.
- A main incident/ticket table with columns such as `Incident hash`, `Subject`, `Email`, `Department`, and `Priority`.
- Priority values visually distinguished as `Critical`, `High`, `Medium`, and `Low`.
- Pagination controls above and below the table for larger ticket queues.
- Row-level channel indicators, such as email, phone, or social source icons.

This mock is useful as a simpler queue-focused view where operators need to scan many incidents quickly and filter them by department or status.

### IT Request Board Workspace

Suggested layout ideas from the third mock:

- A lightweight IT request board organized by ticket status sections such as `New requests`, `Working on it`, and `Done`.
- A compact table layout inside each status section with columns such as `Description`, `Created at`, `Priority`, `Assignee`, and `Due date`.
- Color-coded section accents that make each workflow state easy to scan.
- Strong priority badges for values such as `Low`, `Medium`, and `High`.
- Assignee avatars shown directly in the row for quick ownership visibility.
- A minimal left icon rail for primary application navigation.

This mock is useful for a cleaner internal IT workflow where operators mostly need to track status, ownership, priority, and due dates at a glance.

### SysAid Service Records Workspace

Suggested layout ideas from the fourth mock:

- A modern service-record list with top category tabs such as `All`, `Incident`, `Change`, `Problem`, and `Request`.
- A filter row with assignee and status dropdowns, plus a clear filters action.
- A narrow left icon rail for global navigation and quick actions.
- A dense ticket table with columns such as `ID`, `Status`, `Priority`, `Request user`, `SR Type`, `Title`, `Category`, and `Subcategory`.
- Badge-style status values such as `New`, `Open`, `Pending`, and `In Approval Process`.
- Badge-style priority values such as `Normal` and `Low`.
- Category chips that make service areas visible without opening the record.

This mock is useful for a polished admin/operator queue where different service record types need to live in the same table while still being easy to filter.

### Freshservice Ticket Detail Workspace

Suggested layout ideas from the fifth mock:

- A detailed ticket page focused on one active support request.
- A header with ticket breadcrumb, search, and primary ticket actions such as `Edit`, `Reply`, `Associate`, `Discuss`, and `Close`.
- Ticket metadata near the title, including overdue state, requester, reported time, and portal/source context.
- Tabs for related data such as `Details`, `Child Tickets`, `Tasks`, `Assets`, `Associations`, and `Activities`.
- A prominent description panel for the core issue summary.
- A conversation timeline with customer and operator replies.
- Reply, forward, and internal note actions close to the conversation area.
- A right sidebar with status, first response due time, resolution due time, requester info, collaboration tools, responders, and editable properties.

This mock is useful for the single-ticket work view where an operator needs full context, SLA visibility, collaboration, and message history in one place.

### Zoho Ticket Detail Workspace

Suggested layout ideas from the sixth mock:

- A conversation-first ticket detail page with the selected ticket opened in the main workspace.
- A dark top navigation bar with modules such as `Tickets`, `KB`, `Tasks`, `Reports`, `Customers`, `Community`, `Social`, and `Chat`.
- A left ticket timeline/list with previous tickets, dates, requesters, and status indicators.
- A ticket header with ticket number, title, status ribbon, requester metadata, followers, tags, and elapsed time.
- Primary content tabs such as `Conversation`, `Resolution`, `Time Entry`, `Attachment`, `Task`, `Approval`, and `History`.
- A clean conversation thread with customer and operator messages shown chronologically.
- Bottom quick actions such as `Apply Macro`, `Remote Assist`, and `Reopen Ticket`.
- Compact utility actions for chat, channels, contacts, notes, time, and search.

This is the preferred mock because it balances ticket context, chat history, and operator actions without making the screen feel overloaded. The implementation should lean toward this layout for the main ticket detail screen.

## Database migrations

The API applies pending migrations from `DatabaseMigrations.All` during startup and records every applied version in the `DatabaseVersion` table. The root `schema.sql` file mirrors the complete current schema and can also be used to create an empty database manually. Both definitions must remain synchronized. See [`deploy/README.md`](deploy/README.md) for the container startup workflow.
