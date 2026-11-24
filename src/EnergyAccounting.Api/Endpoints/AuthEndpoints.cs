
using System.Security.Claims;
using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EnergyAccounting.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register",
            (RegisterRequest request, AuthService auth) =>
        {
            var result = auth.Register(
                request.Email, request.Password);

            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    message = result.Message
                });
            }

            return Results.Ok(new
            {
                message = result.Message
            });
        });

        app.MapPost("/api/auth/login",
            async (LoginRequest request,
                   AuthService auth,
                   HttpContext context) =>
        {
            var result = auth.Login(
                request.Email, request.Password);

            if (!result.Success)
            {
                return Results.Json(new
                {
                    message = result.Message
                }, statusCode: 401);
            }

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    result.UserId!.Value.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Results.Ok(new
            {
                message = result.Message
            });
        });
    }
}
