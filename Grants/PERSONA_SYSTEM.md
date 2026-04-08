# Fighter Persona System — Design & Implementation Guide

## Overview

The **Fighter Persona System** is a framework for defining character-specific gameplay mechanics. Rather than fighters having identical rules, each fighter's persona determines:

- **How they play** — their strategic priorities and decision-making
- **What abilities they have** — unique actions unavailable to others
- **How they modify cards** — stat adjustments, stat swaps, alternative effects
- **How rounds resolve** — special combat rules, interrupts, traps, etc.

## Core Components

### 1. **FighterPersona** (Abstract Base Class)
Located in `Models/Fighter/FighterPersona.cs`

Defines the interface all personas must implement:

| Method | Purpose | When Called |
|--------|---------|-------------|
| `CreateRuntimeState()` | Initialize persona's state for a match | Fighter instance created |
| `ModifyCardSelection()` | Intercept/modify paired cards before commitment | Player selects or AI decides |
| `ModifyCardStat()` | Adjust power, defense, speed, movement | Stats calculated during resolution |
| `GetPersonalizedAiDecision()` | Provide custom AI strategy | AiEngine.SelectPair() |
| `OnRoundResolutionStart()` | Apply pre-attack effects (traps, setups) | Before attack phase |
| `OnRoundResolutionComplete()` | Apply post-attack effects (stacks, resets) | After attack phase |
| `UpdateState()` | Decay cooldowns, effects, timers | Each turn tick |
| `GetHudDisplayInfo()` | Return HUD display strings | UI rendering |

### 2. **PersonaState** (Runtime State Container)
Tracks all active persona effects:

```csharp
public class PersonaState
{
    Dictionary<string, int> AbilityCooldowns      // "ability_name" → turns_remaining
    Dictionary<string, int> Counters             // "stack_name" → count
    List<PersonaArenaEffect> ActiveEffects       // Traps, auras, markers
    Dictionary<string, bool> Flags               // One-time triggers
    Dictionary<string, object> CustomData        // Persona-specific data
}
```

### 3. **PersonaArenaEffect** (Board-Level Effect)
Represents location-based effects:
- **Trap placement** — hex coordinates where traps exist
- **Auras / zones** — area denial or buff regions
- **Projectiles** — moving effects across the board
- **Markers** — visual indicators of threat or opportunity

### 4. **StandardPersona** (Default Implementation)
All hook methods are no-ops. Use this as the fallback for fighters without special mechanics.

## Example Personas

### TrappingPersona – "Trapmaster"
**Concept:** Controls board through area denial.

**Key Abilities:**
- `place_trap` (2 turn cooldown) — Place hazard on hex
- `push` (1 turn cooldown) — Knock opponent away

**Mechanics:**
- Traps trigger when opponent moves through them
- Triggered trap: disables location, reduces speed, or knockbacks
- Traps expire after 3 turns if not triggered
- Persona modifies movement/range stats of trap-related cards

**HUD Display:**
```
Traps: 2
Push: Ready
```

### DisruptorPersona – "Disruptor"  
**Concept:** Turns opponent's aggression into openings.

**Key Abilities:**
- `interrupt` (varies) — Stop opponent after they commit cards
- `bait` (1 turn cooldown) — Tempt opponent to attack

**Mechanics:**
- Gain "interrupt stacks" from successful defensive plays
- Spend stacks to interrupt opponent (forces re-selection)
- Failed interrupts grant opponent bonus damage
- Stacks decay over time if not used

**HUD Display:**
```
Interrupt Stacks: 3/5
Interrupt Ready: Next Round
```

## Integration Points

### Wiring Personas into Core Systems

#### AiEngine.SelectPair()
```csharp
// Ask persona for custom AI decision
var personaChoice = ai.Definition.Persona.GetPersonalizedAiDecision(
    ai, opponent, board, ai.PersonaState);

if (personaChoice != null)
    return personaChoice;  // Use persona's choice

// Otherwise fall back to default greedy AI
```

#### ResolutionEngine.ResolveRound()
```csharp
// Call persona hooks before and after attack resolution
fa.Definition.Persona.OnRoundResolutionStart(
    round, match, fa, fb, fa.PersonaState);

// ... attack resolution ...

fa.Definition.Persona.OnRoundResolutionComplete(
    round, match, fa, fb, fa.PersonaState);
```

