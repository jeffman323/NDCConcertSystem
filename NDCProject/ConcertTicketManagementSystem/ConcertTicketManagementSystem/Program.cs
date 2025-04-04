using Microsoft.EntityFrameworkCore;
using ConcertTicketManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<VenueContext>(opt =>
    opt.UseInMemoryDatabase("VenueList"));
builder.Services.AddDbContext<EventContext>(opt =>
    opt.UseInMemoryDatabase("EventList"));
builder.Services.AddDbContext<ReservationContext>(opt =>
    opt.UseInMemoryDatabase("ReservationList"));

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
