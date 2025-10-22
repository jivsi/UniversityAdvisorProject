using Microsoft.AspNetCore.Identity;

namespace UniversityAdvisor.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Country { get; set; }
}
