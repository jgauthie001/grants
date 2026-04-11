using Grants.Models.Board;
using Grants.Models.Match;
using Grants.Models.Fighter;

namespace Grants.Models.Stage;

/// <summary>
/// Default stage with no special mechanics.
/// Standard 7x7 hex board with no hazards or dynamic effects.
/// </summary>
public class StandardStage : StageModifier
{
    public static readonly StandardStage Instance = new()
    {
        StageId = "standard",
        Name = "Training Arena",
        Description = "A neutral arena for balanced combat."
    };

    private StandardStage() { }

    public override StageState CreateRuntimeState()
    {
        return new StageState();
    }

    public override void OnRoundStart(MatchState match, StageState state)
    {
        // No effects
    }

    public override void OnAttackPhaseStart(RoundState round, MatchState match, StageState state)
    {
        // No modifications
    }

    public override void OnRoundComplete(RoundState round, MatchState match, StageState state)
    {
        // No post-round logic
    }
}

/// <summary>
/// Shrinking Arena Stage:
/// Each round, the valid hexes shrink inward from the edges.
/// Fighters standing in newly invalid cells are pushed toward center and lose 1 speed.
/// 
/// Mechanics:
/// - Round 1-3: Full 7x7 hexagon
/// - Round 4-6: Radius 2 (shrunk 1 layer)
/// - Round 7+: Radius 1 (only center and adjacent)
/// - Fighters in invalid cells each turn are pushed to nearest valid hex
/// </summary>
public class ShrinkingArenaStage : StageModifier
{
    public static readonly ShrinkingArenaStage Instance = new()
    {
        StageId = "shrinking",
        Name = "Collapsing Arena",
        Description = "The arena shrinks inward each round. Stay toward the center!"
    };

    private ShrinkingArenaStage() { }

    public override StageState CreateRuntimeState()
    {
        var state = new StageState();
        state.CustomData["current_radius"] = 3; // Start at full 7x7
        return state;
    }

    public override void OnRoundStart(MatchState match, StageState state)
    {
        state.TurnCount++;

        // Determine radius based on round number
        int radius = GetRadiusForRound(state.TurnCount);
        state.CustomData["current_radius"] = radius;

        // Rebuild restricted cells based on new radius
        state.RestrictedCells.Clear();
        GenerateRestrictedCells(radius, state);

        // Log shrink event
        if (radius < 3)
        {
            match.History.LastOrDefault()?.Log.Add($"Stage Effect: Arena shrinks to radius {radius}!");
        }
    }

    public override bool OnFighterMovementComplete(
        HexCoord newPosition,
        FighterInstance fighter,
        MatchState match,
        StageState state)
    {
        // Check if fighter is in a restricted cell
        if (!state.RestrictedCells.Contains(newPosition))
            return false; // Safe

        // Fighter is in restricted cell — push to nearest valid cell
        var validCells = match.Board.AllCells
            .Where(h => !state.RestrictedCells.Contains(h))
            .ToList();

        if (validCells.Count == 0)
            return false; // No valid cells (shouldn't happen)

        // Find closest valid cell
        HexCoord closestValid = validCells
            .OrderBy(h => newPosition.DistanceTo(h))
            .First();

        // Move fighter and apply penalty
        fighter.HexQ = closestValid.Q;
        fighter.HexR = closestValid.R;

        // [TODO] Apply speed penalty: fighter.StaggerTurnsRemaining++ or similar
        return true; // Fighter was moved
    }

    public override List<HexCoord> GetHazardousHexes(StageState state)
    {
        return state.RestrictedCells.ToList();
    }

    public override List<string> GetHudDisplayInfo(StageState state)
    {
        int radius = (int)state.CustomData["current_radius"];
        return new()
        {
            $"Arena Radius: {radius}/3",
            $"Round: {state.TurnCount}"
        };
    }

    private static int GetRadiusForRound(int round)
    {
        // Shrink every 3 rounds
        if (round <= 3) return 3;  // Full board
        if (round <= 6) return 2;  // Shrink 1 layer
        if (round <= 9) return 1;  // Shrink 2 layers
        return 0; // Arena fully collapsed (shouldn't reach this)
    }

