# Complete Integration Guide: Personas + Stages + Core Combat

This guide shows how to wire the Persona and Stage systems into your core combat engines (`AiEngine`, `ResolutionEngine`, `AttackEngine`).

## Overview

```
MatchState
├── FighterA (with Persona)
├── FighterB (with Persona)
├── Stage (with StageState)
└── ResolutionEngine invokes:
    ├── Persona hooks (pre/post attack, stat mods)
    └── Stage hooks (round effects, movement effects)
```

---

## 1. AiEngine.SelectPair() — Persona-Aware AI

**Current code** (before integration):
```csharp
public static CardPair SelectPair(
    FighterInstance ai,
    FighterInstance opponent,
    HexBoard board)
{
    var validPairs = ai.GetValidPairs();
    if (validPairs.Count == 0)
        throw new InvalidOperationException(...);

    int distance = new HexCoord(ai.HexQ, ai.HexR)
        .DistanceTo(new HexCoord(opponent.HexQ, opponent.HexR));

    bool anyInRange = validPairs.Any(p => distance <= (int)p.EffectiveRange);
    if (!anyInRange)
        return validPairs.OrderByDescending(p => p.CombinedMovement).First();

    return validPairs
        .Where(p => distance <= (int)p.EffectiveRange)
        .OrderByDescending(p => p.CombinedPower)
        .ThenByDescending(p => p.CombinedSpeed)
        .First();
}
```

**After integrating Personas:**
```csharp
public static CardPair SelectPair(
    FighterInstance ai,
    FighterInstance opponent,
    HexBoard board,
    MatchState match)  // ← Add match for stage access
{
    var validPairs = ai.GetValidPairs();
    if (validPairs.Count == 0)
        throw new InvalidOperationException(...);

    // === PERSONA HOOK: Ask persona for custom AI ===
    var personaChoice = ai.Definition.Persona.GetPersonalizedAiDecision(
        ai, opponent, board, ai.PersonaState);
    
    if (personaChoice != null)
        return personaChoice;  // Persona decided

    // === STAGE INFO: Check stage restrictions (optional) ===
    var hazardZones = match.Stage.GetHazardousHexes(match.StageState);
    
    // Default greedy AI (with optional stage awareness)
    int distance = new HexCoord(ai.HexQ, ai.HexR)
        .DistanceTo(new HexCoord(opponent.HexQ, opponent.HexR));

    bool anyInRange = validPairs.Any(p => distance <= (int)p.EffectiveRange);
    if (!anyInRange)
        return validPairs.OrderByDescending(p => p.CombinedMovement).First();

    return validPairs
        .Where(p => distance <= (int)p.EffectiveRange)
        .OrderByDescending(p => p.CombinedPower)
        .ThenByDescending(p => p.CombinedSpeed)
        .First();
}
```

---

## 2. ResolutionEngine.ResolveRound() — Full Integration

**Key integration points:**
1. Stage.OnRoundStart() → Setup round (shrink board, spawn hazards)
2. Persona.ModifyCardSelection() → (Optional) intercept pair selection
3. Movement → Stage.OnFighterMovementComplete() → Check hazards, knockback
4. Personas → OnRoundResolutionStart() → Pre-attack setup
5. Attacks → With Persona.ModifyCardStat() for stats
6. Personas → OnRoundResolutionComplete() → Post-attack effects
7. Stage.OnRoundComplete() → Decay effects, update state

