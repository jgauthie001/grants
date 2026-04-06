# Grants — TODO

## Immediate

- [ ] Replace all `_pl` placeholder names/descriptions with real content
  - `Fighters/Grants/GrantsFighter.cs` — card names, descriptions
  - `Fighters/Grants/GrantsUpgradeTree.cs` — node names, descriptions, item flavor
  - Screen strings: "Select your move", "Victory!", "Defeat!", "Round Log:", etc.
- [ ] Test run: `dotnet run --project Grants/Grants.csproj`
  - Verify font loading works on target machine
  - Verify profile save/load round-trips correctly
- [ ] Hex board rendering: replace placeholder filled-square `DrawHex` with
  actual hexagon shape (6-sided polygon or pre-made texture)

## Gameplay

- [ ] Mouse input support in `FightScreen` — click hex to select card pair
- [ ] Keyword effects not yet wired in `FightScreen` display (Parry counter, Knockback, etc.)
- [ ] Bleed stack display in damage panel
- [ ] Stagger turns remaining display
- [ ] Round result animation / pause before auto-advancing (currently requires Enter)
- [ ] AI difficulty levels (current AI is greedy-only)
- [ ] Feint keyword logic (speed appears as X but resolves at X-2 — needs design confirmation)

## Content

- [ ] Second fighter — at minimum one opponent beyond Grants vs Grants (mirror match)
- [ ] AI opponent named properly in PostMatchScreen (currently "CPU Grants")
- [ ] Match record saving (`RecentMatches` list in PlayerProfile) — PostMatchScreen
  currently doesn't append to it
- [ ] ProfileScreen: link "Upgrade Tree" button per fighter row

## Upgrade Tree

- [ ] Visual tree layout (currently flat list) — display as branch columns
- [ ] Show which card a CardSlot node upgrades (TargetCardId → card name lookup)
- [ ] Item node descriptions shown in fight HUD when active
- [ ] FinalNode effect active indicator in FightScreen

## Polish / QA

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
- [x] All screens: MainMenu, FighterSelect, FightScreen, PostMatch, UpgradeTree, Profile
- [x] Game1.cs screen manager
- [x] Content pipeline: DefaultFont (Arial 14pt), SmallFont (Arial 10pt)
- [x] ImplicitUsings + Nullable enabled in .csproj
- [x] Build: 0 errors