    private static void GenerateRestrictedCells(int safeRadius, StageState state)
    {
        // Generate all hexes and mark those outside safe radius as restricted
        for (int q = -3; q <= 3; q++)
        {
            int rMin = Math.Max(-3, -q - 3);
            int rMax = Math.Min(3, -q + 3);
            for (int r = rMin; r <= rMax; r++)
            {
                var coord = new HexCoord(q, r);
                int distFromCenter = coord.DistanceTo(HexCoord.Zero);
                if (distFromCenter > safeRadius)
                    state.RestrictedCells.Add(coord);
            }
        }
    }
}

/// <summary>
/// Push Zone Stage:
/// Each round, all fighters take 1 step of knockback toward the center.
/// This forces constant engagement and makes distance feel precarious.
/// </summary>
public class PushZoneStage : StageModifier
{
    public static readonly PushZoneStage Instance = new()
    {
        StageId = "push_zone",
        Name = "Unstable Ground",
        Description = "The ground shifts each round, pushing fighters toward center."
    };

    private PushZoneStage() { }

    public override StageState CreateRuntimeState()
    {
        return new StageState();
    }

    public override void OnRoundStart(MatchState match, StageState state)
    {
        state.TurnCount++;
        // Push logic will be applied in OnRoundComplete
    }

    public override void OnRoundComplete(RoundState round, MatchState match, StageState state)
    {
        // Apply knockback toward center for both fighters
        PushFighterTowardCenter(match.FighterA, match, round);
        PushFighterTowardCenter(match.FighterB, match, round);
    }

    private static void PushFighterTowardCenter(
        FighterInstance fighter,
        MatchState match,
        RoundState round)
    {
        var currentPos = new HexCoord(fighter.HexQ, fighter.HexR);
        var center = HexCoord.Zero;

        // Find neighbor closest to center
        var neighbors = currentPos.GetNeighbors()
            .Where(h => match.Board.IsValid(h) && !match.Board.IsOccupied(h))
            .ToList();

        if (neighbors.Count == 0)
            return; // Blocked, no valid pushback

        HexCoord nextPos = neighbors
            .OrderBy(h => h.DistanceTo(center))
            .First();

        fighter.HexQ = nextPos.Q;
        fighter.HexR = nextPos.R;

        round.Log.Add($"{fighter.DisplayName} pushed toward center by ground disturbance.");
    }

    public override List<string> GetHudDisplayInfo(StageState state)
    {
        return new() { "Stage: Constant Knockback Each Round" };
    }
}

/// <summary>
/// Hazard Zones Stage:
/// Certain hexes are marked as hazardous (damage, slow, etc.).
/// Hazards activate when a fighter enters them and persist for N turns.
/// New hazards spawn each round in random locations.
/// </summary>
public class HazardZoneStage : StageModifier
{
    public static readonly HazardZoneStage Instance = new()
    {
        StageId = "hazard_zone",
        Name = "Minefield",
        Description = "Random hazards spawn each round. Tread carefully!"
    };

    private HazardZoneStage() { }

    public override StageState CreateRuntimeState()
    {
        var state = new StageState();
        state.CustomData["hazards"] = new List<HazardZone>();
        state.CustomData["rng"] = new Random();
        return state;
    }

    public override void OnRoundStart(MatchState match, StageState state)
    {
        state.TurnCount++;

        // Spawn 1-2 new hazards randomly
        if (state.CustomData.TryGetValue("rng", out var rngObj) && rngObj is Random rng)
        {
            int hazardCount = rng.Next(1, 3);
            SpawnRandomHazards(hazardCount, match, state, rng);
        }

        // Decay existing hazards
        if (state.CustomData.TryGetValue("hazards", out var hazardsObj) &&
            hazardsObj is List<HazardZone> hazards)
        {
            foreach (var hazard in hazards)
            {
                if (hazard.TurnsRemaining > 0)
                    hazard.TurnsRemaining--;
            }

            // Remove expired hazards
            state.CustomData["hazards"] = hazards
                .Where(h => h.TurnsRemaining != 0)
                .ToList();
        }
    }

    public override bool OnFighterMovementComplete(
        HexCoord newPosition,
        FighterInstance fighter,
        MatchState match,
        StageState state)
    {
        if (!state.CustomData.TryGetValue("hazards", out var hazardsObj) ||
            !(hazardsObj is List<HazardZone> hazards))
            return false;

        // Check if fighter entered a hazard
        var hazardAtPos = hazards.FirstOrDefault(h => h.Position == newPosition);
        if (hazardAtPos == null)
            return false;

        // [TODO] Apply hazard effect based on type
        // Examples:
        // - Damage: apply damage to fighter
        // - Stun: apply stagger
        // - Knockback: push fighter away

        return false; // Position didn't change
    }

