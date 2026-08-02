-- Showcase test data for TicketSystem
-- Run this against a freshly migrated, otherwise-empty database (only the seeded
-- lookup tables and the 3 default accounts from the migrations — admin, operator,
-- customer — should exist).
-- Designed to look good in three screenshots: the ticket dialog (ticket #11 has a
-- rich conversation + attachment), the ticket list/kanban board, and the dashboard
-- (recent activity, ticket overview charts, first response time trend).
--
-- All actors below are dedicated demo content for this script, independent of the
-- 3 seed login accounts, so they don't need to exist or be touched beforehand.
-- All timestamps are relative to now() so the data stays fresh no matter when this
-- script is actually run.

BEGIN;

-- ============================================================================
-- Demo users (independent of the admin / operator / customer seed accounts)
-- ============================================================================

INSERT INTO "AppUser" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "UserTypeId", "CreatedAt", "UpdatedAt", "UpdatedByUserId")
VALUES
    ('00000000-aaaa-4aaa-8aaa-000000000001', 'sofia.marchetti@example.com', 'Sofia', 'Marchetti', 'pbkdf2-sha256$100000$YUFcv4vEuLC/oP0DXytOfw==$qdLZall/krl8eqt8YauYy95IKdDayMHQcXCxjGhg3/0=', 1, now() - interval '52 days', now() - interval '52 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-aaaa-4aaa-8aaa-000000000002', 'daniel.osei@example.com', 'Daniel', 'Osei', 'pbkdf2-sha256$100000$YUFcv4vEuLC/oP0DXytOfw==$qdLZall/krl8eqt8YauYy95IKdDayMHQcXCxjGhg3/0=', 1, now() - interval '40 days', now() - interval '40 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-aaaa-4aaa-8aaa-000000000003', 'priya.nair@example.com', 'Priya', 'Nair', 'pbkdf2-sha256$100000$YUFcv4vEuLC/oP0DXytOfw==$qdLZall/krl8eqt8YauYy95IKdDayMHQcXCxjGhg3/0=', 1, now() - interval '25 days', now() - interval '25 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-aaaa-4aaa-8aaa-000000000004', 'liam.fitzgerald@example.com', 'Liam', 'Fitzgerald', 'pbkdf2-sha256$100000$YUFcv4vEuLC/oP0DXytOfw==$qdLZall/krl8eqt8YauYy95IKdDayMHQcXCxjGhg3/0=', 1, now() - interval '58 days', now() - interval '58 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-aaaa-4aaa-8aaa-000000000005', 'aisha.rahman@example.com', 'Aisha', 'Rahman', 'pbkdf2-sha256$100000$YUFcv4vEuLC/oP0DXytOfw==$qdLZall/krl8eqt8YauYy95IKdDayMHQcXCxjGhg3/0=', 1, now() - interval '18 days', now() - interval '18 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-bbbb-4bbb-8bbb-000000000001', 'marcus.chen@ticketsystem.local', 'Marcus', 'Chen', 'pbkdf2-sha256$100000$bVeYhVewq6bgqstV7VxaYg==$sCeDxZbSVh9lZcMLZZfExy+4VLvAeZi8EekO/mcuWrM=', 2, now() - interval '60 days', now() - interval '60 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-bbbb-4bbb-8bbb-000000000002', 'elena.vasquez@ticketsystem.local', 'Elena', 'Vasquez', 'pbkdf2-sha256$100000$bVeYhVewq6bgqstV7VxaYg==$sCeDxZbSVh9lZcMLZZfExy+4VLvAeZi8EekO/mcuWrM=', 2, now() - interval '35 days', now() - interval '35 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-bbbb-4bbb-8bbb-000000000003', 'noah.bergstrom@ticketsystem.local', 'Noah', 'Bergström', 'pbkdf2-sha256$100000$bVeYhVewq6bgqstV7VxaYg==$sCeDxZbSVh9lZcMLZZfExy+4VLvAeZi8EekO/mcuWrM=', 2, now() - interval '65 days', now() - interval '65 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d'),
    ('00000000-bbbb-4bbb-8bbb-000000000004', 'grace.okafor@ticketsystem.local', 'Grace', 'Okafor', 'pbkdf2-sha256$100000$bVeYhVewq6bgqstV7VxaYg==$sCeDxZbSVh9lZcMLZZfExy+4VLvAeZi8EekO/mcuWrM=', 2, now() - interval '45 days', now() - interval '45 days', '2d6781ce-863a-4ca4-83c3-c4d521f8e23d');

-- ============================================================================
-- Chat sessions (one per ticket below, referenced by "ChatSessionId")
-- ============================================================================

