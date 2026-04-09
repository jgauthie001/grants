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
