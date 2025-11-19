using System;

namespace UniversityAdvisor.Domain.Entities;

public class UserProfile : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Country { get; set; }
    public string? Interests { get; set; }
}
