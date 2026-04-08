# Persona Integration Examples

Reference implementations showing how to wire the persona system into core engines.

## Example 1: AiEngine — Persona-Aware Decision Making

```csharp
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
```

## Example 2: ResolutionEngine — Persona-Driven Round Mechanics

```csharp
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
```

## Example 3: UpgradeEngine or TickEngine — Persona State Updates

Called each turn to update persona state (decrement cooldowns, decay effects, etc.):

```csharp
public static void UpdateFighterPersonas(MatchState match)
{
    match.FighterA.Definition.Persona.UpdateState(match.FighterA.PersonaState);
    match.FighterB.Definition.Persona.UpdateState(match.FighterB.PersonaState);
}
```

## Example 4: Card Stat Resolution — Persona Modifications

Modify this in `AttackEngine.cs` or `FighterInstance.GetCardPower()`:

```csharp
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
```

## Example 5: UI / Game1.cs — Display Persona Info in HUD

```csharp
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
```

## Quick Start: Creating a New Persona

### 1. Create the Class

```csharp
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Match;

namespace Grants.Models.Fighter;

public class MyCustomPersona : FighterPersona
{
    public static readonly MyCustomPersona Instance = new()
    {
        PersonaId = "mycustom",
        Name = "Custom Fighter",
        Description = "Special ability description here"
    };

    private MyCustomPersona() { }

    public override PersonaState CreateRuntimeState() => new PersonaState();
    
    public override CardPair ModifyCardSelection(CardPair pair, ...) => pair;
    
    public override CardPair? GetPersonalizedAiDecision(...) => null;
    
    public override void OnRoundResolutionStart(...) { }
}
```

### 2. Assign to Fighter Definition

```csharp
Persona = MyCustomPersona.Instance
```

### 3. Use Persona Hooks

- **Customize AI strategies:** Override `GetPersonalizedAiDecision()`
- **Modify card stats:** Override `ModifyCardStat()`
- **Combat rules changes:** Override `OnRoundResolutionStart()` / `Complete()`
- **Ability management:** Use `PersonaState.Cooldowns`, `PersonaState.Counters`
- **Board effects:** Use `PersonaState.ActiveEffects`

## Pattern Examples

### Pattern 1: Ability Stack System (Disruptor)

```csharp
// Gain stacks on defense
state.Counters["stacks"] = stacks + 1;

// Spend stacks on ability
if (state.Counters["stacks"] >= 1)
{
    state.Counters["stacks"]--;
    // Trigger effect
}
```

### Pattern 2: Board Effects (Trapper)

```csharp
// Place effect on board
var effect = new PersonaArenaEffect
{
    EffectType = "trap",
    Position = targetHex,
    TurnsRemaining = 3
};
state.ActiveEffects.Add(effect);

// Check for trigger
foreach (var trap in state.ActiveEffects)
{
    if (trap.Position == opponentPos)
        TriggerTrap(trap);
}
```

### Pattern 3: Stat Modification

```csharp
public override int ModifyCardStat(CardBase card, StatType stat, int baseValue, ...)
{
    if (stat == StatType.Defense && card.Id.Contains("shield"))
        return baseValue + 3;  // Shield cards gain +3 defense
    return baseValue;
}
```

### Pattern 4: Conditional AI

```csharp
public override CardPair? GetPersonalizedAiDecision(...)
{
    // Check opponent health
    int disabledLocations = opponent.LocationStates
        .Count(l => l.Value.IsDisabled);
    
    if (disabledLocations >= 1)
        return SelectFinisherPair();  // Go for KO
    else
        return null;  // Use default AI
}
```
