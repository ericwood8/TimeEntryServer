namespace TimeEntry.ApiService.Apis;

public static class StringExtensionMethods
{
    public static bool IsNameBad(this string name)
    {
        return string.IsNullOrWhiteSpace(name) || name.HasSpecialChars();
    }

    public static bool HasSpecialChars(this string s)
    {
        // the same rule as the Name annotation on the entities: letters, digits, spaces, apostrophes and hyphens
        return s.Any(ch => !char.IsLetterOrDigit(ch) && ch != ' ' && ch != '\'' && ch != '-');
    }
}