using TicketSystem.Realtime.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy =>
    {
        policy
            .WithOrigins("https://localhost:7097", "http://localhost:5047")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("WebApp");

app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/", () => Results.Ok(new
{
    service = "TicketSystem.Realtime",
    chatHub = "/hubs/chat"
}));

app.Run();
