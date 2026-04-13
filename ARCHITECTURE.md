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
    │   ├── Cards/           # Card types, keywords, pairing
    │   ├── Fighter/         # Fighter definition, instance, damage
    │   ├── Board/           # Hex coordinate math and board
    │   ├── Upgrades/        # FighterUpgradeDef, CardUpgradeSlotDef, FighterProgress, MatchResult
    │   └── Match/           # Match state, player profile, records
    ├── Engine/              # Pure-logic systems (no MonoGame)
    ├── Fighters/
    │   ├── Grants/          # Grants fighter cards, GrantsUpgrades.cs
    │   └── Cursed/          # The Cursed fighter cards + persona
    └── Screens/             # MonoGame UI screens
```

## Data Flow

```
PlayerProfile (disk)
    └─▶ UpgradeEngine.ApplyProgressToInstance(instance, progress, upgradeDef, upgradesEnabled)
            └─▶ FighterInstance (match-time state)

CardSelection phase:
    Human: FightScreen UI → MatchState.SelectedPairA
    AI:    AiEngine.SelectPair() → MatchState.SelectedPairB

Resolution phase:
    ResolutionEngine.ResolveFirstHalf()   (Start + Beginning + Main priority)
    ResolutionEngine.ResolveSecondHalf()  (Main second + Final both + End)
        ├─▶ ExecuteFighterPhaseActions()  (per-fighter, per-phase: move → attack → post-move)
        ├─▶ MovementEngine   (hex pathfinding, board occupation)
        ├─▶ AttackEngine     (range, power vs defense, keywords)
        ├─▶ ApplyAttackerPostMove()  (post-attack movement + Recoil/FollowThrough/Disengage)
        └─▶ RoundState       (log, damage map, outcome)

Post-match:
    UpgradeEngine.BuildMatchResult(match, playerWon)
    UpgradeEngine.RecordMatchAndUnlock(progress, upgradeDef, result)  → List<string> newlyUnlocked
    UpgradeEngine.SaveProfile(profile)
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

Each round runs five named phases. Card actions fire in the phase they declare
(via `MovementPhase`, `AttackPhase`, `PostMovementPhase` on the card).

| Step | Phase | Who acts |
|------|-------|----------|
| 1 | **Start** | Speed determined; priority player set; persona/stage housekeeping |
| 2 | **Beginning** | Priority player's declared Beginning actions |
| 3 | **Beginning** | Second player's declared Beginning actions |
| 4 | **Main** | Priority player's declared Main actions |
| 5 | **Main** | Second player's declared Main actions\* |
| 6 | **Final** | Priority player's declared Final actions |
| 7 | **Final** | Second player's declared Final actions |
| 8 | **End** | Cooldowns, bleed, KO/stalemate check |

\* Second player's Main action is cancelled if priority's attack disabled or
damaged the body location of second's generic card this round.

Speed tie: all phases resolve simultaneously (no midpoint pause).

KO: `IsKnockedOut` = number of `Disabled` locations ≥ `KOThreshold` (default 2).

Stalemate: 5 consecutive rounds with no damage dealt → draw.

## Movement System

### Phase-Driven Actions

Every action on a card declares which phase it fires in:

| Card field | Default phase | Effect |
|---|---|---|
| `MovementPhase` (GenericCard) | Beginning | Pre-attack repositioning; player picks hex if Free |
| `AttackPhase` (UniqueCard / SpecialCard) | Main | The attack itself |
| `PostMovementPhase` (UniqueCard / SpecialCard) | Final | Post-attack repositioning (always auto) |

Cards can override these defaults to place an action in any phase
(e.g., a unique with `AttackPhase = Beginning` hits early, or
`MovementPhase = Main` for a lunge that moves into the attack).

### Card Fields

All card types carry: `MinMovement`, `MaxMovement`,
`BaseMovementType` (`Approach` / `Retreat` / `Free` / `None`).