INSERT INTO "ChatSession" ("Id", "CustomerId", "OperatorId", "StatusId", "CreatedAt", "ClosedAt")
VALUES
    ('00000000-cccc-4ccc-8ccc-000000000001', '00000000-aaaa-4aaa-8aaa-000000000004', '00000000-bbbb-4bbb-8bbb-000000000001', 2, now() - interval '55 days', now() - interval '53 days'),
    ('00000000-cccc-4ccc-8ccc-000000000002', '00000000-aaaa-4aaa-8aaa-000000000001', '00000000-bbbb-4bbb-8bbb-000000000003', 2, now() - interval '50 days', now() - interval '49 days'),
    ('00000000-cccc-4ccc-8ccc-000000000003', '00000000-aaaa-4aaa-8aaa-000000000002', NULL, 1, now() - interval '3 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000004', '00000000-aaaa-4aaa-8aaa-000000000003', '00000000-bbbb-4bbb-8bbb-000000000002', 1, now() - interval '6 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000005', '00000000-aaaa-4aaa-8aaa-000000000005', '00000000-bbbb-4bbb-8bbb-000000000004', 1, now() - interval '12 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000006', '00000000-aaaa-4aaa-8aaa-000000000001', '00000000-bbbb-4bbb-8bbb-000000000001', 2, now() - interval '40 days', now() - interval '39 days'),
    ('00000000-cccc-4ccc-8ccc-000000000007', '00000000-aaaa-4aaa-8aaa-000000000002', '00000000-bbbb-4bbb-8bbb-000000000003', 1, now() - interval '5 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000008', '00000000-aaaa-4aaa-8aaa-000000000003', NULL, 1, now() - interval '2 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000009', '00000000-aaaa-4aaa-8aaa-000000000004', '00000000-bbbb-4bbb-8bbb-000000000004', 1, now() - interval '20 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000010', '00000000-aaaa-4aaa-8aaa-000000000002', NULL, 1, now() - interval '1 day', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-aaaa-4aaa-8aaa-000000000005', '00000000-bbbb-4bbb-8bbb-000000000001', 1, now() - interval '4 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000012', '00000000-aaaa-4aaa-8aaa-000000000001', '00000000-bbbb-4bbb-8bbb-000000000003', 2, now() - interval '33 days', now() - interval '30 days'),
    ('00000000-cccc-4ccc-8ccc-000000000013', '00000000-aaaa-4aaa-8aaa-000000000003', '00000000-bbbb-4bbb-8bbb-000000000002', 1, now() - interval '15 days', NULL),
    ('00000000-cccc-4ccc-8ccc-000000000014', '00000000-aaaa-4aaa-8aaa-000000000004', '00000000-bbbb-4bbb-8bbb-000000000004', 1, now() - interval '7 days', NULL);

-- ============================================================================
-- Tickets
-- ============================================================================

INSERT INTO "Ticket" ("Id", "ChatSessionId", "CustomerId", "OperatorId", "Title", "Content", "StatusId", "PriorityId", "CreatedAt", "UpdatedAt", "ClosedAt", "UpdatedByUserId")
VALUES
    ('00000000-dddd-4ddd-8ddd-000000000001', '00000000-cccc-4ccc-8ccc-000000000001', '00000000-aaaa-4aaa-8aaa-000000000004', '00000000-bbbb-4bbb-8bbb-000000000001',
     'Unable to reset password via email link',
     'I clicked "Forgot password" three times today but the reset email never arrives. I checked spam and promotions folders as well. Can someone reset it manually or tell me what''s going wrong?',
     4, 3, now() - interval '55 days', now() - interval '53 days', now() - interval '53 days', '00000000-bbbb-4bbb-8bbb-000000000001'),

    ('00000000-dddd-4ddd-8ddd-000000000002', '00000000-cccc-4ccc-8ccc-000000000002', '00000000-aaaa-4aaa-8aaa-000000000001', '00000000-bbbb-4bbb-8bbb-000000000003',
     'Invoice PDF shows incorrect VAT amount',
     'The invoice for July (INV-2044) shows 25% VAT instead of the 21% rate that applies to our account. Could you reissue a corrected invoice? We need it for our bookkeeping before month end.',
     4, 4, now() - interval '50 days', now() - interval '49 days', now() - interval '49 days', '00000000-bbbb-4bbb-8bbb-000000000003'),

    ('00000000-dddd-4ddd-8ddd-000000000003', '00000000-cccc-4ccc-8ccc-000000000003', '00000000-aaaa-4aaa-8aaa-000000000002', NULL,
     'Feature request: dark mode for mobile app',
     'Loving the product so far! One thing that would really help during late-night shifts is a dark mode option in the mobile app, similar to what the web dashboard already has. Any plans for this?',
     1, 1, now() - interval '3 days', now() - interval '3 days', NULL, '00000000-aaaa-4aaa-8aaa-000000000002'),

    ('00000000-dddd-4ddd-8ddd-000000000004', '00000000-cccc-4ccc-8ccc-000000000004', '00000000-aaaa-4aaa-8aaa-000000000003', '00000000-bbbb-4bbb-8bbb-000000000002',
     'Dashboard charts not loading on Safari',
     'The ticket overview donut charts on the dashboard stay blank when I open the app in Safari on macOS Sonoma. Works fine in Chrome. Console shows no errors, the chart area is just empty white space.',
     2, 3, now() - interval '6 days', now() - interval '5.5 days', NULL, '00000000-bbbb-4bbb-8bbb-000000000002'),

    ('00000000-dddd-4ddd-8ddd-000000000005', '00000000-cccc-4ccc-8ccc-000000000005', '00000000-aaaa-4aaa-8aaa-000000000005', '00000000-bbbb-4bbb-8bbb-000000000004',
     'How do I export my ticket history to CSV?',
     'We need to archive our closed tickets from last quarter for an internal audit. Is there a way to export the ticket list to CSV or Excel, or do we need to do this manually one by one?',
     3, 2, now() - interval '12 days', now() - interval '10 days', NULL, '00000000-bbbb-4bbb-8bbb-000000000004'),

    ('00000000-dddd-4ddd-8ddd-000000000006', '00000000-cccc-4ccc-8ccc-000000000006', '00000000-aaaa-4aaa-8aaa-000000000001', '00000000-bbbb-4bbb-8bbb-000000000001',
     'Account locked after multiple failed logins',
     'My account got locked after I mistyped my password a few times this morning. I''m now getting "Account temporarily locked" on every attempt, even with the correct password. Can you unlock it?',
     4, 4, now() - interval '40 days', now() - interval '39 days', now() - interval '39 days', '00000000-bbbb-4bbb-8bbb-000000000001'),

    ('00000000-dddd-4ddd-8ddd-000000000007', '00000000-cccc-4ccc-8ccc-000000000007', '00000000-aaaa-4aaa-8aaa-000000000002', '00000000-bbbb-4bbb-8bbb-000000000003',
     'Duplicate notification emails for the same ticket',
     'Every time a ticket I''m following gets a new reply, I receive the exact same notification email twice within a few seconds. It''s not a huge issue but it''s cluttering my inbox.',
     2, 2, now() - interval '5 days', now() - interval '4.6 days', NULL, '00000000-bbbb-4bbb-8bbb-000000000003'),

    ('00000000-dddd-4ddd-8ddd-000000000008', '00000000-cccc-4ccc-8ccc-000000000008', '00000000-aaaa-4aaa-8aaa-000000000003', NULL,
     'Attachment upload fails for files over 5MB',
     'I''m trying to attach a screen recording (about 8MB) to a ticket and it fails silently — the upload progress just resets to zero with no error message. Smaller files under 5MB work fine.',
     1, 3, now() - interval '2 days', now() - interval '2 days', NULL, '00000000-aaaa-4aaa-8aaa-000000000003'),

    ('00000000-dddd-4ddd-8ddd-000000000009', '00000000-cccc-4ccc-8ccc-000000000009', '00000000-aaaa-4aaa-8aaa-000000000004', '00000000-bbbb-4bbb-8bbb-000000000004',
     'Billing address update not saving',
     'I updated our billing address in account settings last week but invoices are still going out with the old address. I''ve tried saving the change twice now.',
     3, 2, now() - interval '20 days', now() - interval '18 days', NULL, '00000000-bbbb-4bbb-8bbb-000000000004'),

    ('00000000-dddd-4ddd-8ddd-000000000010', '00000000-cccc-4ccc-8ccc-000000000010', '00000000-aaaa-4aaa-8aaa-000000000002', NULL,
     'API rate limit unclear in documentation',
     'We''re building an integration against the tickets API and keep hitting 429 responses, but the docs don''t state what the actual rate limit is. Could you clarify the limits per endpoint?',
     1, 1, now() - interval '1 day', now() - interval '1 day', NULL, '00000000-aaaa-4aaa-8aaa-000000000002'),

    ('00000000-dddd-4ddd-8ddd-000000000011', '00000000-cccc-4ccc-8ccc-000000000011', '00000000-aaaa-4aaa-8aaa-000000000005', '00000000-bbbb-4bbb-8bbb-000000000001',
     'Two-factor authentication codes not arriving via SMS',
     'Since yesterday I haven''t been receiving the SMS codes needed to log in with two-factor authentication enabled. I''ve tried requesting a new code five times and restarted my phone. This is currently blocking me from accessing my account at all.',
     2, 4, now() - interval '4 days', now() - interval '2.4 hours', NULL, '00000000-bbbb-4bbb-8bbb-000000000001'),

    ('00000000-dddd-4ddd-8ddd-000000000012', '00000000-cccc-4ccc-8ccc-000000000012', '00000000-aaaa-4aaa-8aaa-000000000001', '00000000-bbbb-4bbb-8bbb-000000000003',
     'Cannot download previous invoices',
     'The download buttons next to our invoices from May and June return a blank page instead of the PDF. Invoices from this month download fine.',
     4, 2, now() - interval '33 days', now() - interval '30 days', now() - interval '30 days', '00000000-bbbb-4bbb-8bbb-000000000003'),

    ('00000000-dddd-4ddd-8ddd-000000000013', '00000000-cccc-4ccc-8ccc-000000000013', '00000000-aaaa-4aaa-8aaa-000000000003', '00000000-bbbb-4bbb-8bbb-000000000002',
     'Request to merge two customer accounts',
     'We accidentally created two separate accounts for the same company (one under our old email domain, one under the new one). Could you help us merge them so we have a single ticket history?',
     3, 1, now() - interval '15 days', now() - interval '14 days', NULL, '00000000-bbbb-4bbb-8bbb-000000000002'),

    ('00000000-dddd-4ddd-8ddd-000000000014', '00000000-cccc-4ccc-8ccc-000000000014', '00000000-aaaa-4aaa-8aaa-000000000004', '00000000-bbbb-4bbb-8bbb-000000000004',
     'Search results missing recently created tickets',
     'Tickets created in the last hour or so don''t show up yet when I search by title, even though they appear fine in the full list. Seems like the search index might be lagging behind.',
     2, 3, now() - interval '7 days', now() - interval '6.5 days', NULL, '00000000-bbbb-4bbb-8bbb-000000000004');

-- ============================================================================
-- Ticket status history (drives the "TicketStatusChanged" dashboard activity)
-- ============================================================================

INSERT INTO "TicketStatusHistory" ("TicketId", "OldStatusId", "NewStatusId", "ChangedByUserId", "ChangedAt")
VALUES
    -- #1 closed
    ('00000000-dddd-4ddd-8ddd-000000000001', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000001', now() - interval '54 days'),
    ('00000000-dddd-4ddd-8ddd-000000000001', 2, 4, '00000000-bbbb-4bbb-8bbb-000000000001', now() - interval '53 days'),
    -- #2 closed
    ('00000000-dddd-4ddd-8ddd-000000000002', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000003', now() - interval '49.7 days'),
    ('00000000-dddd-4ddd-8ddd-000000000002', 2, 4, '00000000-bbbb-4bbb-8bbb-000000000003', now() - interval '49 days'),
    -- #4 in progress
    ('00000000-dddd-4ddd-8ddd-000000000004', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000002', now() - interval '5.5 days'),
    -- #5 resolved
    ('00000000-dddd-4ddd-8ddd-000000000005', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000004', now() - interval '11.5 days'),
    ('00000000-dddd-4ddd-8ddd-000000000005', 2, 3, '00000000-bbbb-4bbb-8bbb-000000000004', now() - interval '10 days'),
    -- #6 closed
    ('00000000-dddd-4ddd-8ddd-000000000006', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000001', now() - interval '39.7 days'),
    ('00000000-dddd-4ddd-8ddd-000000000006', 2, 4, '00000000-bbbb-4bbb-8bbb-000000000001', now() - interval '39 days'),
    -- #7 in progress
    ('00000000-dddd-4ddd-8ddd-000000000007', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000003', now() - interval '4.6 days'),
    -- #9 resolved
    ('00000000-dddd-4ddd-8ddd-000000000009', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000004', now() - interval '19.5 days'),
    ('00000000-dddd-4ddd-8ddd-000000000009', 2, 3, '00000000-bbbb-4bbb-8bbb-000000000004', now() - interval '18 days'),
    -- #11 in progress
    ('00000000-dddd-4ddd-8ddd-000000000011', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000001', now() - interval '3.8 days'),
    -- #12 closed
    ('00000000-dddd-4ddd-8ddd-000000000012', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000003', now() - interval '32.6 days'),
    ('00000000-dddd-4ddd-8ddd-000000000012', 2, 4, '00000000-bbbb-4bbb-8bbb-000000000003', now() - interval '30 days'),
    -- #13 resolved
    ('00000000-dddd-4ddd-8ddd-000000000013', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000002', now() - interval '14.6 days'),
    ('00000000-dddd-4ddd-8ddd-000000000013', 2, 3, '00000000-bbbb-4bbb-8bbb-000000000002', now() - interval '14 days'),
    -- #14 in progress
    ('00000000-dddd-4ddd-8ddd-000000000014', 1, 2, '00000000-bbbb-4bbb-8bbb-000000000004', now() - interval '6.5 days');

-- ============================================================================
-- Chat messages
-- ============================================================================

INSERT INTO "Message" ("ChatSessionId", "TicketId", "SenderId", "Content", "SentAt")
VALUES
    -- #1
    ('00000000-cccc-4ccc-8ccc-000000000001', '00000000-dddd-4ddd-8ddd-000000000001', '00000000-aaaa-4aaa-8aaa-000000000004', 'I clicked "Forgot password" three times today but the reset email never arrives. I checked spam and promotions folders as well. Can someone reset it manually or tell me what''s going wrong?', now() - interval '55 days'),
    ('00000000-cccc-4ccc-8ccc-000000000001', '00000000-dddd-4ddd-8ddd-000000000001', '00000000-bbbb-4bbb-8bbb-000000000001', 'Sorry to hear that! I''ve checked our email service and see a delivery issue on our end for reset links sent yesterday. I''ve manually triggered a new reset link — please check your inbox now.', now() - interval '54.8 days'),
    ('00000000-cccc-4ccc-8ccc-000000000001', '00000000-dddd-4ddd-8ddd-000000000001', '00000000-aaaa-4aaa-8aaa-000000000004', 'Got it, thank you! Password reset successfully.', now() - interval '54.5 days'),

    -- #2
    ('00000000-cccc-4ccc-8ccc-000000000002', '00000000-dddd-4ddd-8ddd-000000000002', '00000000-aaaa-4aaa-8aaa-000000000001', 'The invoice for July (INV-2044) shows 25% VAT instead of the 21% rate that applies to our account. Could you reissue a corrected invoice? We need it for our bookkeeping before month end.', now() - interval '50 days'),
    ('00000000-cccc-4ccc-8ccc-000000000002', '00000000-dddd-4ddd-8ddd-000000000002', '00000000-bbbb-4bbb-8bbb-000000000003', 'Thanks for flagging this, Sofia. You''re right, our VAT lookup used the wrong rate for your region. I''ve corrected it and I''m regenerating the invoice now — you''ll receive INV-2044-R shortly.', now() - interval '49.8 days'),
    ('00000000-cccc-4ccc-8ccc-000000000002', '00000000-dddd-4ddd-8ddd-000000000002', '00000000-aaaa-4aaa-8aaa-000000000001', 'Perfect, received the corrected invoice. Thanks for the quick turnaround!', now() - interval '49.2 days'),

    -- #3
    ('00000000-cccc-4ccc-8ccc-000000000003', '00000000-dddd-4ddd-8ddd-000000000003', '00000000-aaaa-4aaa-8aaa-000000000002', 'Loving the product so far! One thing that would really help during late-night shifts is a dark mode option in the mobile app, similar to what the web dashboard already has. Any plans for this?', now() - interval '3 days'),

    -- #4
    ('00000000-cccc-4ccc-8ccc-000000000004', '00000000-dddd-4ddd-8ddd-000000000004', '00000000-aaaa-4aaa-8aaa-000000000003', 'The ticket overview donut charts on the dashboard stay blank when I open the app in Safari on macOS Sonoma. Works fine in Chrome. Console shows no errors, the chart area is just empty white space.', now() - interval '6 days'),
    ('00000000-cccc-4ccc-8ccc-000000000004', '00000000-dddd-4ddd-8ddd-000000000004', '00000000-bbbb-4bbb-8bbb-000000000002', 'Thanks for the report — can you tell me which Safari version you''re on? I want to check if this is related to a recent Safari update affecting inline SVG rendering.', now() - interval '5.7 days'),
    ('00000000-cccc-4ccc-8ccc-000000000004', '00000000-dddd-4ddd-8ddd-000000000004', '00000000-aaaa-4aaa-8aaa-000000000003', 'Safari 17.5 on macOS Sonoma 14.5. Happy to send a screen recording if that helps.', now() - interval '5.4 days'),

    -- #5
    ('00000000-cccc-4ccc-8ccc-000000000005', '00000000-dddd-4ddd-8ddd-000000000005', '00000000-aaaa-4aaa-8aaa-000000000005', 'We need to archive our closed tickets from last quarter for an internal audit. Is there a way to export the ticket list to CSV or Excel, or do we need to do this manually one by one?', now() - interval '12 days'),
    ('00000000-cccc-4ccc-8ccc-000000000005', '00000000-dddd-4ddd-8ddd-000000000005', '00000000-bbbb-4bbb-8bbb-000000000004', 'A CSV export button isn''t available yet, but I can run a manual export for last quarter''s closed tickets if that helps sooner.', now() - interval '11.5 days'),
    ('00000000-cccc-4ccc-8ccc-000000000005', '00000000-dddd-4ddd-8ddd-000000000005', '00000000-aaaa-4aaa-8aaa-000000000005', 'A manual export for now would be great, thank you!', now() - interval '10.5 days'),
    ('00000000-cccc-4ccc-8ccc-000000000005', '00000000-dddd-4ddd-8ddd-000000000005', '00000000-bbbb-4bbb-8bbb-000000000004', 'Done — sent the CSV to your account email. Let me know if the format works for your audit.', now() - interval '10 days'),

    -- #6
    ('00000000-cccc-4ccc-8ccc-000000000006', '00000000-dddd-4ddd-8ddd-000000000006', '00000000-aaaa-4aaa-8aaa-000000000001', 'My account got locked after I mistyped my password a few times this morning. I''m now getting "Account temporarily locked" on every attempt, even with the correct password. Can you unlock it?', now() - interval '40 days'),
    ('00000000-cccc-4ccc-8ccc-000000000006', '00000000-dddd-4ddd-8ddd-000000000006', '00000000-bbbb-4bbb-8bbb-000000000001', 'I can see the lockout on our end — it clears automatically after 30 minutes, but I''ve gone ahead and unlocked it manually for you right now.', now() - interval '39.8 days'),
    ('00000000-cccc-4ccc-8ccc-000000000006', '00000000-dddd-4ddd-8ddd-000000000006', '00000000-aaaa-4aaa-8aaa-000000000001', 'That worked, I''m back in. Thanks for the fast fix!', now() - interval '39.5 days'),

    -- #7
    ('00000000-cccc-4ccc-8ccc-000000000007', '00000000-dddd-4ddd-8ddd-000000000007', '00000000-aaaa-4aaa-8aaa-000000000002', 'Every time a ticket I''m following gets a new reply, I receive the exact same notification email twice within a few seconds. It''s not a huge issue but it''s cluttering my inbox.', now() - interval '5 days'),
    ('00000000-cccc-4ccc-8ccc-000000000007', '00000000-dddd-4ddd-8ddd-000000000007', '00000000-bbbb-4bbb-8bbb-000000000003', 'Thanks for reporting this — looks like a bug in our notification queue sending duplicates under load. I''ve flagged it to engineering and I''m keeping this ticket open until it''s confirmed fixed.', now() - interval '4.7 days'),

    -- #8
    ('00000000-cccc-4ccc-8ccc-000000000008', '00000000-dddd-4ddd-8ddd-000000000008', '00000000-aaaa-4aaa-8aaa-000000000003', 'I''m trying to attach a screen recording (about 8MB) to a ticket and it fails silently — the upload progress just resets to zero with no error message. Smaller files under 5MB work fine.', now() - interval '2 days'),

    -- #9
    ('00000000-cccc-4ccc-8ccc-000000000009', '00000000-dddd-4ddd-8ddd-000000000009', '00000000-aaaa-4aaa-8aaa-000000000004', 'I updated our billing address in account settings last week but invoices are still going out with the old address. I''ve tried saving the change twice now.', now() - interval '20 days'),
    ('00000000-cccc-4ccc-8ccc-000000000009', '00000000-dddd-4ddd-8ddd-000000000009', '00000000-bbbb-4bbb-8bbb-000000000004', 'I checked your account and the new address wasn''t saving due to a validation issue with the postal code format. I''ve updated it manually on our end — can you confirm it looks correct now?', now() - interval '19.3 days'),
    ('00000000-cccc-4ccc-8ccc-000000000009', '00000000-dddd-4ddd-8ddd-000000000009', '00000000-aaaa-4aaa-8aaa-000000000004', 'Confirmed, looks correct now. Thank you!', now() - interval '18.2 days'),

    -- #10
    ('00000000-cccc-4ccc-8ccc-000000000010', '00000000-dddd-4ddd-8ddd-000000000010', '00000000-aaaa-4aaa-8aaa-000000000002', 'We''re building an integration against the tickets API and keep hitting 429 responses, but the docs don''t state what the actual rate limit is. Could you clarify the limits per endpoint?', now() - interval '1 day'),

    -- #11 — showcase ticket
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-aaaa-4aaa-8aaa-000000000005', 'Since yesterday I haven''t been receiving the SMS codes needed to log in with two-factor authentication enabled. I''ve tried requesting a new code five times and restarted my phone. This is currently blocking me from accessing my account at all.', now() - interval '4 days'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-bbbb-4bbb-8bbb-000000000001', 'Sorry you''re locked out! A few questions to narrow this down: is this on the same phone number as before, and did you recently switch carriers or get a new SIM?', now() - interval '3.9 days'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-aaaa-4aaa-8aaa-000000000005', 'Same number, same phone, no carrier changes. It worked fine yesterday morning.', now() - interval '3.7 days'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-bbbb-4bbb-8bbb-000000000001', 'Thanks. I checked our SMS provider''s status page and there''s a known delivery delay affecting some carriers in your region since yesterday evening. It should resolve on their end, but as a workaround I can temporarily enable backup email codes for your account — want me to do that?', now() - interval '3.5 days'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-aaaa-4aaa-8aaa-000000000005', 'Yes please, that would really help in the meantime.', now() - interval '3.4 days'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-bbbb-4bbb-8bbb-000000000001', 'Done! You should now see an "Email code" option on the login screen. Attaching the delivery log from our SMS provider for reference, in case it''s useful.', now() - interval '3.3 days'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-aaaa-4aaa-8aaa-000000000005', 'Email codes are working, thank you! Any update on when SMS will be back?', now() - interval '7 hours'),
    ('00000000-cccc-4ccc-8ccc-000000000011', '00000000-dddd-4ddd-8ddd-000000000011', '00000000-bbbb-4bbb-8bbb-000000000001', 'Just checked again — the provider marked the incident as resolved 30 minutes ago. Could you try an SMS code once more and let me know if it comes through?', now() - interval '2.4 hours'),

    -- #12
    ('00000000-cccc-4ccc-8ccc-000000000012', '00000000-dddd-4ddd-8ddd-000000000012', '00000000-aaaa-4aaa-8aaa-000000000001', 'The download buttons next to our invoices from May and June return a blank page instead of the PDF. Invoices from this month download fine.', now() - interval '33 days'),
    ('00000000-cccc-4ccc-8ccc-000000000012', '00000000-dddd-4ddd-8ddd-000000000012', '00000000-bbbb-4bbb-8bbb-000000000003', 'I can reproduce this — looks like invoices older than 30 days are hitting an expired storage link. I''ve regenerated the download links for May and June, should work now.', now() - interval '32.5 days'),
    ('00000000-cccc-4ccc-8ccc-000000000012', '00000000-dddd-4ddd-8ddd-000000000012', '00000000-aaaa-4aaa-8aaa-000000000001', 'Both download fine now, thank you!', now() - interval '31 days'),

    -- #13
    ('00000000-cccc-4ccc-8ccc-000000000013', '00000000-dddd-4ddd-8ddd-000000000013', '00000000-aaaa-4aaa-8aaa-000000000003', 'We accidentally created two separate accounts for the same company (one under our old email domain, one under the new one). Could you help us merge them so we have a single ticket history?', now() - interval '15 days'),
    ('00000000-cccc-4ccc-8ccc-000000000013', '00000000-dddd-4ddd-8ddd-000000000013', '00000000-bbbb-4bbb-8bbb-000000000002', 'I can merge these for you — to confirm, you''d like everything moved under the newer email domain account and the old one deactivated?', now() - interval '14.5 days'),
    ('00000000-cccc-4ccc-8ccc-000000000013', '00000000-dddd-4ddd-8ddd-000000000013', '00000000-aaaa-4aaa-8aaa-000000000003', 'Yes exactly, that''s correct.', now() - interval '14.2 days'),
    ('00000000-cccc-4ccc-8ccc-000000000013', '00000000-dddd-4ddd-8ddd-000000000013', '00000000-bbbb-4bbb-8bbb-000000000002', 'All done, your ticket history is now unified under the new account.', now() - interval '14 days'),

    -- #14
    ('00000000-cccc-4ccc-8ccc-000000000014', '00000000-dddd-4ddd-8ddd-000000000014', '00000000-aaaa-4aaa-8aaa-000000000004', 'Tickets created in the last hour or so don''t show up yet when I search by title, even though they appear fine in the full list. Seems like the search index might be lagging behind.', now() - interval '7 days'),
    ('00000000-cccc-4ccc-8ccc-000000000014', '00000000-dddd-4ddd-8ddd-000000000014', '00000000-bbbb-4bbb-8bbb-000000000004', 'Thanks, I can confirm the search index has about a 15-20 minute delay for newly created tickets. I''ve asked engineering if this can be reduced — will update this ticket once I hear back.', now() - interval '6.6 days');

-- ============================================================================
-- Media file attachment (showcase ticket #11)
-- ============================================================================

INSERT INTO "MediaFile" ("ChatSessionId", "UploadedByUserId", "Name", "Extension", "ContentType", "SizeBytes", "Content", "CreatedAt")
VALUES (
    '00000000-cccc-4ccc-8ccc-000000000011',
    '00000000-bbbb-4bbb-8bbb-000000000001',
    'sms-delivery-log',
    '.txt',
    'text/plain',
    octet_length(convert_to('2026-08-02 14:02 UTC  carrier=regional-mobile  status=delayed  reason=upstream_congestion' || E'\n' || '2026-08-02 14:03 UTC  carrier=regional-mobile  status=delayed  reason=upstream_congestion' || E'\n' || '2026-08-02 18:47 UTC  carrier=regional-mobile  status=resolved   reason=incident_cleared', 'UTF8')),
    convert_to('2026-08-02 14:02 UTC  carrier=regional-mobile  status=delayed  reason=upstream_congestion' || E'\n' || '2026-08-02 14:03 UTC  carrier=regional-mobile  status=delayed  reason=upstream_congestion' || E'\n' || '2026-08-02 18:47 UTC  carrier=regional-mobile  status=resolved   reason=incident_cleared', 'UTF8'),
    now() - interval '3.3 days'
);

-- ============================================================================
-- Knowledge base articles
-- ============================================================================

INSERT INTO "Knowledge" ("Title", "Content", "CategoryId", "StatusId", "AuthorId", "CreatedAt", "UpdatedAt", "PublishedAt")
VALUES
    ('How to reset your password',
     'If you''ve forgotten your password, click "Forgot password" on the login screen and enter your account email. You''ll receive a reset link valid for 30 minutes. If the email doesn''t arrive within a few minutes, check your spam folder before contacting support — delivery can occasionally be delayed by a minute or two.',
     (SELECT "Id" FROM "KnowledgeCategory" WHERE "Name" = 'General'), 2,
     '00000000-bbbb-4bbb-8bbb-000000000003',
     now() - interval '49 days', now() - interval '48 days', now() - interval '48 days'),

    ('Understanding two-factor authentication',
     'Two-factor authentication (2FA) adds a second verification step at login, using a code sent via SMS or, if enabled, email. We recommend keeping at least one backup method active in case SMS delivery is delayed by your carrier. You can manage your 2FA methods under Account settings > Security.',
     (SELECT "Id" FROM "KnowledgeCategory" WHERE "Name" = 'Account and access'), 2,
     '00000000-bbbb-4bbb-8bbb-000000000001',
     now() - interval '21 days', now() - interval '20 days', now() - interval '20 days'),

    ('Exporting your ticket history',
     'Administrators and operators can view the full ticket list from the Tickets page, filterable by status and priority. A direct CSV export option is on our roadmap — until then, our support team can generate a manual export for a specific date range on request.',
     (SELECT "Id" FROM "KnowledgeCategory" WHERE "Name" = 'Ticket management'), 2,
     '00000000-bbbb-4bbb-8bbb-000000000004',
     now() - interval '10 days', now() - interval '9 days', now() - interval '9 days'),

    ('Troubleshooting failed file uploads',
     'Attachments are limited to 10MB per file. If an upload fails silently with no error message, it''s most often caused by an unstable connection during the upload rather than the file size itself. Try again on a stable connection, or compress the file before attaching it.',
     (SELECT "Id" FROM "KnowledgeCategory" WHERE "Name" = 'Troubleshooting'), 2,
     '00000000-bbbb-4bbb-8bbb-000000000002',
     now() - interval '7 days', now() - interval '6 days', now() - interval '6 days'),

    ('Merging duplicate customer accounts',
     'Draft notes: outline the process for merging two customer accounts under one email domain, including what happens to existing ticket history and chat sessions. Needs review before publishing.',
     (SELECT "Id" FROM "KnowledgeCategory" WHERE "Name" = 'Account and access'), 1,
     '00000000-bbbb-4bbb-8bbb-000000000003',
     now() - interval '2 days', now() - interval '2 days', NULL);

COMMIT;
