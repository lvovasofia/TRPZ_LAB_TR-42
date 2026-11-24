
namespace EnergyAccounting.Api.Models;

public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