**Code sketch:**
```csharp
public static RoundState ResolveRound(MatchState match)
{
    var pairA = match.SelectedPairA!;
    var pairB = match.SelectedPairB!;
    var fa = match.FighterA;
    var fb = match.FighterB;

    // Speed calculation (existing)
    int speedA = fa.GetCardSpeed(pairA.Generic ?? (CardBase)pairA.Special!) +
                 fa.GetCardSpeed(pairA.Unique ?? (CardBase)pairA.Special!);
    int speedB = fb.GetCardSpeed(pairB.Generic ?? (CardBase)pairB.Special!) +
                 fb.GetCardSpeed(pairB.Unique ?? (CardBase)pairB.Special!);

    var round = new RoundState
    {
        RoundNumber = match.CurrentRound,
        PairA = pairA,
        PairB = pairB,
        SpeedA = speedA,
        SpeedB = speedB,
    };

    var posA = new HexCoord(fa.HexQ, fa.HexR);
    var posB = new HexCoord(fb.HexQ, fb.HexR);

    round.Log.Add($"--- Round {round.RoundNumber} ---");
    round.Log.Add($"Speed: {fa.DisplayName}={speedA}, {fb.DisplayName}={speedB}.");

    // === STAGE HOOK: Round start ===
    match.Stage.OnRoundStart(match, match.StageState);

    // ===== MOVEMENT PHASE =====
    HexCoord newPosA, newPosB;

    if (round.FighterAFaster)
    {
        match.Board.SetOccupied(posB, true);
        newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board);
        match.Board.SetOccupied(posB, false);
        match.Board.SetOccupied(newPosA, true);
        newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, newPosA, match.Board);
        match.Board.SetOccupied(newPosA, false);
    }
    else if (round.FighterBFaster)
    {
        match.Board.SetOccupied(posA, true);
        newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
        match.Board.SetOccupied(posA, false);
        match.Board.SetOccupied(newPosB, true);
        newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, newPosB, match.Board);
        match.Board.SetOccupied(newPosB, false);
    }
    else
    {
        newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board);
        newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
    }

    // Commit positions
    fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
    fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;

    // === STAGE HOOK: After movement ===
    if (match.Stage.OnFighterMovementComplete(newPosA, fa, match, match.StageState))
    {
        // Fighter was moved by stage (knockback, push, etc.)
        round.Log.Add($"{fa.DisplayName} affected by stage effect!");
    }
    if (match.Stage.OnFighterMovementComplete(newPosB, fb, match, match.StageState))
    {
        round.Log.Add($"{fb.DisplayName} affected by stage effect!");
    }

    int distAfterMove = newPosA.DistanceTo(newPosB);
    round.Log.Add($"After movement: dist={distAfterMove}. {fa.DisplayName} at {newPosA}, {fb.DisplayName} at {newPosB}.");

    // === PERSONAS HOOK: Before attacks ===
    fa.Definition.Persona.OnRoundResolutionStart(
        round, match, fa, fb, fa.PersonaState);
    fb.Definition.Persona.OnRoundResolutionStart(
        round, match, fb, fa, fb.PersonaState);

    // === STAGE HOOK: Before attacks ===
    match.Stage.OnAttackPhaseStart(round, match, match.StageState);

    // ===== ATTACK PHASE =====
    if (round.FighterAFaster)
    {
        var resultA = AttackEngine.Resolve(fa, pairA, fb, pairB, distAfterMove, round);
        round.FighterBMissed = !resultA.InRange;

        bool bAttackCancelled = IsPairedBodyPartDisabled(fb, pairB);
        if (!bAttackCancelled)
            AttackEngine.Resolve(fb, pairB, fa, pairA, distAfterMove, round);
        else
            round.Log.Add($"{fb.DisplayName}'s attack cancelled — required body part disabled mid-round.");

        round.Outcome = resultA.Landed
            ? (bAttackCancelled ? RoundOutcome.FighterAWins : RoundOutcome.BothHit)
            : RoundOutcome.FighterBWins;
    }
    // ... (rest of attack resolution similar pattern)

    // === PERSONAS HOOK: After attacks ===
    fa.Definition.Persona.OnRoundResolutionComplete(
        round, match, fa, fb, fa.PersonaState);
    fb.Definition.Persona.OnRoundResolutionComplete(
        round, match, fb, fa, fb.PersonaState);

    // === STAGE HOOK: After attacks ===
    match.Stage.OnRoundComplete(round, match, match.StageState);

    // Decrement timers
    fa.Definition.Persona.UpdateState(fa.PersonaState);
    fb.Definition.Persona.UpdateState(fb.PersonaState);
    match.Stage.GetHudDisplayInfo(match.StageState); // Updates internal timers

    return round;
}
```

---

## 3. AttackEngine.Resolve() — Persona Stat Modifications

**Integrate persona stat mods into damage calculations:**

```csharp
public static AttackResult Resolve(
    FighterInstance attacker,
    CardPair attackPair,
    FighterInstance defender,
    CardPair defensePair,
    int distance,
    RoundState log)
{
    var result = new AttackResult();

    // Get base combined stats
    int attackerPower = attackPair.CombinedPower;
    int defenderDefense = defensePair.CombinedDefense;

    // === PERSONA HOOKS: Modify stats ===
    attackerPower = attacker.Definition.Persona.ModifyCardStat(
        attackPair.Generic ?? (CardBase)attackPair.Special!,
        StatType.Power, attackerPower, attacker, attacker.PersonaState);

    defenderDefense = defender.Definition.Persona.ModifyCardStat(
        defensePair.Generic ?? (CardBase)defensePair.Special!,
        StatType.Defense, defenderDefense, defender, defender.PersonaState);

    // Calculate net power (with modifications)
    int netPower = attackerPower - defenderDefense;
    int damageSteps = Math.Max(1, netPower / 2);

    // Apply to target...
    // (rest of attack resolution)

    return result;
}
```

