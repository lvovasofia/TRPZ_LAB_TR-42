using System.Globalization;
using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Tests;

public class RecipeServiceTests
{
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();

    private static List<IngredientRequest> Ingredients() =>
        new()
        {
            new IngredientRequest("Борошно", 0.5m, "кг"),
            new IngredientRequest("Молоко", 0.25m, "л")
        };

    [Fact]
    public void Create_ValidRecipe_IsSavedWithTrimmedFields()
    {
        var service = new RecipeService();

        var result = service.Create(
            UserA,
            "  Налисники  ",
            new List<IngredientRequest>
            {
                new IngredientRequest("  Борошно ", 0.5m, " кг ")
            });

        Assert.True(result.Success);
        Assert.NotNull(result.Recipe);
        Assert.Equal("Налисники", result.Recipe.Name);

        var saved = Assert.Single(service.GetAll(UserA));
        Assert.Equal(result.Recipe.Id, saved.Id);

        var ingredient = Assert.Single(saved.Ingredients);
        Assert.Equal("Борошно", ingredient.Name);
        Assert.Equal(0.5m, ingredient.Quantity);
        Assert.Equal("кг", ingredient.Unit);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceName_IsRejected(string? name)
    {
        var service = new RecipeService();

        var result = service.Create(UserA, name, Ingredients());

        Assert.False(result.Success);
        Assert.Equal("name_empty", result.Code);
        Assert.Empty(service.GetAll(UserA));
    }

    [Fact]
    public void Create_TooLongName_IsRejected()
    {
        var service = new RecipeService();

        var result = service.Create(
            UserA,
            new string('а', RecipeService.MaxNameLength + 1),
            Ingredients());

        Assert.False(result.Success);
        Assert.Equal("name_too_long", result.Code);
        Assert.Empty(service.GetAll(UserA));
    }

    [Fact]
    public void Create_DuplicateName_IsRejectedWithoutChangingState()
    {
        var service = new RecipeService();
        var first = service.Create(UserA, "Борщ", Ingredients());

        var result = service.Create(UserA, " борщ ", Ingredients());

        Assert.False(result.Success);
        Assert.Equal("name_taken", result.Code);

        var saved = Assert.Single(service.GetAll(UserA));
        Assert.Equal(first.Recipe!.Id, saved.Id);
        Assert.Equal(2, saved.Ingredients.Count);
    }

    [Fact]
    public void Create_SameNameForDifferentUsers_IsAllowed()
    {
        var service = new RecipeService();

        var first = service.Create(UserA, "Борщ", Ingredients());
        var second = service.Create(UserB, "Борщ", Ingredients());

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Single(service.GetAll(UserA));
        Assert.Single(service.GetAll(UserB));
    }

    [Fact]
    public void Create_NullIngredients_IsRejected()
    {
        var service = new RecipeService();

        var result = service.Create(UserA, "Борщ", null);

        Assert.False(result.Success);
        Assert.Equal("no_ingredients", result.Code);
        Assert.Empty(service.GetAll(UserA));
    }

    [Fact]
    public void Create_EmptyIngredientList_IsRejected()
    {
        var service = new RecipeService();

        var result = service.Create(
            UserA, "Борщ", new List<IngredientRequest>());

        Assert.False(result.Success);
        Assert.Equal("no_ingredients", result.Code);
        Assert.Empty(service.GetAll(UserA));
    }

    [Theory]
    [InlineData("Борошно", "0", "кг")]
    [InlineData("Борошно", "-1", "кг")]
    [InlineData("Борошно", null, "кг")]
    [InlineData("Борошно", "0.0001", "кг")]
    [InlineData("Борошно", "1000001", "кг")]
    [InlineData("", "1", "кг")]
    [InlineData("   ", "1", "кг")]
    [InlineData("Борошно", "1", "")]
    [InlineData("Борошно", "1", null)]
    public void Create_InvalidIngredient_IsRejectedWithoutChangingState(
        string name, string? quantity, string? unit)
    {
        var service = new RecipeService();

        decimal? parsed = quantity is null
            ? null
            : decimal.Parse(quantity, CultureInfo.InvariantCulture);

        var result = service.Create(
            UserA,
            "Борщ",
            new List<IngredientRequest>
            {
                new IngredientRequest("Буряк", 2m, "шт"),
                new IngredientRequest(name, parsed, unit)
            });

        Assert.False(result.Success);
        Assert.Equal("invalid_ingredient", result.Code);
        Assert.Contains("№2", result.Message);
        Assert.Empty(service.GetAll(UserA));
    }

    [Fact]
    public void Create_ThreeDecimalQuantity_IsAccepted()
    {
        var service = new RecipeService();

        var result = service.Create(
            UserA,
            "Тісто",
            new List<IngredientRequest>
            {
                new IngredientRequest("Борошно", 0.125m, "кг")
            });

        Assert.True(result.Success);
        Assert.Equal(0.125m, result.Recipe!.Ingredients[0].Quantity);
    }

    [Fact]
    public void GetAll_NoRecipes_ReturnsEmptyList()
    {
        var service = new RecipeService();

        Assert.Empty(service.GetAll(UserA));
    }

    [Fact]
    public void GetAll_ReturnsOnlyRecipesOfThatUser()
    {
        var service = new RecipeService();
        service.Create(UserA, "Борщ", Ingredients());
        service.Create(UserB, "Омлет", Ingredients());

        var recipes = service.GetAll(UserA);

        var recipe = Assert.Single(recipes);
        Assert.Equal("Борщ", recipe.Name);
    }

    [Fact]
    public void FindOwned_OtherUsersRecipe_ReturnsNull()
    {
        var service = new RecipeService();
        var created = service.Create(UserA, "Борщ", Ingredients());

        Assert.NotNull(service.FindOwned(UserA, created.Recipe!.Id));
        Assert.Null(service.FindOwned(UserB, created.Recipe.Id));
        Assert.Null(service.FindOwned(UserA, Guid.NewGuid()));
    }
}
