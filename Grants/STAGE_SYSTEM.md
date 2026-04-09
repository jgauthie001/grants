# Stage Modifier System — Design & Implementation Guide

## Overview

The **Stage Modifier System** defines environmental effects that change combat rules and dynamics. Stages persist across all rounds and can:

- **Modify arena layout** (full board, shrinking, custom shapes)
- **Apply hazards** (damage zones, knockback areas, slow zones)
- **Enforce mechanics** (push fighters inward, spawn effects, dynamic boundaries)
- **Customize AI** (stages can suggest special strategies)
- **Affect match progression** (time pressure, escalating danger)

Think of stages like **Tekken BGM interactions** or **game environment effects** — they add atmosphere and strategic variation without changing core card combat rules.

---

## Core Components

### 1. **StageModifier** (Abstract Base Class)
Located in `Models/Stage/StageModifier.cs`

Defines the interface all stages must implement:

| Method | Purpose | When Called |
|--------|---------|-------------|
| `CreateBoard()` | Define custom board layout | Match initialization |
| `CreateRuntimeState()` | Initialize stage state | Fighter instance created |
| `OnRoundStart()` | Apply start-of-round effects | Before card selection |
| `OnFighterMovementComplete()` | Apply position-based effects | After fighter moves |
| `OnAttackPhaseStart()` | Modify attack parameters | Before attacks resolve |
| `OnRoundComplete()` | Apply end-of-round effects | After attacks resolve |
| `GetHazardousHexes()` | Return dangerous cells for UI | During HUD rendering |
| `GetHudDisplayInfo()` | Return stage state display | During HUD rendering |

### 2. **StageState** (Runtime State Container)
Tracks all dynamic stage effects:

```csharp
public class StageState
{
    int TurnCount                          // Round counter
    Dictionary<string, int> EffectTimers   // Effect timers
    Dictionary<HexCoord, string> HazardMap // Hazard positions
    HashSet<HexCoord> RestrictedCells      // Invalid cells
    Dictionary<string, bool> Flags         // Mode toggles
    Dictionary<string, object> CustomData  // Extensible storage
}
```

### 3. **HazardZone** (Board Effect)
Represents a single hazard instance:

```csharp
public class HazardZone
{
    HexCoord Position              // Where the hazard is
    HazardType Type                // Damage, Stun, Knockback, Ice, Fire, etc.
    int Intensity                  // Damage amount / duration
    int TurnsRemaining             // When it expires (-1 = permanent)
}
```

### 4. **HazardType** (Enum)
Categorizes hazard effects:
- `Damage` — Direct HP damage on entry
- `Stun` — Reduces speed / prevents action
- `Knockback` — Pushes fighter away from hazard
- `OutOfBounds` — Penalty zone or boundary
- `Ice` — Reduces movement range
- `Fire` — Persistent damage each turn
- `Trap` — Similar to persona traps but arena-wide

---

## Example Stages

### StandardStage – "Training Arena"
**Default stage.** No special mechanics; pure balanced combat.
```csharp
Persona = StandardStage.Instance
```

### ShrinkingArenaStage – "Collapsing Arena"
**Concept:** Arena shrinks inward each round.

**Timeline:**
- Rounds 1-3: Full 7x7 hex grid (radius 3)
- Rounds 4-6: Shrink 1 layer (radius 2)
- Rounds 7+: Central region only (radius 1)

**Mechanics:**
- Fighters in invalid cells each turn are pushed to nearest valid cell
- Push applies speed penalty (stagger)
- Forces engagement; no "run and hide" strategy

**HUD Display:**
```
Arena Radius: 2/3
Round: 5
```

### PushZoneStage – "Unstable Ground"
**Concept:** Ground shifts each round, pushing fighters toward center.

**Effect Per Round:**
- After attacks resolve, both fighters are knocked back 1 hex toward center
- No damage, but forces constant repositioning
- Can't maintain distance indefinitely

**Mechanics:**
- Find neighbor cell closest to center that's valid and unoccupied
- Move fighter there
- Log the knockback event

**HUD Display:**
```
Stage: Constant Knockback Each Round
```

