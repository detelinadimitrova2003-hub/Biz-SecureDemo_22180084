using System.ComponentModel.DataAnnotations;

namespace BizSecureDemo_22180084.ViewModels;

public class LoginVm
{
    [Required(ErrorMessage = "Email е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден email адрес.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Паролата е задължителна.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}