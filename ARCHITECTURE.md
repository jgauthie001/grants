# Grants — Architecture

## Overview

Card-based fighter game inspired by Tekken. Two fighters play simultaneous
card pairs on a 7×7 hex board. Faster fighter resolves first; speed ties are
truly simultaneous. Built with MonoGame 3.8 DesktopGL / .NET 9.

## Project Layout

```
E:\grants\
├── Grants.slnx              # Solution
└── Grants/
    ├── Game1.cs             # Screen manager, shared resources
    ├── Program.cs           # Entry point
    ├── Content/
    │   └── Fonts/
    │       ├── DefaultFont.spritefont   # Arial 14pt
    │       └── SmallFont.spritefont    # Arial 10pt
    ├── Models/
    │   ├── Cards/           # Card types and pairing
    │   ├── Fighter/         # Fighter definition, instance, damage
    │   ├── Board/           # Hex coordinate math and board
    │   ├── Upgrades/        # Upgrade tree, nodes, fighter progress
    │   └── Match/           # Match state, player profile, records
    ├── Engine/              # Pure-logic systems (no MonoGame)
    ├── Fighters/
    │   ├── Grants/          # Grants fighter cards + upgrade tree
    │   └── Cursed/          # The Cursed fighter cards + persona
    └── Screens/             # MonoGame UI screens
```

## Data Flow

```
PlayerProfile (disk)
    └─▶ UpgradeEngine.ApplyProgressToInstance()
            └─▶ FighterInstance (match-time state)

CardSelection phase:
    Human: FightScreen UI → MatchState.SelectedPairA
    AI:    AiEngine.SelectPair() → MatchState.SelectedPairB

Resolution phase:
    ResolutionEngine.ResolveRound()
        ├─▶ MovementEngine   (speed order, board occupation)
        ├─▶ AttackEngine     (range, power vs defense, keywords)
        └─▶ RoundState       (log, damage map, outcome)

Post-match:
    FighterProgress.RecordWin()
    UpgradeEngine.SaveProfile()
```

## Card System

Every turn a fighter commits a **CardPair**: one GenericCard + one UniqueCard
or SpecialCard (or a Standalone special alone).

| Type        | Count | Cooldown | Notes                                   |
|-------------|-------|----------|-----------------------------------------|
| GenericCard | 8     | 1 turn   | Selects body part; satisfies BodyTags   |
| UniqueCard  | 8     | 2 turns  | Requires matching BodyTags on generic   |
| SpecialCard | 2     | 3 turns  | Can be Standalone (no generic required) |

Combined stats used for resolution:
- `CombinedSpeed = Generic.Speed + Unique/Special.Speed`
- `CombinedPower`, `CombinedDefense`, `CombinedMovement` — same pattern
- `AllKeywords` — union of both cards' keywords

### Keywords (21)

`Bleed` `ArmorBreak` `Piercing` `Crushing` `Feint` `Quickstep` `Lunge`
`Stagger` `Disrupt` `Knockback` `Guard` `Parry` `Deflect` `Sidestep`
`Press` `Retreat`

Curse keywords (The Cursed only):
`CurseGain` `CursePull` `CurseEmpower` `CurseWeaken`

## Damage Model

Each of 8 body locations (`Head`, `Torso`, `LeftArm`, `RightArm`, `LeftLeg`,
`RightLeg`, `Groin`, `Core`) tracks a `DamageState`:

```
Healthy → Bruised → Injured → Disabled
```

- **Disabled** removes that generic from the hand for the rest of the match.
- Each state applies stat penalties (power/defense/speed) via `DamageStatPenalty`.
- `DamageCap` on a `LocationState` prevents progression past a certain state
  (set by items/final nodes).

Net damage steps per attack: `1 per 2 net power, minimum 1`. Crushing keyword
adds +1 step.

## Hex Board

Axial coordinates `(q, r)`. Radius-3 hexagon = 37 valid cells.

```
Fighter A starts: (-3, 0)    Fighter B starts: (3, 0)
```

`HexMath.ReachableHexes` produces all cells reachable within a movement
budget. `MovementEngine` picks closest hex to opponent (or farthest for
Retreat keyword).

## Resolution Order

1. Compute `CombinedSpeed` for both fighters.
2. Faster fighter: move, then attack.
3. Slower fighter: move, then attack (attack cancelled if their generic's
   body part was **Disabled** during step 2).
