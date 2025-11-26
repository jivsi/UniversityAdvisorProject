namespace UniversityFinder.Models
{
    public class University
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public ICollection<UniversityProgram> Programs { get; set; } = new List<UniversityProgram>();
        public ICollection<UserFavorites> UserFavorites { get; set; } = new List<UserFavorites>();
    }
}