### HazardZoneStage – "Minefield"
**Concept:** Random hazards spawn each round; players must navigate carefully.

**Mechanics:**
- Each round spawn 1-2 random hazards
- Hazards persist for 2 turns then despawn
- Multiple hazard types: Damage, Stun, Knockback
- Damage applied when fighter enters hazard cell
- Hazardous cells display red on HUD

**HUD Display:**
```
Active Hazards: 3
```

---

## Integration Points

### 1. MatchState Initialization

When creating a match, assign a stage:

```csharp
var match = new MatchState
{
    MatchType = MatchType.PvE,
    FighterA = fighterA,
    FighterB = fighterB,
    Stage = ShrinkingArenaStage.Instance,  // ← Assign stage here
    Board = ShrinkingArenaStage.Instance.CreateBoard(),
    StageState = ShrinkingArenaStage.Instance.CreateRuntimeState()
};
```

### 2. ResolutionEngine — Hook into Combat Flow

In `ResolutionEngine.ResolveRound()`, call stage hooks:

```csharp
// At round start (after card commitment)
match.Stage.OnRoundStart(match, match.StageState);

// After each fighter moves
match.Stage.OnFighterMovementComplete(newPosA, fa, match, match.StageState);
match.Stage.OnFighterMovementComplete(newPosB, fb, match, match.StageState);

// Before attacks resolve
match.Stage.OnAttackPhaseStart(round, match, match.StageState);

// After all attacks resolve
match.Stage.OnRoundComplete(round, match, match.StageState);
```

### 3. FightScreen — Display Stage Effects

In rendering code, show hazardous hexes:

```csharp
private void DrawStageHazards(SpriteBatch sb)
{
    var hazardHexes = Game.Match.Stage.GetHazardousHexes(Game.Match.StageState);
    
    foreach (var hex in hazardHexes)
    {
        var (px, py) = HexBoard.HexToPixel(hex, HexSize, OriginX, OriginY);
        // Draw red/warning overlay
        sb.Draw(_hazardTexture, new Rectangle((int)px, (int)py, HexSize, HexSize), 
                Color.Red * 0.3f);
    }
}

private void DrawStageInfo(SpriteBatch sb)
{
    var info = Game.Match.Stage.GetHudDisplayInfo(Game.Match.StageState);
    int y = 250;
    foreach (var line in info)
    {
        sb.DrawString(_smallFont, line, new Vector2(20, y), Color.Yellow);
        y += 16;
    }
}
```

### 4. AiEngine — Stage-Aware Decisions

Personas and AI can check stage state:

```csharp
public override CardPair? GetPersonalizedAiDecision(...)
{
    // Example: In ShrinkingArenaStage, bias toward moving toward center
    if (match.Stage is ShrinkingArenaStage)
    {
        var restrictedCells = match.Stage.StageState.RestrictedCells;
        int distToRestriction = fighter.Position.DistanceTo(...);
        
        if (distToRestriction <= 1)
            return SelectCenteredMovement();  // Prioritize staying safe
    }
    
    return null;  // Use default AI
}
```

---

## Creating a Custom Stage

### Step 1: Create the Class

```csharp
namespace Grants.Models.Stage;

public class MyCustomStage : StageModifier
{
    public static readonly MyCustomStage Instance = new()
    {
        StageId = "mycustom",
        Name = "My Custom Stage",
        Description = "Unique mechanics here"
    };

    private MyCustomStage() { }

    public override StageState CreateRuntimeState()
    {
        var state = new StageState();
        // Initialize custom state...
        return state;
    }

    public override void OnRoundStart(MatchState match, StageState state)
    {
        state.TurnCount++;
        // Apply start-of-round effects...
    }

    // Implement remaining abst methods...
}
```

### Step 2: Assign to Match

```csharp
var match = new MatchState
{
    Stage = MyCustomStage.Instance,
    StageState = MyCustomStage.Instance.CreateRuntimeState(),
    Board = MyCustomStage.Instance.CreateBoard(),
    // ...
};
```

### Step 3: Use Hooks