4. Speed tie: both move simultaneously, then both attack simultaneously.
5. Apply cooldowns (Stagger adds +1 to loser's cooldowns).
6. Check KO: `IsKnockedOut` = number of `Disabled` locations ≥ `KOThreshold`
   (default 2).
7. Stalemate: 5 consecutive rounds with no damage dealt → draw.

## Upgrade System

Each fighter has a **UpgradeTree** with 4 branches × ~8 card-slot nodes +
1 item node each, and 4 **FinalNodes** (one per branch, gated last).

Upgrade points are earned from **PvE wins** and **PvP Casual wins only**
(ranked wins do not count):

| Win range | Rate         |
|-----------|--------------|
| 1–20      | 1 pt / win   |
| 21–50     | 1 pt / 2 wins|
| 51–100    | 1 pt / 3 wins|
| 100+      | 1 pt / 5 wins|

`PowerRating` = sum of `PowerRatingValue` across all unlocked nodes.
Used for ±10 PvP Casual matchmaking.

**Ranked PvP** requires ≥ 15 total wins with that specific fighter.

## Screen Flow

```
MainMenu
  ├─▶ FighterSelect (pve / pvp_casual / pvp_ranked)
  │       └─▶ StageSelect
  │               └─▶ FightScreen
  │                       └─▶ PostMatchScreen
  │                               ├─▶ UpgradeTreeScreen
  │                               ├─▶ FighterSelect (rematch)
  │                               └─▶ MainMenu
  └─▶ ProfileScreen
          └─▶ MainMenu
```

`Game1.SwitchScreen(ScreenType, object? data)` drives all transitions.
Each screen receives typed data via `OnEnter(object? data)`.

## Shared Resources (Game1)

| Property        | Type          | Description               |
|-----------------|---------------|---------------------------|
| `DefaultFont`   | `SpriteFont`  | Arial 14pt                |
| `SmallFont`     | `SpriteFont`  | Arial 10pt                |
| `Pixel`         | `Texture2D`   | 1×1 white for primitives  |
| `PlayerProfile` | `PlayerProfile` | Loaded from disk at start |

## Persistence

Profiles serialized to JSON at:
```
%LocalAppData%\Grants\Saves\{playerId}.json
```

Managed by `UpgradeEngine.SaveProfile` / `LoadOrCreateProfile`.

## Naming Conventions

- **`_pl` suffix** — all flavor text strings (Name, Description,
  FlavorDescription in fighter/upgrade data files) carry this suffix to mark
  them as placeholder content needing real writing.
- Mechanical identifiers (enum values, card IDs, method names) do **not**
  use `_pl`.

## Persona System

Each `FighterDefinition` carries a `FighterPersona` singleton. At match start
`CreateRuntimeState()` produces a `PersonaState` attached to the `FighterInstance`.

### Hooks

| Hook | When called |
|---|---|
| `OnRoundResolutionStart` | Before attacks resolve; populate `ActiveImmunities`, set `RoundPowerModifier` / `RoundSpeedModifier` |
| `OnLandedHit` | After each hit lands (called for both attacker and defender personas) |
| `OnRoundResolutionComplete` | After all attacks and cooldowns for the round |
| `RequiresOpponentRoundStartChoice` | Return true to offer the opponent a Y/N choice before card selection |
| `GetOpponentChoicePrompt` | One-line prompt shown to the human opponent |
| `ResolveAiOpponentChoice` | AI answer for the choice |
| `OnOpponentChoice` | Apply effects of the accepted/declined choice |

### Round-scoped modifiers

`FighterInstance.RoundPowerModifier` and `RoundSpeedModifier` are applied in
`AttackEngine` (power) and `ResolutionEngine` (speed), then cleared at the
start of the next `StartNewRound()`.

`FighterInstance.ActiveImmunities` (`HashSet<CombatImmunity>`) is cleared at
the start of `ResolveFirstHalf` and populated by `OnRoundResolutionStart`.
Immunity flags: `Push`, `Pull`, `DefenseReduction`, `PowerReduction`,
`SpeedReduction`, `Stagger`, `Bleed`, `CurseToken`.

### PersonaChoiceA / PersonaChoiceB phases

After stage choices (pre-card-selection), `FightScreen.AdvancePersonaChoices()`
checks both fighters' personas. If a human player must answer, the match
enters `MatchPhase.PersonaChoiceA` or `PersonaChoiceB` and
`DrawPersonaChoicePrompt` shows the prompt with [Y] Accept / [N] Decline.

## Fighters

| Fighter | Persona | Notes |
|---|---|---|
| Grants | StandardPersona | Balanced brawler; starter |
| The Cursed | CursedPersona | Token-based; curse pool, transfers to opponent |

### The Cursed — persona summary

- **Pool** (0–3): gains 1 on every landed hit; overflow deals 2 self-damage steps.
- `CurseGain` keyword: +1 extra pool token on hit.
- After each gain, 1 token transfers from pool to opponent (0–3 cap).
- **Opponent tokens**: each pre-round the opponent may spend 1 token for −1 Power / −1 Speed; if they don't hit The Cursed that round the token is returned.
- `CursePull`: pull opponent N hexes (N = their tokens).
- `CurseEmpower`: +N power (N = owner's pool).
- `CurseWeaken`: −N defender defense (N = their tokens).

## Build

```bash
cd C:\projects\grants
dotnet build Grants/Grants.csproj
dotnet run --project Grants/Grants.csproj
```

Requires MonoGame content pipeline via the `.mgcb` file in `Content/`.
