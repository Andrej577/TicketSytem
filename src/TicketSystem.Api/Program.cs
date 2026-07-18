using Npgsql;
using TicketSystem.Api.Features.AppUsers;
using TicketSystem.Api.Features.Tickets;
using TicketSystem.Api.Features.UpdateDatabase;
using TicketSystem.DAL.AppUsers;
using TicketSystem.DAL.Configuration;
using TicketSystem.DAL.Tickets;

var builder = WebApplication.CreateBuilder(args);

DapperConfiguration.Configure();

builder.Services.AddControllers();
builder.Services.AddDatabaseUpdater();
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "The 'ConnectionStrings:DefaultConnection' configuration value is required.");

    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddScoped<AppUserDAL>();
builder.Services.AddScoped<TicketDAL>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapAppUserEndpoints();
app.MapTicketEndpoints();

app.Run();