    public override List<HexCoord> GetHazardousHexes(StageState state)
    {
        if (state.CustomData.TryGetValue("hazards", out var hazardsObj) &&
            hazardsObj is List<HazardZone> hazards)
        {
            return hazards.Select(h => h.Position).ToList();
        }
        return new();
    }

    public override List<string> GetHudDisplayInfo(StageState state)
    {
        if (state.CustomData.TryGetValue("hazards", out var hazardsObj) &&
            hazardsObj is List<HazardZone> hazards)
        {
            int activeCount = hazards.Count(h => h.TurnsRemaining != 0);
            return new() { $"Active Hazards: {activeCount}" };
        }
        return new();
    }

    private static void SpawnRandomHazards(int count, MatchState match, StageState state, Random rng)
    {
        if (!state.CustomData.TryGetValue("hazards", out var hazardsObj) ||
            !(hazardsObj is List<HazardZone> hazards))
            return;

        var validCells = match.Board.AllCells.ToList();

        for (int i = 0; i < count && validCells.Count > 0; i++)
        {
            int idx = rng.Next(validCells.Count);
            var pos = validCells[idx];
            validCells.RemoveAt(idx);

            var hazardType = (HazardType)(rng.Next(1, 4)); // Damage, Stun, or Knockback
            hazards.Add(new HazardZone
            {
                Position = pos,
                Type = hazardType,
                Intensity = rng.Next(1, 3),
                TurnsRemaining = 2 // Last 2 rounds
            });
        }
    }
}

/// <summary>
/// The Entryway: A narrow corridor where footwork is mandatory.
/// Any fighter who ends the round on the same hex they started it takes
/// 1 damage step to their least-damaged leg. Standing still is punished —
/// keep moving or pay the price.
/// </summary>
public class EntryWayStage : StageModifier
{
    public static readonly EntryWayStage Instance = new()
    {
        StageId = "entryway",
        Name = "The Entryway",
        Description = "This cramped corridor punishes those who stand still. Move every round or your legs suffer.",
    };

    private EntryWayStage() { }

    // Keys stored in StageState.CustomData
    private const string KeyAQ = "start_aq";
    private const string KeyAR = "start_ar";
    private const string KeyBQ = "start_bq";
    private const string KeyBR = "start_br";

    public override StageState CreateRuntimeState() => new StageState();

    public override void OnRoundStart(MatchState match, StageState state)
    {
        // Snapshot starting positions before any movement or attacks
        state.CustomData[KeyAQ] = match.FighterA.HexQ;
        state.CustomData[KeyAR] = match.FighterA.HexR;
        state.CustomData[KeyBQ] = match.FighterB.HexQ;
        state.CustomData[KeyBR] = match.FighterB.HexR;
    }

    public override void OnRoundComplete(RoundState round, MatchState match, StageState state)
    {
        ApplyStillnessPenalty(match.FighterA, state, round, KeyAQ, KeyAR);
        ApplyStillnessPenalty(match.FighterB, state, round, KeyBQ, KeyBR);
    }

    private static void ApplyStillnessPenalty(
        FighterInstance fighter,
        StageState state,
        RoundState round,
        string keyQ,
        string keyR)
    {
        if (!state.CustomData.TryGetValue(keyQ, out var sq) ||
            !state.CustomData.TryGetValue(keyR, out var sr))
            return; // No snapshot — first round safety

        int startQ = (int)sq;
        int startR = (int)sr;

        // Fighter didn't move — check position unchanged
        if (fighter.HexQ != startQ || fighter.HexR != startR)
            return;

        // Pick least-damaged leg (LeftLeg preferred on tie)
        BodyLocation target = BetterLeg(fighter);
        var locState = fighter.LocationStates[target];

        if (locState.State == DamageState.Disabled)
            return; // Already disabled — can't get worse

        locState.ApplyDamage(1);
        round.Log.Add($"Stage [{EntryWayStage.Instance.Name}]: {fighter.DisplayName} stood still — {target} takes 1 damage! ({locState.State})");
    }

