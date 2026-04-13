namespace Grants.Models.Fighter;

/// <summary>
/// A single option in a pre-round persona self-choice list.
/// </summary>
public record PersonaChoiceOption(string Id, string Label, string Description = "");

/// <summary>
/// Returned by a persona when it wants the owner to pick from a list of options
/// before card selection. Replaces the old mutation/evolution-specific virtual methods
/// with a single generic protocol. Adding new choice-based personas no longer requires
/// changes to FightScreen — only the persona itself and its options need to be defined.
/// </summary>
public class PersonaChoiceRequest
{
    /// <summary>One-line description shown above the option list.</summary>
    public string Prompt { get; init; } = "";

    /// <summary>The selectable options. Must be non-empty unless CanSkip is true.</summary>
    public List<PersonaChoiceOption> Options { get; init; } = new();

    /// <summary>
    /// If true, show a [Backspace] skip hint and allow the player to pass without selecting.
    /// If false, the choice is mandatory.
    /// </summary>
    public bool CanSkip { get; init; } = false;

    /// <summary>RGB tint for the choice screen header (avoids a MonoGame dependency in the model layer).</summary>
    public (int R, int G, int B) HeaderTint { get; init; } = (180, 120, 220);
}
