using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnergyAccounting.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnergyAccounting.Tests;

public class RecipeEndpointsTests
{
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

    private static async Task Login(HttpClient client, string email)
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
    }

    private static CreateRecipeRequest Borsch(string name = "Борщ") =>
        new(name, new List<IngredientRequest>
        {
            new IngredientRequest("Буряк", 2m, "шт"),
            new IngredientRequest("Вода", 1.5m, "л")
        });

    private static StringContent Json(string text) =>
        new(text, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadJson(
        HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Post_WithoutLogin_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/recipes", Borsch());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutLogin_Returns401()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/recipes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_NewUser_ReturnsEmptyList()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(client, "empty-recipes@example.com");

        var response = await client.GetAsync("/api/recipes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJson(response);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task Post_ValidRecipe_Returns201AndAppearsInCatalog()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(client, "create-recipe@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/recipes", Borsch());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var list = await ReadJson(
            await client.GetAsync("/api/recipes"));

        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("Борщ", list[0].GetProperty("name").GetString());
        Assert.Equal(
            2, list[0].GetProperty("ingredients").GetArrayLength());
    }

    [Fact]
    public async Task Post_DuplicateName_Returns409AndCatalogIsUnchanged()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(client, "duplicate-recipe@example.com");

        await client.PostAsJsonAsync("/api/recipes", Borsch());

        var response = await client.PostAsJsonAsync(
            "/api/recipes", Borsch("борщ"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await ReadJson(response);
        Assert.Equal("name_taken", error.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(
                error.GetProperty("message").GetString()));

        var list = await ReadJson(
            await client.GetAsync("/api/recipes"));
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Post_ZeroQuantity_Returns400AndCatalogIsUnchanged()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(client, "zero-quantity@example.com");

        var request = new CreateRecipeRequest(
            "Омлет",
            new List<IngredientRequest>
            {
                new IngredientRequest("Яйце", 0m, "шт")
            });

        var response = await client.PostAsJsonAsync(
            "/api/recipes", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJson(response);
        Assert.Equal(
            "invalid_ingredient", error.GetProperty("code").GetString());

        var list = await ReadJson(
            await client.GetAsync("/api/recipes"));
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Post_EmptyName_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(client, "empty-name@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/recipes", Borsch("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJson(response);
        Assert.Equal("name_empty", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_NoIngredients_Returns400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(client, "no-ingredients@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                "Порожній", new List<IngredientRequest>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJson(response);
        Assert.Equal(
            "no_ingredients", error.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("{ це не json")]
    [InlineData("")]
    [InlineData("{\"name\":\"Борщ\",\"ingredients\":[{\"name\":\"Буряк\",\"quantity\":\"багато\",\"unit\":\"кг\"}]}")]
    [InlineData("{\"name\":123,\"ingredients\":\"текст\"}")]
    public async Task Post_MalformedBody_Returns400WithMessage(string body)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = CreateClient(factory);
        await Login(
            client, $"malformed-{Guid.NewGuid():N}@example.com");

        var response = await client.PostAsync(
            "/api/recipes", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJson(response);
        Assert.Equal(
            "invalid_request", error.GetProperty("code").GetString());

        var list = await ReadJson(
            await client.GetAsync("/api/recipes"));
        Assert.Equal(0, list.GetArrayLength());
    }

    [Fact]
    public async Task Get_ReturnsOnlyOwnRecipes()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var first = CreateClient(factory);
        using var second = CreateClient(factory);

        await Login(first, "owner-one@example.com");
        await Login(second, "owner-two@example.com");

        await first.PostAsJsonAsync("/api/recipes", Borsch("Борщ"));
        await second.PostAsJsonAsync("/api/recipes", Borsch("Омлет"));

        var list = await ReadJson(await first.GetAsync("/api/recipes"));

        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("Борщ", list[0].GetProperty("name").GetString());
    }
}
