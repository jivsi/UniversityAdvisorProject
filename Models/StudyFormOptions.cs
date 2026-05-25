namespace UniversityFinder.Models;

/// <summary>
/// Allowed StudyForm values — must match Supabase check constraint universityprograms_studyform_chk.
/// </summary>
public static class StudyFormOptions
{
    public const string Regular = "Редовно";
    public const string PartTime = "Задочно";
    public const string RegularAndPartTime = "Редовно, Задочно";
    public const string Distance = "Дистанционно";

    public static readonly IReadOnlyList<string> All =
    [
        Regular,
        PartTime,
        RegularAndPartTime,
        Distance
    ];
}
