using Grants.Models.Cards;

namespace Grants.Models.Fighter;

/// <summary>
/// Live instance of a fighter during a match. Tracks all runtime state.
/// Created fresh from FighterDefinition at match start, modified during play.
/// </summary>
public class FighterInstance
{
    public FighterDefinition Definition { get; init; } = null!;
    public string DisplayName { get; init; } = string.Empty;

    // --- Position on hex board ---
    public int HexQ { get; set; }
    public int HexR { get; set; }
    public int HexFacing { get; set; } = 0; // 0-5, clockwise from top

    // --- Damage state per location ---
    public Dictionary<BodyLocation, LocationState> LocationStates { get; private set; } = new();

    // --- Card cooldown tracking (CardId → turns remaining) ---
    public Dictionary<string, int> Cooldowns { get; private set; } = new();

    // --- Bleed/status from keywords ---
    public int BleedStacks { get; set; } = 0;  // Global bleed (applies to next hit location)
    public int StaggerTurnsRemaining { get; set; } = 0; // Cooldowns +1 when > 0

    // --- Applied upgrade modifiers (from FighterProgress) ---
    public Dictionary<string, int> UpgradedCardPower { get; set; } = new();
    public Dictionary<string, int> UpgradedCardDefense { get; set; } = new();
    public Dictionary<string, int> UpgradedCardSpeed { get; set; } = new();
    public Dictionary<string, int> UpgradedCardMovement { get; set; } = new();
    public Dictionary<string, int> UpgradedCardCooldownReduction { get; set; } = new();
    public Dictionary<string, List<CardKeywordValue>> UpgradedCardKeywords { get; set; } = new();

    // --- Passive item effects (from upgrade tree items) ---
    public List<string> ActiveItemIds { get; set; } = new();

    // --- Persona-specific runtime state ---
    public PersonaState PersonaState { get; set; } = null!;

    // --- Round-scoped stat modifiers (set by persona/stage choice hooks, cleared at StartNewRound) ---
    public int RoundPowerModifier { get; set; } = 0;
    public int RoundSpeedModifier { get; set; } = 0;

    // --- Active immunities for this round (populated by OnRoundResolutionStart, cleared at round start) ---
    public HashSet<CombatImmunity> ActiveImmunities { get; } = new();

    public FighterInstance(FighterDefinition definition, string displayName = "")
    {
        Definition = definition;
        DisplayName = string.IsNullOrEmpty(displayName) ? definition.Name : displayName;
        PersonaState = definition.Persona.CreateRuntimeState();

        foreach (BodyLocation loc in Enum.GetValues<BodyLocation>())
            LocationStates[loc] = new LocationState { Location = loc };
    }

    /// <summary>Returns available generic cards this turn (not on cooldown, location not disabled).</summary>
    public List<GenericCard> GetAvailableGenerics()
    {
        var available = new List<GenericCard>();
        foreach (var card in Definition.GenericCards)
        {
            var loc = BodyPartToLocation(card.BodyPart);
            if (!LocationStates[loc].IsAvailable) continue;
            if (GetCooldown(card.Id) > 0) continue;
            available.Add(card);
        }
        return available;
    }

    /// <summary>Returns available unique cards this turn (not on cooldown).</summary>
    public List<UniqueCard> GetAvailableUniques()
    {
        return Definition.UniqueCards
            .Where(c => GetCooldown(c.Id) <= 0)
            .ToList();
    }

    /// <summary>Returns available special cards this turn (not on cooldown).</summary>
    public List<SpecialCard> GetAvailableSpecials()
    {
        return Definition.SpecialCards
            .Where(c => GetCooldown(c.Id) <= 0)
            .ToList();
    }

