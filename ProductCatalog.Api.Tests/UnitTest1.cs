using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ProductCatalog.Api.Tests;

public class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    record ProductDto(Guid Id, string Name, decimal Price);
    record ProductCreateDto(string Name, decimal Price);
    record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary);

    [Fact]
    public async Task GetProducts_ReturnsSeededProducts()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "Sample A");
        Assert.Contains(products, p => p.Name == "Sample B");
    }

    [Fact]
    public async Task GetProductById_ReturnsProduct_WhenExists()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        Assert.NotNull(products);
        var first = products.First();
        var resp = await client.GetAsync($"/api/products/{first.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var product = await resp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(first.Id, product!.Id);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/api/products/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_Valid_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var dto = new ProductCreateDto("New Product", 12.50m);
        var resp = await client.PostAsJsonAsync("/api/products", dto);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = await resp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("New Product", created!.Name);
        Assert.Equal(12.50m, created.Price);

        // cleanup - delete created product
        var deleteResp = await client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_InvalidName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var dto = new { Name = "", Price = 1.0m };
        var resp = await client.PostAsJsonAsync("/api/products", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_InvalidPrice_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var dto = new { Name = "X", Price = -5.0m };
        var resp = await client.PostAsJsonAsync("/api/products", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_RemovesProduct()
    {
        var client = _factory.CreateClient();
        // create a product
        var dto = new ProductCreateDto("ToDelete", 3.33m);
        var createResp = await client.PostAsJsonAsync("/api/products", dto);
        var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        // delete it
        var delResp = await client.DeleteAsync($"/api/products/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // verify gone
        var getResp = await client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsFiveItems()
    {
        var client = _factory.CreateClient();
        var forecast = await client.GetFromJsonAsync<List<WeatherForecastDto>>("/weatherforecast");
        Assert.NotNull(forecast);
        Assert.Equal(5, forecast.Count);
    }
}
