using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Models;

namespace EnergyAccounting.Api.Services;

public class RecipeService
{
    public const int MaxNameLength = 100;
    public const int MaxUnitLength = 20;
    public const int MaxIngredients = 50;
    public const decimal MaxQuantity = 1_000_000m;

    private readonly List<Recipe> _recipes = new();
    private readonly object _lock = new();

    // UC-03: створити рецепт. Дані змінюються лише після всіх перевірок.
    public RecipeResult Create(
        Guid userId,
        string? name,
        IReadOnlyList<IngredientRequest>? ingredients)
    {
        // Крок 5 (A1): назва заповнена.
        var recipeName = name?.Trim();

        if (string.IsNullOrWhiteSpace(recipeName))
        {
            return Fail("name_empty", "Введіть назву рецепта.");
        }

        if (recipeName.Length > MaxNameLength)
        {
            return Fail(
                "name_too_long",
                $"Назва рецепта не може бути довшою за {MaxNameLength} символів.");
        }

        lock (_lock)
        {
            // Крок 6 (A2): назва унікальна в довіднику цього користувача.
            var exists = _recipes.Any(recipe =>
                recipe.OwnerId == userId &&
                string.Equals(
                    recipe.Name,
                    recipeName,
                    StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return Fail(
                    "name_taken",
                    "Рецепт із такою назвою вже є у вашому довіднику.");
            }

            // Крок 7 (A3): є хоча б один інгредієнт.
            if (ingredients is null || ingredients.Count == 0)
            {
                return Fail(
                    "no_ingredients",
                    "Додайте хоча б один інгредієнт.");
            }

            if (ingredients.Count > MaxIngredients)
            {
                return Fail(
                    "too_many_ingredients",
                    $"Рецепт може містити не більше {MaxIngredients} інгредієнтів.");
            }

            // Крок 8 (A4): кожен інгредієнт коректний.
            var validated = new List<Ingredient>();

            for (var i = 0; i < ingredients.Count; i++)
            {
                var error = ValidateIngredient(
                    i + 1, ingredients[i], out var ingredient);

                if (error is not null)
                {
                    return Fail("invalid_ingredient", error);
                }

                validated.Add(ingredient!);
            }

            // Крок 9: зберігаємо рецепт у довіднику користувача.
            var recipe = new Recipe
            {
                OwnerId = userId,
                Name = recipeName,
                Ingredients = validated
            };

            _recipes.Add(recipe);

            return new RecipeResult(
                true,
                null,
                "Рецепт успішно створено.",
                ToDto(recipe));
        }
    }

    // UC-04: довідник рецептів поточного користувача.
    public IReadOnlyList<RecipeDto> GetAll(Guid userId)
    {
        lock (_lock)
        {
            return _recipes
                .Where(recipe => recipe.OwnerId == userId)
                .Select(ToDto)
                .ToList();
        }
    }

    // Для розкладу меню (UC-05): рецепт, який належить користувачу.
    public Recipe? FindOwned(Guid userId, Guid recipeId)
    {
        lock (_lock)
        {
            return _recipes.FirstOrDefault(recipe =>
                recipe.Id == recipeId && recipe.OwnerId == userId);
        }
    }

    private static string? ValidateIngredient(
        int number,
        IngredientRequest? request,
        out Ingredient? ingredient)
    {
        ingredient = null;

        var name = request?.Name?.Trim();
        var unit = request?.Unit?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return $"Інгредієнт №{number}: введіть назву.";
        }

        if (name.Length > MaxNameLength)
        {
            return $"Інгредієнт №{number}: назва довша за {MaxNameLength} символів.";
        }

        if (request?.Quantity is null)
        {
            return $"Інгредієнт №{number}: вкажіть кількість числом.";
        }

        var quantity = request.Quantity.Value;

        if (quantity <= 0m)
        {
            return $"Інгредієнт №{number}: кількість має бути більшою за нуль.";
        }

        if (quantity > MaxQuantity)
        {
            return $"Інгредієнт №{number}: кількість завелика.";
        }

        if (decimal.Round(quantity, 3) != quantity)
        {
            return $"Інгредієнт №{number}: кількість має мати не більше 3 знаків після коми.";
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return $"Інгредієнт №{number}: вкажіть одиницю вимірювання.";
        }

        if (unit.Length > MaxUnitLength)
        {
            return $"Інгредієнт №{number}: одиниця вимірювання задовга.";
        }

        ingredient = new Ingredient
        {
            Name = name,
            Quantity = quantity,
            Unit = unit
        };

        return null;
    }

    private static RecipeDto ToDto(Recipe recipe)
    {
        return new RecipeDto(
            recipe.Id,
            recipe.Name,
            recipe.Ingredients
                .Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit))
                .ToList());
    }

    private static RecipeResult Fail(string code, string message)
    {
        return new RecipeResult(false, code, message, null);
    }
}
