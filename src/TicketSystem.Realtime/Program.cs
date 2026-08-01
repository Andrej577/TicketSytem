using TicketSystem.Realtime.Hubs;
using TicketSystem.Realtime.Internal;
using TicketSystem.Realtime.Security;
using TicketSystem.Shared.Realtime;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["https://localhost:7097", "http://localhost:5047"];

var internalKey = builder.Configuration["InternalRealtime:Key"] ?? throw new InvalidOperationException("The 'InternalRealtime:Key' configuration value is required.");

if (internalKey.Length < 32)
{
    throw new InvalidOperationException("InternalRealtime:Key must contain at least 32 characters.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRealtimeAuthentication(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddSingleton(new InternalRealtimeOptions(internalKey));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("WebApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<AppUserHub>(RealtimeHubRoutes.AppUsers);
app.MapHub<ChatHub>(RealtimeHubRoutes.Chat);
app.MapHub<KnowledgeHub>(RealtimeHubRoutes.Knowledge);
app.MapHub<TicketHub>(RealtimeHubRoutes.Tickets);
app.MapInternalRealtimeEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "TicketSystem.Realtime",
    appUserHub = RealtimeHubRoutes.AppUsers,
    chatHub = RealtimeHubRoutes.Chat,
    knowledgeHub = RealtimeHubRoutes.Knowledge,
    ticketHub = RealtimeHubRoutes.Tickets
}));

app.Run();
