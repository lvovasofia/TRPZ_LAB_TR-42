using System.Net;
using System.Net.Http.Json;
using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnergyAccounting.Tests;

public class ShoppingListEndpointsTests
{
    private sealed class TestIngredientSource
        : IShoppingIngredientSource
    {
        private readonly Dictionary<Guid,
            IReadOnlyCollection<ShoppingIngredient>> _data = new();

        public void SetIngredients(
            Guid userId,
            params ShoppingIngredient[] ingredients)
        {
            _data[userId] = ingredients.ToArray();
        }

        public IReadOnlyCollection<ShoppingIngredient>
            GetIngredients(Guid userId)
        {
            return _data.TryGetValue(userId, out var ingredients)
                ? ingredients.ToArray()
                : Array.Empty<ShoppingIngredient>();
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(
        TestIngredientSource source)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IShoppingIngredientSource>();

                    services.AddSingleton<IShoppingIngredientSource>(
                        source);
                });
            });
    }

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
                HandleCookies = true
            });
    }

    private static async Task<Guid> RegisterAndLogin(
        HttpClient client,
        WebApplicationFactory<Program> factory,
        string email)
    {
        const string password = "Test12345";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, password));

        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, password));

        login.EnsureSuccessStatusCode();

        var auth = factory.Services
            .GetRequiredService<AuthService>();

        var result = auth.Login(email, password);

        return result.UserId!.Value;
    }

    [Fact]
    public async Task Get_WithoutAuthorization_Returns401()
    {
        var source = new TestIngredientSource();

        using var factory = CreateFactory(source);
        using var client = CreateClient(factory);

        var response = await client.GetAsync(
            "/api/shopping-list");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_WithIngredients_ReturnsShoppingList()
    {
        var source = new TestIngredientSource();

        using var factory = CreateFactory(source);
        using var client = CreateClient(factory);

        var userId = await RegisterAndLogin(
            client, factory, "shopping@example.com");

        source.SetIngredients(userId,
            new ShoppingIngredient("Молоко", 0.25m, "л"),
            new ShoppingIngredient("молоко", 0.50m, "л"),
            new ShoppingIngredient("Молоко", 100m, "мл"),
            new ShoppingIngredient("Борошно", 0.5m, "кг"));

        var response = await client.GetAsync(
            "/api/shopping-list");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ShoppingListResult>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);

        Assert.Contains(result.Items, item =>
            item.Name == "Молоко" &&
            item.Unit == "л" &&
            item.TotalQuantity == 0.75m);
    }

    [Fact]
    public async Task Get_EmptySchedule_Returns400()
    {
        var source = new TestIngredientSource();

        using var factory = CreateFactory(source);
        using var client = CreateClient(factory);

        await RegisterAndLogin(
            client, factory, "empty@example.com");

        var response = await client.GetAsync(
            "/api/shopping-list");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}