    /// <summary>Returns valid card pairs given currently available generics and uniques/specials.</summary>
    public List<CardPair> GetValidPairs()
    {
        var pairs = new List<CardPair>();
        var generics = GetAvailableGenerics();
        var uniques = GetAvailableUniques();
        var specials = GetAvailableSpecials();

        foreach (var g in generics)
        {
            foreach (var u in uniques)
            {
                if (CanPair(g, u))
                    pairs.Add(new CardPair { Generic = g, Unique = u });
            }
            foreach (var s in specials)
            {
                pairs.Add(new CardPair { Generic = g, Special = s });
            }
        }

        // Standalone specials
        foreach (var s in specials.Where(s => s.Standalone))
            pairs.Add(new CardPair { Special = s });

        return pairs;
    }

    public bool CanPair(GenericCard generic, UniqueCard unique)
    {
        if (unique.ForbiddenBodyParts.Contains(generic.BodyPart)) return false;
        if (unique.RequiredBodyTags.Count == 0) return true;
        return unique.RequiredBodyTags.Any(tag => generic.SatisfiesTags.Contains(tag));
    }

    public int GetCooldown(string cardId) =>
        Cooldowns.TryGetValue(cardId, out int cd) ? cd : 0;

    public void SetCooldown(string cardId, int turns) =>
        Cooldowns[cardId] = turns;

    public void TickCooldowns()
    {
        foreach (var key in Cooldowns.Keys.ToList())
        {
            if (Cooldowns[key] > 0)
                Cooldowns[key]--;
        }
        if (StaggerTurnsRemaining > 0)
            StaggerTurnsRemaining--;
    }

    /// <summary>Gets the effective (upgraded) power for a card.</summary>
    public int GetCardPower(CardBase card) =>
        card.BasePower - DamageStatPenalty.PowerPenalty(GetLocationState(card))
        + (UpgradedCardPower.TryGetValue(card.Id, out int p) ? p : 0);

    public int GetCardDefense(CardBase card) =>
        card.BaseDefense - DamageStatPenalty.DefensePenalty(GetLocationState(card))
        + (UpgradedCardDefense.TryGetValue(card.Id, out int d) ? d : 0);

    public int GetCardSpeed(CardBase card) =>
        card.BaseSpeed - DamageStatPenalty.SpeedPenalty(GetLocationState(card))
        + (UpgradedCardSpeed.TryGetValue(card.Id, out int s) ? s : 0);

    public int GetCardMovement(CardBase card) =>
        card.MaxMovement + (UpgradedCardMovement.TryGetValue(card.Id, out int m) ? m : 0);

    public int GetCardCooldown(CardBase card)
    {
        int reduction = UpgradedCardCooldownReduction.TryGetValue(card.Id, out int r) ? r : 0;
        return Math.Max(0, card.BaseCooldown - reduction);
    }

    public List<CardKeywordValue> GetCardKeywords(CardBase card)
    {
        var kws = new List<CardKeywordValue>(card.Keywords);
        if (UpgradedCardKeywords.TryGetValue(card.Id, out var extra))
            kws.AddRange(extra);
        return kws;
    }

    /// <summary>Returns the location state relevant to a card (body-part cards use their own location).</summary>
    private DamageState GetLocationState(CardBase card)
    {
        if (card is GenericCard gc)
            return LocationStates[BodyPartToLocation(gc.BodyPart)].State;
        return DamageState.Healthy; // Unique/Special cards don't have a direct location
    }

    public bool IsKnockedOut()
    {
        int disabledCriticals = Definition.CriticalLocations
            .Count(loc => LocationStates[loc].State == DamageState.Disabled);
        return disabledCriticals >= Definition.KOThreshold;
    }

    public static BodyLocation BodyPartToLocation(Cards.BodyPart part) => part switch
    {
        Cards.BodyPart.Head    => BodyLocation.Head,
        Cards.BodyPart.Torso   => BodyLocation.Torso,
        Cards.BodyPart.LeftArm => BodyLocation.LeftArm,
        Cards.BodyPart.RightArm=> BodyLocation.RightArm,
        Cards.BodyPart.LeftLeg => BodyLocation.LeftLeg,
        Cards.BodyPart.RightLeg=> BodyLocation.RightLeg,
        Cards.BodyPart.Core    => BodyLocation.Core,
        Cards.BodyPart.Stance  => BodyLocation.Stance,
        _ => BodyLocation.Torso,
    };
}
