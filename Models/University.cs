using System;

namespace UniversityFinder.Models
{
    public class University
    {
        public Guid? Id { get; set; }  // UUID primary key from Supabase
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public ICollection<UniversityProgram> Programs { get; set; } = new List<UniversityProgram>();
        public ICollection<UserFavorites> UserFavorites { get; set; } = new List<UserFavorites>();
    }
}