    private static BodyLocation BetterLeg(FighterInstance fighter)
    {
        var leftState  = fighter.LocationStates[BodyLocation.LeftLeg].State;
        var rightState = fighter.LocationStates[BodyLocation.RightLeg].State;
        // Hit the less-damaged leg; LeftLeg wins ties
        return rightState < leftState ? BodyLocation.RightLeg : BodyLocation.LeftLeg;
    }

    public override List<string> GetHudDisplayInfo(StageState state)
    {
        return new() { "THE ENTRYWAY: Stand still = leg damage" };
    }
}

/// <summary>
/// The Exhibition: A high-stakes showcase where fighters can claim territory.
///
/// Each round, BEFORE card selection:
///   1. Any fighter standing on or adjacent to an existing token takes 1 damage
///      step per token (to a random available location).
///   2. Each fighter is offered a choice: place a token on their current hex.
///      If they accept, their outgoing damage this round is reduced by 2 steps
///      (the commitment weakens their attack focus).
///
/// Tokens persist until the match ends.
/// </summary>
public class ExhibitionStage : StageModifier
{
    public static readonly ExhibitionStage Instance = new()
    {
        StageId = "exhibition",
        Name = "The Exhibition",
        Description = "Claim territory with tokens — but tokens near you deal damage each round, and placing one costs you offensive power.",
    };

    private ExhibitionStage() { }

    private const string KeyTokens   = "tokens";
    private const string KeyTokenA   = "token_a_placed";
    private const string KeyTokenB   = "token_b_placed";
    private const string KeyPreRndLog = "pre_round_log";

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static HashSet<HexCoord> GetTokens(StageState state)
    {
        if (state.CustomData.TryGetValue(KeyTokens, out var v) && v is HashSet<HexCoord> t)
            return t;
        var newSet = new HashSet<HexCoord>();
        state.CustomData[KeyTokens] = newSet;
        return newSet;
    }

