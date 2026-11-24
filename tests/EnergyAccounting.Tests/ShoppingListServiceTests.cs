using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Tests;

public class ShoppingListServiceTests
{
    [Fact]
    public void Generate_ValidIngredients_ReturnsCorrectList()
    {
        var service = new ShoppingListService();

        var ingredients = new[]
        {
            new ShoppingIngredient("Молоко", 0.25m, "л"),
            new ShoppingIngredient("молоко", 0.5m, "л"),
            new ShoppingIngredient("Молоко", 100m, "мл"),
            new ShoppingIngredient("Борошно", 1m, "кг")
        };

        var expected = new[]
        {
            new ShoppingListItem("Борошно", "кг", 1m),
            new ShoppingListItem("Молоко", "л", 0.75m),
            new ShoppingListItem("Молоко", "мл", 100m)
        };

        var result = service.Generate(ingredients);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Items);
    }

    [Fact]
    public void Generate_Quantity_RoundsToThreeDecimals()
    {
        var service = new ShoppingListService();

        var ingredients = new[]
        {
            new ShoppingIngredient("Молоко", 0.1234m, "л"),
            new ShoppingIngredient("Молоко", 0.1234m, "л")
        };

        var result = service.Generate(ingredients);

        var item = Assert.Single(result.Items);

        Assert.Equal(0.247m, item.TotalQuantity);
    }

    [Fact]
    public void Generate_EmptyIngredients_IsRejected()
    {
        var service = new ShoppingListService();

        var ingredients = new List<ShoppingIngredient>();

        var result = service.Generate(ingredients);

        Assert.False(result.Success);
        Assert.Empty(result.Items);
    }
}