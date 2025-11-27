using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class RvuProgramImportService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RvuProgramImportService> _logger;
        private const string RvuBaseUrl = "https://rvu.mon.bg/bg/universities";

        public RvuProgramImportService(HttpClient httpClient, ILogger<RvuProgramImportService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Set user agent to avoid blocking
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Fetches programs for a specific university from RVU
        /// </summary>
        public async Task<List<UniversityProgram>> FetchProgramsForUniversityAsync(string universityName)
        {
            try
            {
                // Convert university name to slug for URL
                var slug = GenerateSlugFromName(universityName);
                var url = $"{RvuBaseUrl}/{slug}";
                
                _logger.LogInformation("🔄 Fetching programs for {University} from: {Url}", universityName, url);
                
                var response = await _httpClient.GetAsync(url);
                
                // If direct URL fails, try the main universities page and find the link
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ Direct URL failed ({Status}), trying alternative method...", response.StatusCode);
                    return await FetchProgramsViaMainPageAsync(universityName);
                }
                
                var htmlContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Successfully fetched HTML content for {University} ({Length} bytes)", universityName, htmlContent.Length);
                
                return ParseProgramsFromHtml(htmlContent, universityName);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ HTTP error while fetching programs for {University}: {Message}", universityName, ex.Message);
                return new List<UniversityProgram>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error while fetching programs for {University}: {Message}", universityName, ex.Message);
                return new List<UniversityProgram>();
            }
        }

        /// <summary>
        /// Alternative method: fetch via main page and find university link
        /// </summary>
        private async Task<List<UniversityProgram>> FetchProgramsViaMainPageAsync(string universityName)
        {
            try
            {
                var response = await _httpClient.GetAsync(RvuBaseUrl);
                response.EnsureSuccessStatusCode();
                
                var htmlContent = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);
                
                // Find link to university page
                var links = doc.DocumentNode.SelectNodes("//a[@href]");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        var linkText = ExtractText(link)?.Trim();
                        if (string.IsNullOrWhiteSpace(linkText))
                            continue;
                            
                        // Check if link text contains university name
                        if (linkText.Contains(universityName, StringComparison.OrdinalIgnoreCase) ||
                            universityName.Contains(linkText, StringComparison.OrdinalIgnoreCase))
                        {
                            var href = link.GetAttributeValue("href", "");
                            if (string.IsNullOrWhiteSpace(href))
                                continue;
                                
                            // Make absolute URL if relative
                            var universityUrl = href.StartsWith("http") 
                                ? href 
                                : $"https://rvu.mon.bg{href.TrimStart('/')}";
                                
                            var programResponse = await _httpClient.GetAsync(universityUrl);
                            if (programResponse.IsSuccessStatusCode)
                            {
                                var programHtml = await programResponse.Content.ReadAsStringAsync();
                                return ParseProgramsFromHtml(programHtml, universityName);
                            }
                        }
                    }
                }
                
                return new List<UniversityProgram>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Alternative fetch method failed: {Message}", ex.Message);
                return new List<UniversityProgram>();
            }
        }

        /// <summary>
        /// Parses programs from HTML content
        /// </summary>
        private List<UniversityProgram> ParseProgramsFromHtml(string html, string universityName)
        {
            var programs = new List<UniversityProgram>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Try multiple parsing strategies
            var found = TryParseByTable(doc, programs, universityName) ||
                       TryParseByList(doc, programs, universityName) ||
                       TryParseByCards(doc, programs, universityName) ||
                       TryParseGeneric(doc, programs, universityName);

            if (!found || programs.Count == 0)
            {
                _logger.LogWarning("⚠️ No programs found using standard selectors for {University}. Attempting fallback parsing...", universityName);
                TryParseFallback(doc, programs, universityName);
            }

            // Normalize and deduplicate
            var normalized = NormalizePrograms(programs, universityName);
            
            _logger.LogInformation("✅ Parsed {Count} unique programs for {University}", normalized.Count, universityName);
            
            return normalized;
        }

        /// <summary>
        /// Strategy 1: Parse from HTML table structure
        /// </summary>
        private bool TryParseByTable(HtmlDocument doc, List<UniversityProgram> programs, string universityName)
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

                    var programName = ExtractText(nameCell)?.Trim();
                    if (string.IsNullOrWhiteSpace(programName) || programName.Length < 3)
                        continue;

                    // Skip header rows
                    if (programName.Contains("Програма", StringComparison.OrdinalIgnoreCase) ||
                        programName.Contains("Специалност", StringComparison.OrdinalIgnoreCase) ||
                        programName.Contains("Program", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var degreeType = ExtractDegreeType(cells, doc);
                    var duration = ExtractDuration(cells, doc);
                    var language = ExtractLanguage(cells, doc);

                    programs.Add(new UniversityProgram
                    {
                        UniversityName = universityName,
                        Name = programName,
                        DegreeType = degreeType,
                        Duration = duration,
                        Language = language ?? "Bulgarian"
                    });
                }

                return programs.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Table parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Strategy 2: Parse from list structure
        /// </summary>
        private bool TryParseByList(HtmlDocument doc, List<UniversityProgram> programs, string universityName)
        {
            try
            {
                var items = doc.DocumentNode.SelectNodes("//ul//li | //ol//li | //div[@class='program'] | //div[@class='specialty']");
                if (items == null || items.Count == 0)
                    return false;

                foreach (var item in items)
                {
                    var text = ExtractText(item)?.Trim();
                    if (string.IsNullOrWhiteSpace(text) || text.Length < 3)
                        continue;

                    var programName = ExtractProgramName(text);
                    if (string.IsNullOrWhiteSpace(programName))
                        continue;

                    var degreeType = ExtractDegreeTypeFromText(text);
                    var duration = ExtractDurationFromText(text);
                    var language = ExtractLanguageFromText(text);

                    programs.Add(new UniversityProgram
                    {
                        UniversityName = universityName,
                        Name = programName,
                        DegreeType = degreeType,
                        Duration = duration,
                        Language = language ?? "Bulgarian"
                    });
                }

                return programs.Count > 0;
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
        private bool TryParseByCards(HtmlDocument doc, List<UniversityProgram> programs, string universityName)
        {
            try
            {
                var cards = doc.DocumentNode.SelectNodes(
                    "//div[contains(@class, 'program')] | " +
                    "//div[contains(@class, 'specialty')] | " +
                    "//div[contains(@class, 'degree')] | " +
                    "//article[contains(@class, 'program')]");
                
                if (cards == null || cards.Count == 0)
                    return false;

                foreach (var card in cards)
                {
                    var nameNode = card.SelectSingleNode(".//h1 | .//h2 | .//h3 | .//h4 | .//strong | .//b | .//span[contains(@class, 'name')] | .//div[contains(@class, 'name')]");
                    if (nameNode == null)
                        continue;

                    var programName = ExtractText(nameNode)?.Trim();
                    if (string.IsNullOrWhiteSpace(programName) || programName.Length < 3)
                        continue;

                    var degreeTypeNode = card.SelectSingleNode(".//span[contains(@class, 'degree')] | .//div[contains(@class, 'degree')]");
                    var durationNode = card.SelectSingleNode(".//span[contains(@class, 'duration')] | .//div[contains(@class, 'duration')]");
                    var languageNode = card.SelectSingleNode(".//span[contains(@class, 'language')] | .//div[contains(@class, 'language')]");

                    var degreeType = degreeTypeNode != null ? ExtractText(degreeTypeNode)?.Trim() : null;
                    var duration = durationNode != null ? ExtractDurationFromText(ExtractText(durationNode) ?? "") : null;
                    var language = languageNode != null ? ExtractText(languageNode)?.Trim() : null;

                    programs.Add(new UniversityProgram
                    {
                        UniversityName = universityName,
                        Name = programName,
                        DegreeType = degreeType,
                        Duration = duration,
                        Language = language ?? "Bulgarian"
                    });
                }

                return programs.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Card parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Strategy 4: Generic parsing
        /// </summary>
        private bool TryParseGeneric(HtmlDocument doc, List<UniversityProgram> programs, string universityName)
        {
            try
            {
                var allText = doc.DocumentNode.InnerText;
                var lines = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 5)
                        continue;

                    // Look for program-like patterns
                    if (trimmed.Contains("Бакалавър", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Магистър", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Доктор", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Bachelor", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Master", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("PhD", StringComparison.OrdinalIgnoreCase))
                    {
                        var programName = ExtractProgramName(trimmed);
                        if (!string.IsNullOrWhiteSpace(programName))
                        {
                            var degreeType = ExtractDegreeTypeFromText(trimmed);
                            var duration = ExtractDurationFromText(trimmed);
                            var language = ExtractLanguageFromText(trimmed);

                            programs.Add(new UniversityProgram
                            {
                                UniversityName = universityName,
                                Name = programName,
                                DegreeType = degreeType,
                                Duration = duration,
                                Language = language ?? "Bulgarian"
                            });
                        }
                    }
                }

                return programs.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Generic parsing strategy failed: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Fallback parsing from headings and links
        /// </summary>
        private void TryParseFallback(HtmlDocument doc, List<UniversityProgram> programs, string universityName)
        {
            try
            {
                var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");
                if (headings != null)
                {
                    foreach (var heading in headings)
                    {
                        var text = ExtractText(heading)?.Trim();
                        if (string.IsNullOrWhiteSpace(text) || text.Length < 5)
                            continue;

                        // Skip if it's clearly not a program
                        if (text.Contains("Университет", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("Контакти", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("Contact", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var programName = ExtractProgramName(text);
                        if (!string.IsNullOrWhiteSpace(programName))
                        {
                            var degreeType = ExtractDegreeTypeFromText(text);
                            
                            programs.Add(new UniversityProgram
                            {
                                UniversityName = universityName,
                                Name = programName,
                                DegreeType = degreeType,
                                Language = "Bulgarian"
                            });
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
        /// Extracts text from HTML node
        /// </summary>
        private string? ExtractText(HtmlNode node)
        {
            if (node == null)
                return null;

            var text = node.InnerText;
            text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ", RegexOptions.Compiled);
            
            return text.Trim();
        }

        /// <summary>
        /// Extracts program name from text
        /// </summary>
        private string ExtractProgramName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove common prefixes
            text = Regex.Replace(text, @"^(Програма\s*:?\s*|Специалност\s*:?\s*|Program\s*:?\s*)", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\([^)]*\)", ""); // Remove parentheses
            text = Regex.Replace(text, @"\s+", " ");
            
            return text.Trim();
        }

        /// <summary>
        /// Extracts degree type from HTML cells or document
        /// </summary>
        private string? ExtractDegreeType(HtmlNodeCollection? cells, HtmlDocument doc)
        {
            if (cells != null && cells.Count > 1)
            {
                var text = ExtractText(cells[1]);
                return ExtractDegreeTypeFromText(text ?? "");
            }
            
            return null;
        }

        /// <summary>
        /// Extracts degree type from text
        /// </summary>
        private string? ExtractDegreeTypeFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Bulgarian degree types
            if (text.Contains("Бакалавър", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Bachelor", StringComparison.OrdinalIgnoreCase))
                return "Bachelor";
                
            if (text.Contains("Магистър", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Master", StringComparison.OrdinalIgnoreCase))
                return "Master";
                
            if (text.Contains("Доктор", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("PhD", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Doctor", StringComparison.OrdinalIgnoreCase))
                return "PhD";

            return null;
        }

        /// <summary>
        /// Extracts duration from HTML cells or document
        /// </summary>
        private int? ExtractDuration(HtmlNodeCollection? cells, HtmlDocument doc)
        {
            if (cells != null && cells.Count > 1)
            {
                var text = ExtractText(cells[1]);
                return ExtractDurationFromText(text ?? "");
            }
            
            return null;
        }

        /// <summary>
        /// Extracts duration (in months) from text
        /// </summary>
        private int? ExtractDurationFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Look for years pattern (e.g., "4 години", "4 years")
            var yearMatch = Regex.Match(text, @"(\d+)\s*(години|years|год)", RegexOptions.IgnoreCase);
            if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var years))
            {
                return years * 12; // Convert to months
            }

            // Look for months pattern
            var monthMatch = Regex.Match(text, @"(\d+)\s*(месеца|months|мес)", RegexOptions.IgnoreCase);
            if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out var months))
            {
                return months;
            }

            // Look for semesters (1 semester ≈ 6 months)
            var semesterMatch = Regex.Match(text, @"(\d+)\s*(семестър|semesters|сем)", RegexOptions.IgnoreCase);
            if (semesterMatch.Success && int.TryParse(semesterMatch.Groups[1].Value, out var semesters))
            {
                return semesters * 6;
            }

            return null;
        }

        /// <summary>
        /// Extracts language from HTML cells
        /// </summary>
        private string? ExtractLanguage(HtmlNodeCollection? cells, HtmlDocument doc)
        {
            if (cells != null && cells.Count > 2)
            {
                var text = ExtractText(cells[2]);
                return ExtractLanguageFromText(text ?? "");
            }
            
            return null;
        }

        /// <summary>
        /// Extracts language from text
        /// </summary>
        private string? ExtractLanguageFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (text.Contains("English", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Английски", StringComparison.OrdinalIgnoreCase))
                return "English";
                
            if (text.Contains("Bulgarian", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Български", StringComparison.OrdinalIgnoreCase))
                return "Bulgarian";

            return null;
        }

        /// <summary>
        /// Normalizes and deduplicates programs
        /// </summary>
        private List<UniversityProgram> NormalizePrograms(List<UniversityProgram> programs, string universityName)
        {
            var normalized = new List<UniversityProgram>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var program in programs)
            {
                if (string.IsNullOrWhiteSpace(program.Name))
                    continue;

                var name = program.Name.Trim();
                name = Regex.Replace(name, @"\s+", " ");
                
                if (name.Length < 3)
                    continue;

                // Skip duplicates
                if (seenNames.Contains(name))
                    continue;

                seenNames.Add(name);

                normalized.Add(new UniversityProgram
                {
                    UniversityName = universityName,
                    Name = name,
                    DegreeType = program.DegreeType,
                    Duration = program.Duration,
                    Language = program.Language ?? "Bulgarian"
                });
            }

            normalized.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return normalized;
        }

        /// <summary>
        /// Generates URL slug from university name
        /// </summary>
        private string GenerateSlugFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Convert to lowercase and replace spaces with hyphens
            var slug = name.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9а-я\s-]", ""); // Remove special characters
            slug = Regex.Replace(slug, @"\s+", "-"); // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"-+", "-"); // Replace multiple hyphens with single
            
            return slug.Trim('-');
        }
    }
}

