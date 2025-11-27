using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class RvuSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RvuSyncService> _logger;
        private const string RvuUrl = "https://rvu.mon.bg/bg/universities";

        public RvuSyncService(HttpClient httpClient, ILogger<RvuSyncService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Set user agent to avoid blocking
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Fetches and parses Bulgarian universities from the official RVU register
        /// </summary>
        public async Task<List<University>> FetchUniversitiesAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Fetching universities from RVU: {Url}", RvuUrl);
                
                var response = await _httpClient.GetAsync(RvuUrl);
                response.EnsureSuccessStatusCode();
                
                var htmlContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Successfully fetched HTML content ({Length} bytes)", htmlContent.Length);
                
                return ParseUniversitiesFromHtml(htmlContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ HTTP error while fetching RVU page: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error while fetching RVU page: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Parses university data from HTML content
        /// </summary>
        private List<University> ParseUniversitiesFromHtml(string html)
        {
            var universities = new List<University>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Try multiple parsing strategies for robustness
            var found = TryParseByTable(doc, universities) ||
                       TryParseByList(doc, universities) ||
                       TryParseByCards(doc, universities) ||
                       TryParseGeneric(doc, universities);

            if (!found || universities.Count == 0)
            {
                _logger.LogWarning("⚠️ No universities found using standard selectors. Attempting fallback parsing...");
                TryParseFallback(doc, universities);
            }

            // Normalize and deduplicate
            var normalized = NormalizeUniversities(universities);
            
            _logger.LogInformation("✅ Parsed {Count} unique universities from RVU", normalized.Count);
            
            return normalized;
        }

        /// <summary>
        /// Strategy 1: Parse from HTML table structure
        /// </summary>
        private bool TryParseByTable(HtmlDocument doc, List<University> universities)
        {
            try
            {
                var rows = doc.DocumentNode.SelectNodes("//table//tr | //tbody//tr");
                if (rows == null || rows.Count == 0)
                    return false;

                foreach (var row in rows)
                {
                    var cells = row.SelectNodes(".//td | .//th");
                    if (cells == null || cells.Count == 0)
                        continue;

                    var nameCell = cells.FirstOrDefault();
                    if (nameCell == null)
                        continue;

                    var name = ExtractText(nameCell)?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    // Try to extract city from next cells
                    string? city = null;
                    if (cells.Count > 1)
                    {
                        city = ExtractText(cells[1])?.Trim();
                    }

                    universities.Add(new University
                    {
                        Name = name,
                        City = string.IsNullOrWhiteSpace(city) ? "Unknown" : city,
                        Country = "Bulgaria"
                    });
                }

                return universities.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Table parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Strategy 2: Parse from list structure (ul/ol)
        /// </summary>
        private bool TryParseByList(HtmlDocument doc, List<University> universities)
        {
            try
            {
                var items = doc.DocumentNode.SelectNodes("//ul//li | //ol//li | //div[@class='university'] | //div[@class='institution']");
                if (items == null || items.Count == 0)
                    return false;

                foreach (var item in items)
                {
                    var text = ExtractText(item)?.Trim();
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var name = ExtractUniversityName(text);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var city = ExtractCityFromText(text);

                    universities.Add(new University
                    {
                        Name = name,
                        City = city ?? "Unknown",
                        Country = "Bulgaria"
                    });
                }

                return universities.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ List parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Strategy 3: Parse from card/div structures
        /// </summary>
        private bool TryParseByCards(HtmlDocument doc, List<University> universities)
        {
            try
            {
                var cards = doc.DocumentNode.SelectNodes(
                    "//div[contains(@class, 'card')] | " +
                    "//div[contains(@class, 'university-item')] | " +
                    "//div[contains(@class, 'institution-item')] | " +
                    "//article");
                
                if (cards == null || cards.Count == 0)
                    return false;

                foreach (var card in cards)
                {
                    var nameNode = card.SelectSingleNode(".//h1 | .//h2 | .//h3 | .//h4 | .//strong | .//b | .//span[contains(@class, 'name')] | .//div[contains(@class, 'name')]");
                    if (nameNode == null)
                        continue;

                    var name = ExtractText(nameNode)?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var cityNode = card.SelectSingleNode(".//span[contains(@class, 'city')] | .//div[contains(@class, 'city')] | .//p[contains(text(), 'град') or contains(text(), 'City')]");
                    var city = cityNode != null ? ExtractText(cityNode)?.Trim() : null;

                    universities.Add(new University
                    {
                        Name = name,
                        City = string.IsNullOrWhiteSpace(city) ? "Unknown" : city,
                        Country = "Bulgaria"
                    });
                }

                return universities.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Card parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Strategy 4: Generic parsing from any structured content
        /// </summary>
        private bool TryParseGeneric(HtmlDocument doc, List<University> universities)
        {
            try
            {
                // Look for common patterns in text content
                var allText = doc.DocumentNode.InnerText;
                var lines = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 5)
                        continue;

                    // Look for lines that might be university names
                    // Common patterns: "Университет", "Академия", "Колеж", "Институт"
                    if (trimmed.Contains("Университет", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Академия", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Колеж", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Институт", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = ExtractUniversityName(trimmed);
                        if (!string.IsNullOrWhiteSpace(name) && name.Length > 5)
                        {
                            universities.Add(new University
                            {
                                Name = name,
                                City = "Unknown",
                                Country = "Bulgaria"
                            });
                        }
                    }
                }

                return universities.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Generic parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Fallback: Extract university names from any anchor links or headings
        /// </summary>
        private void TryParseFallback(HtmlDocument doc, List<University> universities)
        {
            try
            {
                // Look for links that might contain university names
                var links = doc.DocumentNode.SelectNodes("//a[@href]");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        var text = ExtractText(link)?.Trim();
                        if (string.IsNullOrWhiteSpace(text) || text.Length < 5)
                            continue;

                        if (text.Contains("Университет", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("Академия", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("Колеж", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("Институт", StringComparison.OrdinalIgnoreCase))
                        {
                            var name = ExtractUniversityName(text);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                universities.Add(new University
                                {
                                    Name = name,
                                    City = "Unknown",
                                    Country = "Bulgaria"
                                });
                            }
                        }
                    }
                }

                // Look in headings
                var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");
                if (headings != null)
                {
                    foreach (var heading in headings)
                    {
                        var text = ExtractText(heading)?.Trim();
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        if (text.Contains("Университет", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("Академия", StringComparison.OrdinalIgnoreCase))
                        {
                            var name = ExtractUniversityName(text);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                universities.Add(new University
                                {
                                    Name = name,
                                    City = "Unknown",
                                    Country = "Bulgaria"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Fallback parsing failed: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Extracts clean text from HTML node
        /// </summary>
        private string? ExtractText(HtmlNode node)
        {
            if (node == null)
                return null;

            // Get inner text and clean it
            var text = node.InnerText;
            
            // Remove script and style content
            text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            
            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // Normalize whitespace
            text = Regex.Replace(text, @"\s+", " ", RegexOptions.Compiled);
            
            return text.Trim();
        }

        /// <summary>
        /// Extracts university name from text, cleaning up common prefixes/suffixes
        /// </summary>
        private string ExtractUniversityName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove common prefixes and suffixes
            text = Regex.Replace(text, @"^(Държавен\s+|Частен\s+|Национален\s+)", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+-\s+.*$", ""); // Remove everything after dash
            text = Regex.Replace(text, @"\([^)]*\)", ""); // Remove parentheses content
            text = Regex.Replace(text, @"\[[^\]]*\]", ""); // Remove brackets content

            return text.Trim();
        }

        /// <summary>
        /// Attempts to extract city name from text
        /// </summary>
        private string? ExtractCityFromText(string text)
        {
            // Common Bulgarian cities that might appear in text
            var cities = new[] { "София", "Пловдив", "Варна", "Бургас", "Русе", "Стара Загора", "Плевен", "Сливен", "Добрич", "Шумен", "Ямбол", "Хасково", "Благоевград", "Велико Търново", "Габрово", "Смолян", "Търговище", "Кюстендил", "Пазарджик", "Перник", "Разград", "Силистра", "Търговище" };

            foreach (var city in cities)
            {
                if (text.Contains(city, StringComparison.OrdinalIgnoreCase))
                {
                    return city;
                }
            }

            return null;
        }

        /// <summary>
        /// Normalizes university names, removes duplicates, and cleans data
        /// </summary>
        private List<University> NormalizeUniversities(List<University> universities)
        {
            var normalized = new List<University>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var university in universities)
            {
                if (string.IsNullOrWhiteSpace(university.Name))
                    continue;

                // Normalize name
                var name = university.Name.Trim();
                name = Regex.Replace(name, @"\s+", " "); // Normalize whitespace
                
                // Skip if too short
                if (name.Length < 5)
                    continue;

                // Skip if duplicate
                if (seenNames.Contains(name))
                    continue;

                seenNames.Add(name);

                // Normalize city
                var city = string.IsNullOrWhiteSpace(university.City) || university.City == "Unknown"
                    ? "Unknown"
                    : university.City.Trim();

                normalized.Add(new University
                {
                    Name = name,
                    City = city,
                    Country = "Bulgaria"
                });
            }

            // Sort by name
            normalized.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return normalized;
        }
    }
}

