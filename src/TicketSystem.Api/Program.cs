using Npgsql;
using TicketSystem.Api.Features.Authentication;
using TicketSystem.Api.Features.AppUsers;
using TicketSystem.Api.Features.Knowledge;
using TicketSystem.Api.Features.Tickets;
using TicketSystem.Api.Features.UpdateDatabase;
using TicketSystem.DAL.AppUsers;
using TicketSystem.DAL.Configuration;
using TicketSystem.DAL.Knowledge;
using TicketSystem.DAL.Tickets;

var builder = WebApplication.CreateBuilder(args);

DapperConfiguration.Configure();

builder.Services.AddControllers();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddDatabaseUpdater();
builder.Services.AddSignalR();
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "The 'ConnectionStrings:DefaultConnection' configuration value is required.");

    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddScoped<AppUserDAL>();
builder.Services.AddScoped<AppUserRealtimeNotifier>();
builder.Services.AddScoped<KnowledgeDAL>();
builder.Services.AddScoped<KnowledgeRealtimeNotifier>();
builder.Services.AddScoped<TicketDAL>();
builder.Services.AddScoped<TicketRealtimeNotifier>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapAuthenticationEndpoints();
app.MapAppUserEndpoints();
app.MapKnowledgeEndpoints();
app.MapTicketEndpoints();
app.MapHub<AppUserHub>("/hubs/app-users");
app.MapHub<KnowledgeHub>("/hubs/knowledge");
app.MapHub<TicketHub>("/hubs/tickets");

app.Run();
