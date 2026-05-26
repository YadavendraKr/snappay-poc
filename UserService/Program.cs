using UserService.Models;
using UserService.Services;
using Microsoft.EntityFrameworkCore; // Added for UseInMemoryDatabase

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseInMemoryDatabase("UserDb"); // Use in-memory database
});
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    context.Database.EnsureCreated();
    // Optional: Seed initial data if needed
    // context.Users.Add(new User { Id = 1, Name = "Test User", Email = "test@example.com", Role = "Admin", Status = "Active", CreatedAt = DateTime.UtcNow });
    // context.SaveChanges();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapGrpcService<UserGrpcService>();
app.MapControllers();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client or REST API.");

app.Run();
