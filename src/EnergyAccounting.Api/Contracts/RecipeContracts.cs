namespace EnergyAccounting.Api.Contracts;

public record IngredientRequest(
    string? Name,
    decimal? Quantity,
    string? Unit
);

public record CreateRecipeRequest(
    string? Name,
    List<IngredientRequest>? Ingredients
);

public record IngredientDto(
    string Name,
    decimal Quantity,
    string Unit
);

public record RecipeDto(
    Guid Id,
    string Name,
    IReadOnlyList<IngredientDto> Ingredients
);

public record RecipeResult(
    bool Success,
    string? Code,
    string Message,
    RecipeDto? Recipe
);
