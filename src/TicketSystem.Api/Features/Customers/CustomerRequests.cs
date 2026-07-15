namespace TicketSystem.Api.Features.Customers;

public sealed record CreateCustomerRequest(string? Email, string Password);

public sealed record UpdateCustomerRequest(string? Email, string? Password);
