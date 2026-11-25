namespace EnergyAccounting.Api.Models;

public class Recipe
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<Ingredient> Ingredients { get; init; }
        = Array.Empty<Ingredient>();
}