`CardPair` exposes:
- `EffectiveMinMovement` / `EffectiveMaxMovement` / `CombinedMovementType` — generic movement
- `PostMovementMin` / `PostMovementMax` / `PostMovementType` — unique/special post-move
- `GenericMovementPhase`, `AttackPhase`, `PostMovementPhase` — phase declarations

### Post-Attack Movement Keywords

| Keyword | Trigger | Effect |
|---------|---------|--------|
| `Recoil N` | Always (hit or miss) | Attacker retreats N hexes |
| `FollowThrough N` | Landed hit only | Attacker advances N hexes |
| `Disengage N` | Out-of-range / missed only | Attacker retreats N hexes |

Processed in order after card-field post-movement: Recoil → FollowThrough → Disengage.
Handled by `ResolutionEngine.ApplyAttackerPostMove()`.

### Distribution Targets
- ~2 average movement per card across all fighters
- ~1/3 Approach, ~1/3 Retreat, ~1/3 Free; aggressive fighters lean Approach

## Upgrade System

Each fighter has a **FighterUpgradeDef** — a flat dictionary of
`CardUpgradeSlotDef` keyed by `SlotId` (`"cardId:slotIndex"`).

### Slot Tiers

| Slot | Gate | Typical reward |
|------|------|----------------|
| 0 | 5 distinct matches with this card | +1 stat (Power/Defense/Speed) |
| 1 | 15 distinct matches with this card | Add keyword |
| 2 | Mastery condition (see below) | Keyword upgrade or Persona unlock |

### Mastery Condition Types

| Type | Description |
|------|-------------|
| `PlayedInMatches` | Card played in N distinct matches |
| `LandedHits` | Attack with this card landed N times (cumulative) |
| `LandedVsFaster` | Landed against a faster opponent N times |
| `LandedAtRange` | Landed at ≥ MinDistance hexes N times |
| `EventCounter` | Named event counter reaches N (e.g. `follow_through`) |
| `WonMatchWithCard` | Won N matches where this card was played |
| `WonWithKillingBlow` | Won N matches where this card dealt the killing blow |

### Progression Tracking (FighterProgress)

Fields saved per fighter: `CardDistinctMatches`, `CardLandedHits`,
`CardLandedVsFaster`, `CardLandedAtRange`, `CardWinsWithCard`,
`CardKillingBlows`, `EventCounters` (all `Dictionary<string,int>`),
`UnlockedSlots` (`HashSet<string>`).

Stats are recorded after each match via:
```
UpgradeEngine.BuildMatchResult(match, playerWon)   // builds MatchResult from history
UpgradeEngine.RecordMatchAndUnlock(progress, upgradeDef, result)  // returns List<string> newly unlocked SlotIds
```

### Applying Upgrades

```
UpgradeEngine.ApplyProgressToInstance(instance, progress, upgradeDef, upgradesEnabled)
```

No-op when `upgradesEnabled = false` (safe to disable for PvP balance).
`PersonaUnlock` slot type adds a string ID to `FighterInstance.UnlockedPersonaIds`;
personas query `instance.HasUpgrade("unlock_id")` to conditionally activate mastered behaviours.

### Per-Fighter Slot Definitions

| Fighter | File | Slots |
|---------|------|-------|
| Grants  | `Fighters/Grants/GrantsUpgrades.cs` | 54 (18 cards × 3) |

**Ranked PvP** requires ≥ 15 total wins with that specific fighter.

## Screen Flow

```
MainMenu
  ├─▶ FighterSelect (pve / pvp_casual / pvp_ranked)
  │       └─▶ StageSelect
  │               └─▶ FightScreen
  │                       └─▶ PostMatchScreen
  │                               ├─▶ UpgradeSelectionScreen  data: (fighterId, List<string> newlyUnlocked)
  │                               │       └─▶ UpgradeTreeScreen  data: fighterId
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