- **Board layout:** Override `CreateBoard()` for custom 7x7 variants or non-standard shapes
- **Start-of-round:** Override `OnRoundStart()` to spawn hazards, shrink boundaries
- **Movement effects:** Override `OnFighterMovementComplete()` to apply knockback or damage
- **Attack mods:** Override `OnAttackPhaseStart()` to modify range/power based on position
- **End-of-round:** Override `OnRoundComplete()` to decay effects, award bonuses

---

## Common Patterns

### Pattern 1: Shrinking/Expanding Boundaries

```csharp
public override void OnRoundStart(MatchState match, StageState state)
{
    int radius = GetRadiusForRound(state.TurnCount);
    
    for (int q = -3; q <= 3; q++)
    {
        for (int r = -3; r <= 3; r++)
        {
            var coord = new HexCoord(q, r);
            if (coord.DistanceTo(HexCoord.Zero) > radius)
                state.RestrictedCells.Add(coord);
        }
    }
}

public override bool OnFighterMovementComplete(HexCoord pos, FighterInstance f, ...)
{
    if (state.RestrictedCells.Contains(pos))
    {
        // Push fighter to valid cell
        var valid = GetNearestValidCell(pos, match.Board);
        f.HexQ = valid.Q;
        f.HexR = valid.R;
        return true;
    }
    return false;
}
```

### Pattern 2: Hazard Spawning

```csharp
public override void OnRoundStart(MatchState match, StageState state)
{
    var rng = new Random();
    int hazardCount = rng.Next(1, 3);
    
    for (int i = 0; i < hazardCount; i++)
    {
        var randomHex = GetRandomValidHex(match.Board, rng);
        state.HazardMap[randomHex] = $"hazard_{state.TurnCount}_{i}";
        
        // Track with turn count for expiration
        state.EffectTimers[$"hazard_{...}"] = 2; // Lasts 2 turns
    }
}
```

### Pattern 3: Knockback Toward Center

```csharp
private static void PushTowardCenter(FighterInstance fighter, MatchState match, StageState state)
{
    var current = new HexCoord(fighter.HexQ, fighter.HexR);
    var center = HexCoord.Zero;
    
    var neighbors = current.GetNeighbors()
        .Where(h => match.Board.IsValid(h) && !match.Board.IsOccupied(h))
        .OrderBy(h => h.DistanceTo(center))
        .First();
    
    fighter.HexQ = neighbors.Q;
    fighter.HexR = neighbors.R;
}
```

### Pattern 4: Position-Based Damage

```csharp
public override void OnAttackPhaseStart(RoundState round, MatchState match, StageState state)
{
    var currentHazard = state.HazardMap.FirstOrDefault(
        h => h.Key == new HexCoord(match.FighterA.HexQ, match.FighterA.HexR));
    
    if (currentHazard.Value != null)
    {
        // Apply damage to FighterA
        int damageAmount = 2;
        round.DamageToA[BodyLocation.Core] = damageAmount;
        round.Log.Add($"{match.FighterA.DisplayName} hit by hazard!");
    }
}
```

---

## Testing Your Stage

1. **Unit test** stage state transitions (turns increment, hazards spawn/expire)
2. **Integration test** in a full match (verify knockback positions, damage application)
3. **HUD test** verify UI displays correctly (hazard markers, info text)
4. **AI test** verify stage doesn't break AI decision-making

---

## Next Steps

To activate stages in your game:

1. **Wire stage hooks** into `ResolutionEngine.ResolveRound()`
2. **Render stage hazards** in `FightScreen` (overlays, warning markers)
3. **Display stage info** in match HUD (turn count, active effects)
4. **Create stage selector** (choose stage before match starts)
5. **Add stage-specific upgrades** (meta strategies per stage)
6. **Design stage progression** (early = simple, late = complex)

---

## References

- **Framework defn:** `Models/Stage/StageModifier.cs`
- **Example stages:** `ExampleStages.cs`
- **Integration:** `Models/Match/MatchState.cs`
- **Board utilities:** `Models/Board/HexCoord.cs` (includes `GetNeighbors()`)
