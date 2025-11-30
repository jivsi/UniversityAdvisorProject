using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using UniversityFinder.Services;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for inferring subjects/programs from university names and descriptions
    /// using multilingual keyword matching. This enables search functionality even when
    /// HEI API does not return program data.
    /// </summary>
    public class SubjectInferenceService : ISubjectInferenceService
    {
        private readonly ILogger<SubjectInferenceService> _logger;
        
        /// <summary>
        /// Multilingual keyword mappings for subject detection
        /// Each subject has keywords in multiple languages for comprehensive detection
        /// </summary>
        private static readonly Dictionary<string, List<string>> SubjectKeywords = new()
        {
            // Engineering / Ingénierie / Ingegneria / Ingeniería
            ["Engineering"] = new List<string>
            {
                // English
                "engineering", "engineer", "ingenieur", "ingenier", "ingénier", "ingegneria",
                "ingenieria", "ingeniería", "technik", "technische", "polytechnic", "polytechnique",
                "politecnico", "politechnika", "institut technologie", "technology institute",
                "institute technology", "école ingénieur", "escuela ingeniería"
            },
            
            // Medicine / Médecine / Medicina
            ["Medicine"] = new List<string>
            {
                // English
                "medicine", "medical", "médicine", "médecine", "medicina", "medizin",
                "medical school", "school medicine", "école médecine", "faculté médecine",
                "faculty medicine", "università medicina", "universidad medicina",
                "medical university", "université médecine", "chirurgia", "chirurgie", "surgery"
            },
            
            // Law / Droit / Diritto / Derecho
            ["Law"] = new List<string>
            {
                // English
                "law", "legal", "droit", "diritto", "derecho", "recht", "jurisprudence",
                "law school", "école droit", "faculté droit", "faculty law",
                "università giurisprudenza", "universidad derecho", "law university"
            },
            
            // Computer Science / Informatique / Informatica
            ["Computer Science"] = new List<string>
            {
                // English
                "computer science", "computing", "informatics", "informatique", "informatica",
                "informática", "informatik", "computer engineering", "software engineering",
                "programming", "computer technology", "information technology", "it",
                "computer science", "école informatique", "faculté informatique"
            },
            
            // Economics / Économie / Economía
            ["Economics"] = new List<string>
            {
                // English
                "economics", "economy", "économie", "economía", "wirtschaft", "ekonomi",
                "economic science", "sciences économiques", "ciencias económicas",
                "faculty economics", "faculté économie", "school economics"
            },
            
            // Architecture / Architecture / Architettura
            ["Architecture"] = new List<string>
            {
                // English
                "architecture", "architectural", "architectura", "architettura", "architektur",
                "school architecture", "école architecture", "faculté architecture",
                "faculty architecture", "università architettura", "universidad arquitectura"
            },
            
            // Business / Commerce / Affaires
            ["Business"] = new List<string>
            {
                // English
                "business", "commerce", "management", "affaires", "negocios", "business school",
                "école commerce", "school business", "faculté gestion", "faculty management",
                "école gestion", "management school", "mba", "universidad negocios"
            },
            
            // Education / Éducation / Educación
            ["Education"] = new List<string>
            {
                // English
                "education", "pedagogy", "éducation", "educación", "pädagogik", "pedagogia",
                "teaching", "teacher training", "formation enseignants", "teacher education",
                "faculty education", "faculté éducation", "école normale", "normal school"
            },
            
            // Psychology / Psychologie / Psicología
            ["Psychology"] = new List<string>
            {
                // English
                "psychology", "psychologie", "psicología", "psychologie", "psicologia",
                "faculty psychology", "faculté psychologie", "school psychology",
                "department psychology"
            },
            
            // Pharmacy / Pharmacie / Farmacia
            ["Pharmacy"] = new List<string>
            {
                // English
                "pharmacy", "pharmacie", "farmacia", "farmacia", "pharmazie",
                "school pharmacy", "école pharmacie", "faculté pharmacie",
                "faculty pharmacy", "pharmaceutical"
            },
            
            // Nursing / Soins infirmiers / Enfermería
            ["Nursing"] = new List<string>
            {
                // English
                "nursing", "soins infirmiers", "enfermería", "enfermagem", "pflege",
                "nurse", "nursing school", "école infirmière", "school nursing"
            },
            
            // Mathematics / Mathématiques / Matemáticas
            ["Mathematics"] = new List<string>
            {
                // English
                "mathematics", "math", "maths", "mathématiques", "matemáticas", "matematica",
                "mathematik", "faculty mathematics", "faculté mathématiques", "department mathematics"
            },
            
            // Physics / Physique / Física
            ["Physics"] = new List<string>
            {
                // English
                "physics", "physique", "física", "fisica", "physik", "faculty physics",
                "faculté physique", "department physics", "institute physics"
            },
            
            // Chemistry / Chimie / Química
            ["Chemistry"] = new List<string>
            {
                // English
                "chemistry", "chimie", "química", "chimica", "chemie", "faculty chemistry",
                "faculté chimie", "department chemistry", "school chemistry"
            },
            
            // Technology / Technologie / Tecnología
            ["Technology"] = new List<string>
            {
                // English
                "technology", "technologie", "tecnología", "tecnologia", "technik",
                "institute technology", "institut technologie", "technology university",
                "université technologie", "universidad tecnología"
            },
            
            // Agriculture / Agriculture / Agricultura
            ["Agriculture"] = new List<string>
            {
                // English
                "agriculture", "agriculture", "agricultura", "agricoltura", "landwirtschaft",
                "agricultural", "agronomy", "agronomie", "school agriculture",
                "faculty agriculture", "faculté agriculture"
            },
            
            // Political Science / Sciences politiques / Ciencias políticas
            ["Political Science"] = new List<string>
            {
                // English
                "political science", "sciences politiques", "ciencias políticas", "politologia",
                "political studies", "études politiques", "government", "public administration",
                "faculty political science", "faculté sciences politiques"
            },
            
            // Social Sciences / Sciences sociales / Ciencias sociales
            ["Social Sciences"] = new List<string>
            {
                // English
                "social sciences", "sciences sociales", "ciencias sociales", "social science",
                "sociology", "sociologie", "sociología", "anthropology", "anthropologie",
                "faculty social sciences", "faculté sciences sociales"
            },
            
            // Fine Arts / Beaux-arts / Bellas artes
            ["Fine Arts"] = new List<string>
            {
                // English
                "fine arts", "beaux-arts", "bellas artes", "belle arti", "kunst",
                "arts", "art school", "école beaux-arts", "academy arts", "académie arts",
                "school arts", "faculty arts", "faculté arts"
            }
        };

        public SubjectInferenceService(ILogger<SubjectInferenceService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Analyzes university name and description to infer relevant subjects
        /// </summary>
        public Task<List<string>> InferSubjectsAsync(string universityName, string? universityDescription = null)
        {
            if (string.IsNullOrWhiteSpace(universityName))
            {
                _logger.LogWarning("Cannot infer subjects: university name is empty");
                return Task.FromResult(new List<string>());
            }

            // Combine name and description for analysis
            var textToAnalyze = universityName;
            if (!string.IsNullOrWhiteSpace(universityDescription))
            {
                textToAnalyze = $"{universityName} {universityDescription}";
            }

            // Normalize text: remove accents, lowercase, trim
            var normalizedText = NormalizeText(textToAnalyze);
            
            _logger.LogDebug($"Analyzing text for subject inference: {universityName}");
            _logger.LogDebug($"Normalized text: {normalizedText}");

            var inferredSubjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Check each subject's keywords against the normalized text
            foreach (var subjectEntry in SubjectKeywords)
            {
                var subjectName = subjectEntry.Key;
                var keywords = subjectEntry.Value;

                foreach (var keyword in keywords)
                {
                    var normalizedKeyword = NormalizeText(keyword);
                    
                    // Check if keyword appears in the text (word boundary aware)
                    if (ContainsWord(normalizedText, normalizedKeyword))
                    {
                        inferredSubjects.Add(subjectName);
                        _logger.LogDebug($"Matched keyword '{keyword}' for subject '{subjectName}'");
                        break; // Found a match for this subject, move to next
                    }
                }
            }

            var result = inferredSubjects.ToList();
            
            if (result.Count > 0)
            {
                _logger.LogInformation($"Inferred {result.Count} subject(s) for '{universityName}': {string.Join(", ", result)}");
            }
            else
            {
                _logger.LogInformation($"No subjects inferred for '{universityName}' - no matching keywords found");
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Normalizes text for matching: removes accents, converts to lowercase, trims punctuation
        /// </summary>
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Remove accents/diacritics
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                // Keep only letters, digits, and spaces
                if (unicodeCategory == UnicodeCategory.LowercaseLetter ||
                    unicodeCategory == UnicodeCategory.UppercaseLetter ||
                    unicodeCategory == UnicodeCategory.DecimalDigitNumber ||
                    unicodeCategory == UnicodeCategory.SpaceSeparator)
                {
                    sb.Append(c);
                }
                else if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                {
                    // Skip diacritics (accents)
                    continue;
                }
                // Replace other punctuation with space
                else if (char.IsPunctuation(c) || char.IsSymbol(c))
                {
                    sb.Append(' ');
                }
            }

            // Convert to lowercase and normalize whitespace
            var result = sb.ToString()
                .ToLowerInvariant()
                .Trim();
            
            // Replace multiple spaces with single space
            result = Regex.Replace(result, @"\s+", " ");
            
            return result;
        }

        /// <summary>
        /// Checks if the text contains the keyword as a whole word (not just as substring)
        /// </summary>
        private static bool ContainsWord(string text, string keyword)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
            {
                return false;
            }

            // Exact match
            if (text == keyword)
            {
                return true;
            }

            // Check if keyword appears as a whole word (word boundary)
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
        }
    }
}

