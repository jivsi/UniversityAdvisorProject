using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;


namespace UniversityAdvisor.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Country { get; set; }

    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<SearchHistory> SearchHistories { get; set; }
    = new List<SearchHistory>();

}
