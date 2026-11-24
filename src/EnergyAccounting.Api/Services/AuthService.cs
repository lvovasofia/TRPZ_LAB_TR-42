using System.Net.Mail;
using EnergyAccounting.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace EnergyAccounting.Api.Services;

public class AuthService
{
    private readonly List<User> _users = new();
    private readonly object _lock = new();
    private readonly PasswordHasher<User> _hasher = new();

    private User? FindUser(string email)
    {
        foreach (var user in _users)
        {
            if (string.Equals(user.Email, email,
                StringComparison.OrdinalIgnoreCase))
            {
                return user;
            }
        }

        return null;
    }

    public (bool Success, string Message) Register(
        string? email, string? password)
    {
        email = email?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return (false, "Заповніть усі поля.");
        }

        if (!MailAddress.TryCreate(email, out var address) ||
            address.Address != email)
        {
            return (false, "Некоректна електронна пошта.");
        }

        if (password.Length < 8)
        {
            return (false, "Пароль має містити мінімум 8 символів.");
        }

        lock (_lock)
        {
            if (FindUser(email) != null)
            {
                return (false, "Користувач вже існує.");
            }

            var user = new User
            {
                Email = email
            };

            user.PasswordHash = _hasher.HashPassword(user, password);

            _users.Add(user);

            return (true, "Реєстрація успішна.");
        }
    }

    public (bool Success, string Message, Guid? UserId) Login(
        string? email, string? password)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return (false, "Заповніть усі поля.", null);
        }

        lock (_lock)
        {
            var user = FindUser(email.Trim());

            if (user == null)
            {
                return (false, "Неправильна пошта або пароль.", null);
            }

            var result = _hasher.VerifyHashedPassword(
                user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                return (false, "Неправильна пошта або пароль.", null);
            }

            return (true, "Вхід успішний.", user.Id);
        }
    }

    public int UserCount
    {
        get
        {
            lock (_lock)
            {
                return _users.Count;
            }
        }
    }

    public bool UserExists(Guid userId)
    {
        lock (_lock)
        {
            return _users.Any(user => user.Id == userId);
        }
    }
}