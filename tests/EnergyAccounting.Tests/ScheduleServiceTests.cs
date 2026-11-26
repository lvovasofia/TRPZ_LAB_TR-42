using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Tests;

public class ScheduleServiceTests
{
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();

    private static Guid CreateRecipe(RecipeService recipes, Guid userId, string name)
    {
        var result = recipes.Create(userId, name, new List<IngredientRequest>
        {
            new IngredientRequest("Борошно", 0.5m, "кг")
        });

        Assert.True(result.Success);
        return result.Recipe!.Id;
    }

    [Fact]
    public void Add_FreeSlot_EntryIsAdded()
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var recipeId = CreateRecipe(recipes, UserA, "Налисники");

        var result = schedule.Add(UserA, "Monday", "Breakfast", recipeId);

        Assert.True(result.Success);
        var entry = Assert.Single(schedule.GetForUser(UserA));
        Assert.Equal("Monday", entry.Day);
        Assert.Equal("Breakfast", entry.Meal);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal("Налисники", entry.RecipeName);
    }

    [Fact]
    public void Add_TakenSlot_IsRejectedAndScheduleUnchanged()
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var first = CreateRecipe(recipes, UserA, "Налисники");
        var second = CreateRecipe(recipes, UserA, "Омлет");
        schedule.Add(UserA, "Monday", "Breakfast", first);

        var result = schedule.Add(UserA, "Monday", "Breakfast", second);

        Assert.False(result.Success);
        Assert.Equal("slot_taken", result.Code);
        Assert.Null(result.Entry);

        var entry = Assert.Single(schedule.GetForUser(UserA));
        Assert.Equal(first, entry.RecipeId);
    }

    [Fact]
    public void Add_SameDayOtherMeal_IsAccepted()
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var recipeId = CreateRecipe(recipes, UserA, "Налисники");
        schedule.Add(UserA, "Monday", "Breakfast", recipeId);

        var result = schedule.Add(UserA, "Monday", "Dinner", recipeId);

        Assert.True(result.Success);
        Assert.Equal(2, schedule.GetForUser(UserA).Count);
    }

    [Fact]
    public void Add_OtherUsersRecipe_IsRejected()
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var recipeOfB = CreateRecipe(recipes, UserB, "Чужий рецепт");

        var result = schedule.Add(UserA, "Monday", "Lunch", recipeOfB);

        Assert.False(result.Success);
        Assert.Equal("recipe_not_found", result.Code);
        Assert.Empty(schedule.GetForUser(UserA));
    }

    [Theory]
    [InlineData(null, "Lunch", "invalid_day")]
    [InlineData("Понеділок", "Lunch", "invalid_day")]
    [InlineData("7", "Lunch", "invalid_day")]
    [InlineData("Monday", "Supper", "invalid_meal")]
    [InlineData("Monday", "", "invalid_meal")]
    public void Add_InvalidDayOrMeal_IsRejected(
        string? day, string? meal, string expectedCode)
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var recipeId = CreateRecipe(recipes, UserA, "Налисники");

        var result = schedule.Add(UserA, day, meal, recipeId);

        Assert.False(result.Success);
        Assert.Equal(expectedCode, result.Code);
        Assert.Empty(schedule.GetForUser(UserA));
    }

    [Fact]
    public void GetForUser_ReturnsOnlyOwnEntries()
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var recipeOfA = CreateRecipe(recipes, UserA, "Налисники");
        schedule.Add(UserA, "Monday", "Breakfast", recipeOfA);

        Assert.Single(schedule.GetForUser(UserA));
        Assert.Empty(schedule.GetForUser(UserB));
    }

    [Fact]
    public void GetForUser_IsSortedFromMonday()
    {
        var recipes = new RecipeService();
        var schedule = new ScheduleService(recipes);
        var recipeId = CreateRecipe(recipes, UserA, "Налисники");
        schedule.Add(UserA, "Sunday", "Lunch", recipeId);
        schedule.Add(UserA, "Monday", "Lunch", recipeId);

        var entries = schedule.GetForUser(UserA);

        Assert.Equal("Monday", entries[0].Day);
        Assert.Equal("Sunday", entries[1].Day);
    }
}