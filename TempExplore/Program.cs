using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

class Program
{
    static async Task Main()
    {
        string supabaseUrl = "https://wbvrgmryycgnjodcmlld.supabase.co";
        string apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndidnJnbXJ5eWNnbmpvZGNtbGxkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQwOTE2MDAsImV4cCI6MjA3OTY2NzYwMH0.HD6eP04fK4yjQiQoTP2pDUMc04ubBwW-E6zias_1WyU";
        string sofiaUniId = "f6f08bf1-0a91-4818-b01e-71a5f008541b";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        client.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var programsData = new List<(string Name, string CategoryId)>
        {
            ("Българска филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Английска филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Немска филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Френска филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Испанска филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Италианска филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Руски език и литература", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Класическа филология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("История", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Археология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Философия", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Културология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Теология", "f9eef5a6-6abf-456d-ba0e-f21e2c534fa1"),
            ("Право", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Психология", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Социология", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Политология", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Публична администрация", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Международни отношения", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Журналистика", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Връзки с обществеността", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Икономика", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Стопанско управление", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Счетоводство, финанси и контрол", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Бизнес администрация", "66aee80c-fbd1-40fe-a299-bdf6df38c656"),
            ("Компютърни науки", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Софтуерно инженерство", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Информатика", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Информационни системи", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Математика", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Приложна математика", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Статистика", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Физика", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Астрономия", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Химия", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Биология", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Биотехнологии", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("География", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Геология", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Екология и опазване на околната среда", "8eea2cbe-04f5-40f4-95dd-ae7dc42c5fcc"),
            ("Начална училищна педагогика", "9d41c6a3-e21f-4f89-b800-343490a2b7d1"),
            ("Предучилищна педагогика", "9d41c6a3-e21f-4f89-b800-343490a2b7d1"),
            ("Специална педагогика", "9d41c6a3-e21f-4f89-b800-343490a2b7d1")
        };

        Console.WriteLine($"Clearing existing programs for Sofia University ({sofiaUniId})...");
        var deleteResponse = await client.DeleteAsync($"{supabaseUrl}/rest/v1/UniversityPrograms?UniversityId=eq.{sofiaUniId}");
        Console.WriteLine($"Delete status: {deleteResponse.StatusCode}");

        foreach (var p in programsData)
        {
            string subjectId = null;
            try
            {
                // 1. Find or Create Subject
                string subjectUrl = $"{supabaseUrl}/rest/v1/Subjects?name=eq.{Uri.EscapeDataString(p.Name)}&select=Id,CategoryId";
                var subjectResponse = await client.GetAsync(subjectUrl);
                var subjectJson = await subjectResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(subjectJson);
                
                if (doc.RootElement.GetArrayLength() == 0)
                {
                    Console.WriteLine($"Creating Subject: {p.Name}");
                    var newSubject = new { name = p.Name, CategoryId = p.CategoryId };
                    var content = new StringContent(JsonSerializer.Serialize(newSubject), Encoding.UTF8, "application/json");
                    var postResponse = await client.PostAsync($"{supabaseUrl}/rest/v1/Subjects?select=Id", content);
                    var postJson = await postResponse.Content.ReadAsStringAsync();
                    using var postDoc = JsonDocument.Parse(postJson);
                    subjectId = postDoc.RootElement[0].GetProperty("Id").GetString();
                }
                else
                {
                    subjectId = doc.RootElement[0].GetProperty("Id").GetString();
                    // Update CategoryId if missing
                    if (doc.RootElement[0].GetProperty("CategoryId").ValueKind == JsonValueKind.Null)
                    {
                        Console.WriteLine($"Updating Category for Subject: {p.Name}");
                        var updateSubject = new { CategoryId = p.CategoryId };
                        var patchContent = new StringContent(JsonSerializer.Serialize(updateSubject), Encoding.UTF8, "application/json");
                        await client.PatchAsync($"{supabaseUrl}/rest/v1/Subjects?Id=eq.{subjectId}", patchContent);
                    }
                }

                // 2. Create University Program
                Console.WriteLine($"Adding Program: {p.Name}");
                var newProgram = new 
                { 
                    UniversityId = sofiaUniId, 
                    SubjectId = subjectId, 
                    DegreeType = "Бакалавър"
                };
                var programContent = new StringContent(JsonSerializer.Serialize(newProgram), Encoding.UTF8, "application/json");
                var progResponse = await client.PostAsync($"{supabaseUrl}/rest/v1/UniversityPrograms", programContent);
                if (!progResponse.IsSuccessStatusCode)
                {
                    var err = await progResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to add program {p.Name}: {err}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {p.Name}: {ex.Message}");
            }
        }

        Console.WriteLine("Sync completed!");
    }
}
