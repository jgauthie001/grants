# Grants — TODO

## Immediate

- [ ] Replace all `_pl` placeholder names/descriptions with real content
  - `Fighters/Grants/GrantsFighter.cs` — card names, descriptions
  - `Fighters/Grants/GrantsUpgradeTree.cs` — node names, descriptions, item flavor
  - `Fighters/Cursed/CursedFighter.cs` — card descriptions
  - Screen strings: "Select your move", "Victory!", "Defeat!", "Round Log:", etc.
- [x] Test run: `dotnet run --project Grants/Grants.csproj`
  - Font loading verified working
  - Profile save/load verified working (`%LOCALAPPDATA%\Grants\Saves\`)
- [x] Hex board rendering: real pointy-top hexagon shape (scanline fill)
- [x] Add range display to CharacterBuilderScreen — MinRange/MaxRange editable for unique/special, MinRangeMod/MaxRangeMod for generic

## Builder

- [ ] CharacterBuilderScreen audit pass — ensure all fighter-level settings are editable:
  - `CriticalLocations` — toggle which body locations count toward KO
  - `KOThreshold` — how many critical locations must be Disabled to trigger KO
  - Card `PrimaryTarget` / `SecondaryTarget` on unique/special cards

## Gameplay

- [ ] Mouse input support in `FightScreen` — click hex to select card pair
- [ ] Local PvP: P2 movement selection — currently AI-resolved; P2 should choose hex destination when their card grants movement
- [ ] Keyword effects not yet wired in `FightScreen` display (Parry counter, Knockback, etc.)
- [ ] Bleed stack display in damage panel
- [ ] Stagger turns remaining display
- [ ] Round result animation / pause before auto-advancing (currently requires Enter)
- [ ] AI difficulty levels (current AI is greedy-only)
- [ ] Feint keyword logic (speed appears as X but resolves at X-2 — needs design confirmation)
- [ ] The Cursed upgrade tree (no tree yet; Standard persona has none either)
- [ ] PersonaHud opponent token display — show opponent’s curse tokens to The Cursed player

## Content

- [ ] AI opponent named properly in PostMatchScreen (currently "CPU Grants" / "CPU The Cursed")
- [ ] Match record saving (`RecentMatches` list in PlayerProfile) — PostMatchScreen
  currently doesn't append to it
- [ ] ProfileScreen: link "Upgrade Tree" button per fighter row

## Upgrade Tree

- [ ] Visual tree layout (currently flat list) — display as branch columns
- [ ] Show which card a CardSlot node upgrades (TargetCardId → card name lookup)
- [ ] Item node descriptions shown in fight HUD when active
- [ ] FinalNode effect active indicator in FightScreen

## Polish / QA

- [ ] Resolution support — UI positions are currently hardcoded for 1280×720; add support for common resolutions (1920×1080, 1600×900, etc.) using scaled/relative layout

- [ ] Window title ("Grants")
- [ ] Background color / art pass for all screens
- [ ] Sound effects placeholder (MonoGame SoundEffect)
- [ ] Keyboard navigation wrap-around on all screens
- [ ] Escape-to-back consistency audit across all screens
- [ ] Error handling if save file is corrupt (catch JSON parse failure in UpgradeEngine)

## Distribution

- [ ] `dotnet publish` self-contained build
- [ ] Icon (replace default MonoGame icon)
- [ ] README for players

## Done ✓

- [x] Card models (CardBase, GenericCard, UniqueCard, SpecialCard, CardPair)
- [x] Fighter models (FighterDefinition, FighterInstance, DamageLocation)
- [x] Board models (HexCoord, HexBoard)
- [x] Upgrade models (UpgradeNode, UpgradeTree, FighterProgress)
- [x] Match models (MatchState, PlayerProfile)
- [x] Engine: MovementEngine, AttackEngine, ResolutionEngine, AiEngine, UpgradeEngine
- [x] Grants fighter — 8 generics, 8 uniques, 2 specials
- [x] Grants upgrade tree — 4 branches, 4 items, 4 final nodes
- [x] All screens: MainMenu, FighterSelect, StageSelect, FightScreen, PostMatch, UpgradeTree, Profile
- [x] Game1.cs screen manager
- [x] Content pipeline: DefaultFont (Arial 14pt), SmallFont (Arial 10pt)
- [x] ImplicitUsings + Nullable enabled in .csproj
- [x] Hex board rendering: real pointy-top hexagon shape (scanline fill)
- [x] Range display in CharacterBuilderScreen
- [x] StageSelectScreen — stage chosen before fight; wired into FightScreen
- [x] ExhibitionStage — token hexes, pre-round damage, StageChoiceA/B
- [x] Resign button (Y/N confirm) + Draw condition (5 consecutive no-damage rounds)
- [x] F6 quit from MainMenu
- [x] CombatImmunity enum (Push, Pull, DefenseReduction, PowerReduction, SpeedReduction, Stagger, Bleed, CurseToken)
- [x] FighterPersona hook system (OnLandedHit, RequiresOpponentRoundStartChoice, GetOpponentChoicePrompt, ResolveAiOpponentChoice, OnOpponentChoice)
- [x] PersonaChoiceA/B phases in MatchPhase + FightScreen wiring
- [x] RoundPowerModifier, RoundSpeedModifier on FighterInstance
- [x] Curse keywords: CurseGain, CursePull, CurseEmpower, CurseWeaken
- [x] The Cursed fighter — 8 generics, 6 uniques, 2 specials, CursedPersona
- [x] Build: 0 errors
