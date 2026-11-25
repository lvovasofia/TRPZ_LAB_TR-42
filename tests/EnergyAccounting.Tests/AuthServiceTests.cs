using EnergyAccounting.Api.Services;

namespace EnergyAccounting.Tests;

public class AuthServiceTests
{
    [Fact]
    public void RegisterAndLogin_ValidData_Succeeds()
    {
        var service = new AuthService();

        var register = service.Register(
            "test@example.com", "Test12345");

        Assert.True(register.Success);

        var login = service.Login(
            "test@example.com", "Test12345");

        Assert.True(login.Success);
    }

    [Fact]
    public void Register_DuplicateEmail_DoesNotChangeState()
    {
        var service = new AuthService();

        service.Register(
            "test@example.com", "Test12345");

        var result = service.Register(
            "TEST@example.com", "Another123");

        Assert.False(result.Success);
        Assert.Equal(1, service.UserCount);
    }

    [Fact]
    public void Register_InvalidEmail_IsRejected()
    {
        var service = new AuthService();

        var result = service.Register(
            "invalid-email", "Test12345");

        Assert.False(result.Success);
    }

    [Fact]
    public void Register_ShortPassword_IsRejected()
    {
        var service = new AuthService();

        var result = service.Register(
            "test@example.com", "123");

        Assert.False(result.Success);
    }

    [Fact]
    public void Login_InvalidCredentials_IsRejected()
    {
        var service = new AuthService();

        service.Register(
            "test@example.com", "Test12345");

        var wrongPassword = service.Login(
            "test@example.com", "Wrong123");

        var unknownEmail = service.Login(
            "unknown@example.com", "Test12345");

        Assert.False(wrongPassword.Success);
        Assert.False(unknownEmail.Success);
    }
}