    /// <summary>Returns true if the game round's attacker placed a token (damage reduced).</summary>
    public static bool AttackerPlacedToken(MatchState match, bool attackerIsA, StageState state)
    {
        string key = attackerIsA ? KeyTokenA : KeyTokenB;
        return state.Flags.TryGetValue(key, out bool v) && v;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override StageState CreateRuntimeState()
    {
        var state = new StageState();
        state.CustomData[KeyTokens]    = new HashSet<HexCoord>();
        state.CustomData[KeyPreRndLog] = new List<string>();
        state.Flags[KeyTokenA]         = false;
        state.Flags[KeyTokenB]         = false;
        return state;
    }

    /// <summary>
    /// Called at the very start of a new round (pre-card-selection).
    /// Resets session flags, then applies token damage to this fighter.
    /// </summary>
    public override void ApplyPreRoundEffects(
        FighterInstance fighter,
        MatchState match,
        StageState state,
        List<string> log)
    {
        // Reset token-placed flags once per round (do it on FighterA so it happens once)
        if (match.IsFighterA(fighter))
        {
            state.Flags[KeyTokenA] = false;
            state.Flags[KeyTokenB] = false;
        }

        var tokens = GetTokens(state);
        if (tokens.Count == 0) return;

        var fighterPos = new HexCoord(fighter.HexQ, fighter.HexR);
        var adjacentAndSelf = new HashSet<HexCoord>(fighterPos.GetNeighbors()) { fighterPos };

        int totalTokensNearby = tokens.Count(t => adjacentAndSelf.Contains(t));
        if (totalTokensNearby == 0) return;

        // Apply 1 damage step per nearby token to a random non-disabled, non-Stance location
        var targetable = fighter.LocationStates.Values
            .Where(ls => ls.State != DamageState.Disabled && ls.Location != BodyLocation.Stance)
            .OrderBy(ls => (int)ls.State)  // hit the least-damaged first
            .ToList();

        if (targetable.Count == 0) return;

        int stepsDone = 0;
        for (int i = 0; i < totalTokensNearby; i++)
        {
            // Cycle through available locations so multiple tokens hit different spots
            var loc = targetable[i % targetable.Count];
            loc.ApplyDamage(1);
            stepsDone++;
        }

        log.Add($"[The Exhibition] {fighter.DisplayName} takes {stepsDone} damage from nearby tokens!");
    }

    // ─── Choice hooks ─────────────────────────────────────────────────────────

    public override bool RequiresRoundStartChoice(
        FighterInstance fighter,
        MatchState match,
        StageState state) => true;

    public override string GetChoicePrompt(FighterInstance fighter, StageState state)
    {
        int nearby = CountNearbyTokens(fighter, GetTokens(state));
        string warning = nearby > 0
            ? $" (WARNING: {nearby} token(s) near here will hurt you next round!)"
            : "";
        return $"Place a token here? Reduces YOUR damage by 2 this round.{warning}  [Y] Yes  [N] No";
    }

    public override bool ResolveAiChoice(
        FighterInstance fighter,
        MatchState match,
        StageState state)
    {
        // AI places a token if it has low offensive power this round (damage reduction less costly)
        // Simple heuristic: place if fighter is injured (less to lose offensively, more to gain territorially)
        int badLocations = fighter.LocationStates.Values
            .Count(ls => ls.State >= DamageState.Injured);
        return badLocations >= 2;
    }

    public override void OnFighterChoice(
        FighterInstance fighter,
        bool accepted,
        MatchState match,
        StageState state)
    {
        bool isA = match.IsFighterA(fighter);
        state.Flags[isA ? KeyTokenA : KeyTokenB] = accepted;

        if (accepted)
        {
            var pos = new HexCoord(fighter.HexQ, fighter.HexR);
            GetTokens(state).Add(pos);
            // Log into pre-round log (no RoundState yet)
            var preLog = GetPreRoundLog(state);
            preLog.Add($"[The Exhibition] {fighter.DisplayName} places a token at {pos}. (Their damage -2 this round)");
        }
    }

    // ─── Round hooks ──────────────────────────────────────────────────────────

    public override void OnRoundStart(MatchState match, StageState state) { }

    public override void OnRoundComplete(RoundState round, MatchState match, StageState state)
    {
        // Flush pre-round log into round log for visibility
        var preLog = GetPreRoundLog(state);
        round.Log.InsertRange(0, preLog);
        preLog.Clear();

        // Apply damage discount: if a fighter placed a token, undo 2 steps of damage they dealt
        ApplyDiscount(match.FighterA, match.FighterB, round.LastHitOnB, KeyTokenA, state, round);
        ApplyDiscount(match.FighterB, match.FighterA, round.LastHitOnA, KeyTokenB, state, round);
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(StageState state)
    {
        int count = GetTokens(state).Count;
        string aPlaced = state.Flags.TryGetValue(KeyTokenA, out bool a) && a ? " [A: token placed]" : "";
        string bPlaced = state.Flags.TryGetValue(KeyTokenB, out bool b) && b ? " [B: token placed]" : "";
        return new()
        {
            $"THE EXHIBITION | Tokens on board: {count}{aPlaced}{bPlaced}",
        };
    }

    public override List<HexCoord> GetHazardousHexes(StageState state)
        => GetTokens(state).ToList();

    // ─── Internals ────────────────────────────────────────────────────────────

    private static List<string> GetPreRoundLog(StageState state)
    {
        if (state.CustomData.TryGetValue(KeyPreRndLog, out var v) && v is List<string> l)
            return l;
        var newLog = new List<string>();
        state.CustomData[KeyPreRndLog] = newLog;
        return newLog;
    }

    private static int CountNearbyTokens(FighterInstance fighter, HashSet<HexCoord> tokens)
    {
        var pos = new HexCoord(fighter.HexQ, fighter.HexR);
        var adjacentAndSelf = new HashSet<HexCoord>(pos.GetNeighbors()) { pos };
        return tokens.Count(t => adjacentAndSelf.Contains(t));
    }

    private static void ApplyDiscount(
        FighterInstance attacker,
        FighterInstance defender,
        BodyLocation? hitLocation,
        string flagKey,
        StageState state,
        RoundState round)
    {
        if (!state.Flags.TryGetValue(flagKey, out bool placed) || !placed) return;
        if (hitLocation == null) return;

        var locState = defender.LocationStates[hitLocation.Value];
        if (locState.State == DamageState.Healthy) return; // Nothing to undo

        locState.ReduceDamage(2);
        round.Log.Add($"[The Exhibition] {attacker.DisplayName} placed a token — damage to {defender.DisplayName}'s {hitLocation.Value} reduced by 2. ({locState.State})");
    }
}
