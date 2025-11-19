using System.ComponentModel.DataAnnotations;

namespace UniversityAdvisor.ViewModels;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Country")]
    public string? Country { get; set; }
}
