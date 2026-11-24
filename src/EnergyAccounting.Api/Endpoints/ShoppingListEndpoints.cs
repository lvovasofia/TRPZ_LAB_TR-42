
using System.Security.Claims;
using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Api.Endpoints;

public static class ShoppingListEndpoints
{
    public static void MapShoppingListEndpoints(
        this WebApplication app)
    {
        app.MapGet("/api/shopping-list",
            (HttpContext context,
             ShoppingListService shoppingService) =>
        {
            var userId = context.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userId, out var id))
            {
                return Results.Unauthorized();
            }

            var source = context.RequestServices
                .GetService<IShoppingIngredientSource>();

            if (source == null)
            {
                return Results.Json(new
                {
                    success = false,
                    message = "Сервіс розкладу ще не підключено."
                }, statusCode: 503);
            }

            var ingredients = source.GetIngredients(id);

            var result = shoppingService.Generate(ingredients);

            if (!result.Success)
            {
                return Results.BadRequest(result);
            }

            return Results.Ok(result);
        })
        .RequireAuthorization();
    }
}
