using System.Security.Claims;
using System.Text.Json;
using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Api.Endpoints;

public static class RecipeEndpoints
{
    public static void MapRecipeEndpoints(this WebApplication app)
    {
        // UC-03: створити рецепт.
        app.MapPost("/api/recipes",
            async Task<IResult> (HttpContext context, RecipeService recipes) =>
        {
            if (!TryGetUserId(context.User, out var userId))
            {
                return Results.Unauthorized();
            }

            CreateRecipeRequest? request;

            try
            {
                request = await context.Request
                    .ReadFromJsonAsync<CreateRecipeRequest>();
            }
            catch (Exception ex) when (
                ex is JsonException ||
                ex is InvalidOperationException ||
                ex is BadHttpRequestException)
            {
                return Error(
                    400,
                    "invalid_request",
                    "Некоректні дані рецепта. Перевірте назву та кількість інгредієнтів.");
            }

            if (request is null)
            {
                return Error(
                    400,
                    "invalid_request",
                    "Порожній запит.");
            }

            var result = recipes.Create(
                userId, request.Name, request.Ingredients);

            if (result.Success)
            {
                return Results.Created(
                    $"/api/recipes/{result.Recipe!.Id}",
                    result.Recipe);
            }

            var status = result.Code == "name_taken" ? 409 : 400;

            return Error(status, result.Code!, result.Message);
        })
        .RequireAuthorization();

        // UC-04: довідник рецептів.
        app.MapGet("/api/recipes",
            (HttpContext context, RecipeService recipes) =>
        {
            if (!TryGetUserId(context.User, out var userId))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(recipes.GetAll(userId));
        })
        .RequireAuthorization();
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        return Guid.TryParse(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out userId);
    }

    private static IResult Error(int status, string code, string message)
    {
        return Results.Json(new { code, message }, statusCode: status);
    }
}
