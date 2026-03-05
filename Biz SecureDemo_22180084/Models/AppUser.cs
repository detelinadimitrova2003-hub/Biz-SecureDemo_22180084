namespace BizSecureDemo_22180084.Models;

public class AppUser
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Брой неуспешни опити за login
    public int FailedLogins { get; set; } = 0;

    // До кога е заключен акаунтът
    public DateTime? LockoutUntilUtc { get; set; }
}