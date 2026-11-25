
using EnergyAccounting.Api.Contracts;

namespace EnergyAccounting.Api.Services;

public interface IShoppingIngredientSource
{
    IReadOnlyCollection<ShoppingIngredient> GetIngredients(
        Guid userId);
}
