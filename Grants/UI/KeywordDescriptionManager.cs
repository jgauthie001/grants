using Grants.Models.Cards;
using System.Text.Json;

namespace Grants.UI;

/// <summary>
/// Manages custom keyword descriptions that can be edited in-game.
/// Stores custom descriptions and provides fallback to CardTooltip defaults.
/// </summary>
public class KeywordDescriptionManager
{
    private Dictionary<CardKeyword, string> _customDescriptions = new();
    private static readonly string SavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keyword_descriptions.json");

    public KeywordDescriptionManager()
    {
        // Initialize with default descriptions
        ResetToDefaults();
        // Try to load custom descriptions
        Load();
    }

    /// <summary>Reset all descriptions to defaults.</summary>
    public void ResetToDefaults()
    {
        _customDescriptions.Clear();
        var keywords = Enum.GetValues<CardKeyword>();
        foreach (var kw in keywords)
        {
            _customDescriptions[kw] = CardTooltip.GetKeywordDescription(kw);
        }
    }

    /// <summary>Get the current description for a keyword.</summary>
    public string GetDescription(CardKeyword keyword)
    {
        if (_customDescriptions.TryGetValue(keyword, out var desc))
            return desc;
        return CardTooltip.GetKeywordDescription(keyword);
    }

    /// <summary>Set a custom description for a keyword.</summary>
    public void SetDescription(CardKeyword keyword, string description)
    {
        _customDescriptions[keyword] = description;
    }

    /// <summary>Get all keywords.</summary>
    public List<CardKeyword> GetAllKeywords()
    {
        return Enum.GetValues<CardKeyword>().ToList();
    }

    /// <summary>Save custom descriptions to disk.</summary>
    public void Save()
    {
        try
        {
            var data = _customDescriptions
                .Where(kvp => kvp.Key != CardKeyword.None)  // Skip None
                .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save keyword descriptions: {ex.Message}");
        }
    }

    /// <summary>Load custom descriptions from disk.</summary>
    private void Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return;

            var json = File.ReadAllText(SavePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data == null) return;

            foreach (var kvp in data)
            {
                if (Enum.TryParse<CardKeyword>(kvp.Key, out var keyword))
                {
                    _customDescriptions[keyword] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load keyword descriptions: {ex.Message}");
        }
    }
}
