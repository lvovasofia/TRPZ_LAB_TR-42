
namespace EnergyAccounting.Api.Contracts;

public record ShoppingIngredient(
    string Name,
    decimal Quantity,
    string Unit
);

public record ShoppingListItem(
    string Name,
    string Unit,
    decimal TotalQuantity
);

public record ShoppingListResult(
    bool Success,
    string Message,
    IReadOnlyList<ShoppingListItem> Items
);
