using Npgsql;
using TicketSystem.Api.Features.AppUsers;
using TicketSystem.Api.Features.Customers;
using TicketSystem.Api.Features.UpdateDatabase;
using TicketSystem.DAL.AppUsers;
using TicketSystem.DAL.Configuration;
using TicketSystem.DAL.Customers;

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
builder.Services.AddScoped<AppUserRepository>();
builder.Services.AddScoped<CustomerRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapAppUserEndpoints();
app.MapCustomerEndpoints();

app.Run();
