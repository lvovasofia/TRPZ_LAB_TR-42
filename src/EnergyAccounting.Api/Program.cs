
using EnergyAccounting.Api.Services;
using EnergyAccounting.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ShoppingListService>();


builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;

options.Events.OnValidatePrincipal = context =>
{
    var auth = context.HttpContext.RequestServices
        .GetRequiredService<AuthService>();

    var userId = context.Principal?
        .FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(userId, out var id) ||
        !auth.UserExists(id))
    {
        context.RejectPrincipal();
    }

    return Task.CompletedTask;
};


        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.UseDefaultFiles();
app.UseStaticFiles();


app.UseAuthentication();
app.UseAuthorization();


app.MapAuthEndpoints();
app.MapShoppingListEndpoints();

app.Run();
public partial class Program { }
