namespace Grants.Models.Cards;

/// <summary>
/// A keyword with an optional numeric value (e.g., "Bleed" with value 2).
/// </summary>
public class CardKeywordValue
{
    public CardKeyword Keyword { get; set; }
    public int Value { get; set; } = 1;

    public CardKeywordValue() { }

    public CardKeywordValue(CardKeyword keyword, int value = 1)
    {
        Keyword = keyword;
        Value = value;
    }

    public override string ToString()
    {
        if (Value > 1)
            return $"{Keyword} {Value}";
        return Keyword.ToString();
    }
}

/// <summary>
/// Extension methods for working with keyword values in lists and collections.
/// </summary>
public static class CardKeywordValueExtensions
{
    /// <summary>
    /// Check if a keyword type exists in a list of CardKeywordValue.
    /// </summary>
    public static bool ContainsKeyword(this IEnumerable<CardKeywordValue> keywords, CardKeyword keyword)
        => keywords.Any(kw => kw.Keyword == keyword);

    /// <summary>
    /// Get the value of a specific keyword, or 0 if not found.
    /// </summary>
    public static int GetKeywordValue(this IEnumerable<CardKeywordValue> keywords, CardKeyword keyword)
        => keywords.FirstOrDefault(kw => kw.Keyword == keyword)?.Value ?? 0;

    /// <summary>
    /// Get all keywords as enum values (ignoring the numeric values).
    /// </summary>
    public static IEnumerable<CardKeyword> GetKeywordTypes(this IEnumerable<CardKeywordValue> keywords)
        => keywords.Select(kw => kw.Keyword).Distinct();
}
