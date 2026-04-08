// INTEGRATION EXAMPLES: Wiring the Persona System into Core Engines
// These are reference implementations showing how to use the framework.
// Copy these into AiEngine.cs, ResolutionEngine.cs, etc.

// ============================================================================
// EXAMPLE 1: AiEngine — Persona-Aware Decision Making
// ============================================================================

public static Models.Cards.CardPair SelectPair(
    FighterInstance ai,
    FighterInstance opponent,
    Models.Board.HexBoard board)
{
    var validPairs = ai.GetValidPairs();
    if (validPairs.Count == 0)
        throw new InvalidOperationException($"AI fighter {ai.DisplayName} has no valid card pairs.");

    // === PERSONA HOOK: Allow persona to override selection ===
    var personaChoice = ai.Definition.Persona.GetPersonalizedAiDecision(
        ai, opponent, board, ai.PersonaState);
    
    if (personaChoice != null)
        return personaChoice; // Persona made the decision

    // === Default AI logic follows ===
    int distance = new Models.Board.HexCoord(ai.HexQ, ai.HexR)
        .DistanceTo(new Models.Board.HexCoord(opponent.HexQ, opponent.HexR));

    bool anyInRange = validPairs.Any(p => distance <= (int)p.EffectiveRange);
    if (!anyInRange)
    {
        return validPairs.OrderByDescending(p => p.CombinedMovement).First();
    }

    return validPairs
        .Where(p => distance <= (int)p.EffectiveRange)
        .OrderByDescending(p => p.CombinedPower)
        .ThenByDescending(p => p.CombinedSpeed)
        .First();
}

// ============================================================================
// EXAMPLE 2: ResolutionEngine — Persona-Driven Round Mechanics
// ============================================================================

public static RoundState ResolveRound(MatchState match)
{
    var pairA = match.SelectedPairA!;
    var pairB = match.SelectedPairB!;
    var fa = match.FighterA;
    var fb = match.FighterB;

    // ... (speed calculation, movement phase as before) ...

    var round = new RoundState { /* ... */ };

    // === PERSONA HOOKS: Before attack resolution ===
    // Allow both fighters' personas to modify round state before attacks
    fa.Definition.Persona.OnRoundResolutionStart(
        round, match, fa, fb, fa.PersonaState);
    
    fb.Definition.Persona.OnRoundResolutionStart(
        round, match, fb, fa, fb.PersonaState);

    // ===== ATTACK PHASE (as normal) =====
    // ... (resolve attacks normally) ...

    // === PERSONA HOOKS: After attack resolution ===
    fa.Definition.Persona.OnRoundResolutionComplete(
        round, match, fa, fb, fa.PersonaState);
    
    fb.Definition.Persona.OnRoundResolutionComplete(
        round, match, fb, fa, fb.PersonaState);

    return round;
}

// ============================================================================
// EXAMPLE 3: UpgradeEngine or TickEngine — Persona State Updates
// ============================================================================

// Called each turn to update persona state (decrement cooldowns, decay effects, etc.)
public static void UpdateFighterPersonas(MatchState match)
{
    match.FighterA.Definition.Persona.UpdateState(match.FighterA.PersonaState);
    match.FighterB.Definition.Persona.UpdateState(match.FighterB.PersonaState);
}

// ============================================================================
// EXAMPLE 4: Card Stat Resolution — Persona Modifications
// ============================================================================

// Modify this in AttackEngine.cs or FighterInstance.GetCardPower() etc.
public int GetCardPower(CardBase card, FighterInstance fighter = null!)
{
    int power = card.BasePower;

    // Apply upgrades
    if (fighter!.UpgradedCardPower.TryGetValue(card.Id, out int upgrade))
        power += upgrade;

    // === PERSONA HOOK: Modify stat based on persona ===
    if (fighter.Definition.Persona is not null)
    {
        power = fighter.Definition.Persona.ModifyCardStat(
            card, StatType.Power, power, fighter, fighter.PersonaState);
    }

    return power;
}

// ============================================================================
// EXAMPLE 5: UI / Game1.cs — Display Persona Info in HUD
// ============================================================================

private void DrawFighterPersonaInfo(SpriteBatch sb, FighterInstance fighter, int x, int y)
{
    // Draw persona name
    string personaLabel = $"Persona: {fighter.Definition.Persona.Name}";
    sb.DrawString(_font, personaLabel, new Vector2(x, y), Color.Yellow);
    y += 20;

    // Draw persona-specific HUD info
    var hudInfo = fighter.Definition.Persona.GetHudDisplayInfo(fighter.PersonaState);
    foreach (var info in hudInfo)
    {
        sb.DrawString(_smallFont, info, new Vector2(x, y), Color.LimeGreen);
        y += 14;
    }
}

// ============================================================================
// QUICK START: Creating a New Persona
// ============================================================================

/*
1. Create a new file: Models/Fighter/MyCustomPersona.cs
2. Inherit from FighterPersona
3. Implement abstract methods:
   - CreateRuntimeState() → initialize PersonaState with abilities/counters
   - ModifyCardSelection() → intercept/modify pair selection
   - GetPersonalizedAiDecision() → return null to use default AI, or override
   - OnRoundResolutionStart() → apply pre-attack effects
4. Override virtual methods as needed:
   - ModifyCardStat() → adjust power/defense/speed/movement
   - OnRoundResolutionComplete() → apply post-attack effects
   - UpdateState() → decrement cooldowns, decay effects
   - GetHudDisplayInfo() → custom HUD display
5. Create a public static Instance: `public static readonly MyPersona Instance = new() { ... };`
6. Assign to FighterDefinition: `Persona = MyCustomPersona.Instance`

Example stub:

    public class MyCustomPersona : FighterPersona
    {
        public static readonly MyCustomPersona Instance = new()
        {
            PersonaId = "mycustom",
            Name = "Custom Fighter",
            Description = "Special ability description here"
        };

        public override PersonaState CreateRuntimeState() => new PersonaState();
        public override CardPair ModifyCardSelection(CardPair pair, ...) => pair;
        public override CardPair? GetPersonalizedAiDecision(...) => null;
        public override void OnRoundResolutionStart(...) { }
    }
*/
