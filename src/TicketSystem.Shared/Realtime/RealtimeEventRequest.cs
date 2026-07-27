namespace TicketSystem.Shared.Realtime;

public sealed record RealtimeEventRequest(string EventName, Guid? EntityId = null, Guid? TicketId = null, Guid? ChatSessionId = null, Guid? CustomerId = null);