---

## 4. FightScreen Rendering — Show Personas & Stages

**Display both persona and stage info:**

```csharp
private void DrawHUD(SpriteBatch sb)
{
    // Fighter personas at top
    DrawFighterPersonaInfo(sb, _match.FighterA, 20, 20);
    DrawFighterPersonaInfo(sb, _match.FighterB, 820, 20);

    // Stage effects in center
    DrawStageInfo(sb);

    // Hazardous hexes during movement/attack
    DrawStageHazards(sb);
}

private void DrawFighterPersonaInfo(SpriteBatch sb, FighterInstance fighter, int x, int y)
{
    string personaLabel = $"Persona: {fighter.Definition.Persona.Name}";
    sb.DrawString(_font, personaLabel, new Vector2(x, y), Color.Yellow);
    y += 20;

    var hudInfo = fighter.Definition.Persona.GetHudDisplayInfo(fighter.PersonaState);
    foreach (var info in hudInfo)
    {
        sb.DrawString(_smallFont, info, new Vector2(x, y), Color.LimeGreen);
        y += 14;
    }
}

private void DrawStageInfo(SpriteBatch sb)
{
    var info = _match.Stage.GetHudDisplayInfo(_match.StageState);
    int cx = Game.GraphicsDevice.Viewport.Width / 2;
    int y = 20;
    
    foreach (var line in info)
    {
        var size = _smallFont.MeasureString(line);
        sb.DrawString(_smallFont, line, new Vector2(cx - size.X / 2, y), Color.Cyan);
        y += 16;
    }
}

private void DrawStageHazards(SpriteBatch sb)
{
    var hazardHexes = _match.Stage.GetHazardousHexes(_match.StageState);
    
    foreach (var hex in hazardHexes)
    {
        var (px, py) = HexBoard.HexToPixel(hex, HexSize, OriginX, OriginY);
        // Draw red warning overlay on hazard hexes
        sb.Draw(_whitePixel, new Rectangle((int)px - HexSize/2, (int)py - HexSize/2, HexSize, HexSize),
                Color.Red * 0.3f);
    }
}
```

---

## 5. MatchState Initialization

**Wire everything together when creating a match:**

```csharp
public static MatchState CreateMatch(
    FighterDefinition fighterADef,
    string fighterAName,
    FighterDefinition fighterBDef,
    string fighterBName,
    StageModifier stage = null)
{
    // Use provided stage or default
    stage ??= StandardStage.Instance;

    // Create fighter instances (personas auto-initialized)
    var fa = new FighterInstance(fighterADef, fighterAName);
    var fb = new FighterInstance(fighterBDef, fighterBName);

    // Create match with stage
    var match = new MatchState
    {
        MatchType = MatchType.PvE,
        FighterA = fa,
        FighterB = fb,
        Stage = stage,
        StageState = stage.CreateRuntimeState(),
        Board = stage.CreateBoard()
    };

    // Position fighters on board
    fa.HexQ = HexBoard.FighterAStart.Q;
    fa.HexR = HexBoard.FighterAStart.R;
    fb.HexQ = HexBoard.FighterBStart.Q;
    fb.HexR = HexBoard.FighterBStart.R;

    return match;
}
```

---

## Summary: Integration Checklist

- [ ] Update `AiEngine.SelectPair()` to accept `MatchState` and call persona hook
- [ ] Add stage hooks to `ResolutionEngine.ResolveRound()`:
  - `OnRoundStart`
  - `OnFighterMovementComplete` (both fighters)
  - `OnAttackPhaseStart`
  - `OnRoundComplete`
- [ ] Integrate `Persona.ModifyCardStat()` into `AttackEngine` stat calculations
- [ ] Add persona and stage HUD display to `FightScreen`
- [ ] Render stage hazard hexes with warning overlays
- [ ] Update `MatchState` creation to initialize stage
- [ ] Test full match with persona + stage interactions

---

## Testing Strategy

1. **Persona-only test:** Match with TrappingPersona vs StandardPersona
2. **Stage-only test:** ShrinkingArenaStage with standard fighters
3. **Combined test:** TrappingPersona on ShrinkingArenaStage
4. **Edge cases:** Persona abilities + stage knockback interactions

---

## Reference Files

- `PERSONA_SYSTEM.md` — Persona architecture and extension guide
- `STAGE_SYSTEM.md` — Stage architecture and examples
- `AiEngine.cs` — Where to add persona hook
- `ResolutionEngine.cs` — Where to add stage hooks
- `AttackEngine.cs` — Where to add persona stat modifications
- `FightScreen.cs` — Where to add persona/stage HUD rendering
