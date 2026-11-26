using System.Security.Claims;
using System.Text.Json;
using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this WebApplication app)
    {
        app.MapPost("/api/schedule",
            async Task<IResult> (HttpContext context, ScheduleService schedule) =>
            {
                if (!TryGetUserId(context.User, out var userId))
                {
                    return Results.Unauthorized();
                }

                AddScheduleEntryRequest? request;

                try
                {
                    request = await context.Request
                        .ReadFromJsonAsync<AddScheduleEntryRequest>();
                }
                catch (Exception ex) when (
                    ex is JsonException ||
                    ex is InvalidOperationException ||
                    ex is BadHttpRequestException)
                {
                    return Error(400, "invalid_request",
                        "Некоректні дані запиту.");
                }

                if (request is null)
                {
                    return Error(400, "invalid_request", "Порожній запит.");
                }

                var result = schedule.Add(
                    userId, request.Day, request.Meal, request.RecipeId);

                if (result.Success)
                {
                    return Results.Created("/api/schedule", result.Entry);
                }

                var status = result.Code switch
                {
                    "slot_taken" => 409,        
                    "recipe_not_found" => 404,  
                    _ => 400                    
                };

                return Error(status, result.Code!, result.Message);
            })
        .RequireAuthorization();

        app.MapGet("/api/schedule",
            (HttpContext context, ScheduleService schedule) =>
            {
                if (!TryGetUserId(context.User, out var userId))
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(schedule.GetForUser(userId));
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