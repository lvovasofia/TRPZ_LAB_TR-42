
using EnergyAccounting.Api.Contracts;

namespace EnergyAccounting.Api.Services;

public class ShoppingListService
{
    public ShoppingListResult Generate(
        IReadOnlyCollection<ShoppingIngredient> ingredients)
    {
        if (ingredients.Count == 0)
        {
            return new ShoppingListResult(
                false,
                "Неможливо сформувати список покупок: розклад порожній.",
                Array.Empty<ShoppingListItem>()
            );
        }

        var items = ingredients
            .GroupBy(i => new
            {
                Name = i.Name.Trim().ToUpperInvariant(),
                Unit = i.Unit.Trim()
            })
            .Select(group => new ShoppingListItem(
                group.First().Name.Trim(),
                group.Key.Unit,
                Math.Round(
                    group.Sum(i => i.Quantity),
                    3,
                    MidpointRounding.AwayFromZero)
            ))
            .OrderBy(
                i => i.Name,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Unit)
            .ToList();

        return new ShoppingListResult(
            true,
            "Список покупок успішно сформовано.",
            items
        );
    }
}
