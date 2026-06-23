using System;
using System.IO;
using System.Collections.Concurrent;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();

//app.UseHttpsRedirection();

var summaries = new[]
{
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };


// In-memory product store
var products = new ConcurrentDictionary<Guid, Product>();

// Seed sample products
var p1 = new Product(Guid.NewGuid(), "Sample A", 9.99m);
var p2 = new Product(Guid.NewGuid(), "Sample B", 19.99m);
products[p1.Id] = p1;
products[p2.Id] = p2;

// Product APIs
app.MapGet("/api/products", () =>
{
    return Results.Ok(products.Values.OrderBy(p => p.Name));
})
.WithName("GetProducts");

app.MapGet("/api/products/{id:guid}", (Guid id) =>
{
    return products.TryGetValue(id, out var product)
        ? Results.Ok(product)
        : Results.NotFound();
})
.WithName("GetProductById");

app.MapPost("/api/products", (ProductCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Name"] = new[] { "Name is required." }
        });

    if (dto.Price < 0)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Price"] = new[] { "Price must be >= 0." }
        });

    var product = new Product(Guid.NewGuid(), dto.Name.Trim(), dto.Price);
    products[product.Id] = product;

    return Results.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct");

app.MapDelete("/api/products/{id:guid}", (Guid id) =>
{
    return products.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound();
})
.WithName("DeleteProduct");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record Product(Guid Id, string Name, decimal Price);

record ProductCreateDto(string Name, decimal Price);

// Expose Program so WebApplicationFactory<Program> in tests can find the entry point.
public partial class Program { }
