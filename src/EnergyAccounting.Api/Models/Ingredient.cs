namespace EnergyAccounting.Api.Models;

public class Ingredient
{
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
}