#### Card Stat Calculation
```csharp
int power = card.BasePower + upgrades;

// Let persona modify the stat
power = fighter.Definition.Persona.ModifyCardStat(
    card, StatType.Power, power, fighter, fighter.PersonaState);
```

#### Per-Turn Updates (In TickEngine or UpgradeEngine)
```csharp
match.FighterA.Definition.Persona.UpdateState(match.FighterA.PersonaState);
match.FighterB.Definition.Persona.UpdateState(match.FighterB.PersonaState);
```

#### UI Display (Game1.cs or FightScreen)
```csharp
var hudInfo = fighter.Definition.Persona.GetHudDisplayInfo(fighter.PersonaState);
foreach (var line in hudInfo)
    sb.DrawString(_font, line, pos, Color.Yellow);
```

## Creating a Custom Persona

### Step 1: Create the Class

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
        Name = "My Custom Fighter",
        Description = "Unique gameplay mechanic here"
    };

    private MyCustomPersona() { }

    public override PersonaState CreateRuntimeState()
    {
        var state = new PersonaState();
        state.SetAbilityCooldown("my_ability", 0);
        return state;
    }

    // Implement remaining abstract methods...
}
```

### Step 2: Assign to Fighter Definition

```csharp
public static FighterDefinition CreateMyFighterDefinition()
{
    return new FighterDefinition
    {
        Id = "mycustom",
        Name = "My Fighter",
        Persona = MyCustomPersona.Instance,  // ← Assign here
        // ... rest of definition
    };
}
```

### Step 3: Use Persona Hooks

- **For attack modifications**: Override `ModifyCardStat()`
- **For unique AI behavior**: Override `GetPersonalizedAiDecision()`
- **For combat rules changes**: Override `OnRoundResolutionStart()` / `Complete()`
- **For cooldown-based abilities**: Use `PersonaState.Cooldowns` and `UpdateState()`
- **For stacks/counters**: Use `PersonaState.Counters`
- **For board effects**: Use `PersonaState.ActiveEffects`

## Design Patterns

### Pattern 1: Ability Stack System (Disruptor)
```csharp
// Gain stacks on defense
state.Counters["interrupt_stacks"] = stacks + 1;

// Spend stacks on ability
if (state.Counters["interrupt_stacks"] >= 1)
{
    state.Counters["interrupt_stacks"]--;
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

### Pattern 3: Stat Modification (Armor, Speed Boost)
```csharp
public override int ModifyCardStat(CardBase card, StatType stat, int baseValue, ...)
{
    if (stat == StatType.Defense && card.Id.Contains("shield"))
        return baseValue + 3;  // Shield cards gain +3 defense
    return baseValue;
}
```

### Pattern 4: Conditional AI (Smart Engagement)
```csharp
public override CardPair? GetPersonalizedAiDecision(...)
{
    // Check opponent health
    int opponentDisabledLocations = opponent.LocationStates
        .Count(l => l.Value.IsDisabled);
    
    if (opponentDisabledLocations >= 1)
        return SelectFinisherPair();  // Go for KO
    else
        return null;  // Use default AI
}
```

## Testing Your Persona

1. **Unit Test Ability Logic:** Create tests for `ModifyCardStat()`, `GetPersonalizedAiDecision()`
2. **Integration Test:** Play a match against a persona-using AI, verify behavior
3. **HUD Display:** Confirm `GetHudDisplayInfo()` shows correct state
4. **State Decay:** Verify cooldowns/effects properly expire with `UpdateState()`

## Next Steps

To fully implement persona mechanics in your game:

1. **Integrate hooks into core engines** (see PersonaIntegrationExamples.cs)
2. **Create a second fighter** with a unique persona (e.g., Trapmaster or Disruptor)
3. **Add persona-specific upgrades** (tree nodes that boost persona abilities)
4. **Implement UI for persona abilities** (on-screen prompts, activation buttons)
5. **Design unique card distributions** per persona (some cards only available via persona)
6. **Add persona unlock progression** (early personas are simple; later ones are complex)

## References

- **Framework defn:** `Models/Fighter/FighterPersona.cs`
- **Examples:** `TrappingPersona.cs`, `DisruptorPersona.cs`
- **Integration:** `PersonaIntegrationExamples.cs`
- **Default impl:** `StandardPersona.cs`
