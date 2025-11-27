namespace UniversityFinder.Models
{
    public class University
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        
        /// <summary>
        /// HEI (Higher Education Institution) code from RVU/NACID - used as unique key for sync
        /// </summary>
        public string? HeiCode { get; set; }

        public List<string> Programs { get; set; } = new();
        public ICollection<UserFavorites> UserFavorites { get; set; } = new List<UserFavorites>();
    }
}
