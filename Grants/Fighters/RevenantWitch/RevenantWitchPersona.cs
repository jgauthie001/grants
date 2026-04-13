using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.RevenantWitch;

/// <summary>
/// The Revenant Witch persona.
///
/// SPIRITS:
///   3 spirits total.  One starts at the center hex (0,0) at match begin.
///   Each pre-round, the Witch may place one spirit on her hex or an adjacent valid hex.
///   If all 3 are already deployed, placing one recalls the FARTHEST spirit first.
///   Spirits move 2 hexes toward the opponent at the end of every round.
///   A spirit occupying the opponent's exact hex at round-end deals 2 random damage steps.
///
/// UNIQUE CARD INTERACTIONS (resolved in OnRoundResolutionStart / OnRoundResolutionComplete):
///   witch_u_soul_swap       — End-of-round: swap positions with the nearest spirit.
///   witch_u_spectral_strike — +1 Power per spirit within 2 hexes of opponent (max +3).
///   witch_u_spirit_ward     — +1 Defense per spirit within 2 hexes of Witch (max +3).
///   witch_u_grave_hex       — +2 Power if any spirit is on the opponent's exact hex.
///   witch_u_haunting_shroud — +2 Defense if ≥ 2 spirits within 2 hexes of Witch.
///   witch_u_soul_drain      — Consume nearest spirit → +3 Power this round.
///   witch_u_phantom_rush    — Spirits travel +2 extra hexes at end of round.
///   witch_u_curse_chain     — +2 Power if ≥ 1 spirit within 1 hex of opponent.
/// </summary>
public class RevenantWitchPersona : FighterPersona
{
    public static readonly RevenantWitchPersona Instance = new()
    {
        PersonaId   = "revenant_witch",
        Name        = "Revenant Witch",
        Description = "Deploys spirits that hunt the opponent across the board, empowering her attacks and position.",
    };

    private RevenantWitchPersona() { }

    // ── Constants ─────────────────────────────────────────────────────────────
    private const string SpiritEffectType   = "witch_spirit";
    private const string KeyPendingUnique   = "witch_pending_unique";
    private const int    MaxSpirits         = 3;
    private const int    SpiritMovePerRound = 2;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override PersonaState CreateRuntimeState()
    {
        var state = new PersonaState();

        // Place 1 starting spirit at the center hex
        state.ActiveEffects.Add(new PersonaArenaEffect
        {
            EffectType      = SpiritEffectType,
            Position        = HexCoord.Zero,
            TurnsRemaining  = -1, // permanent until removed
        });

        state.CustomData["rng"] = new Random();
        return state;
    }

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state) => null;

    // ── Pre-round spirit placement ────────────────────────────────────────────

    public override PersonaChoiceRequest? GetPreRoundSelfChoice(
        FighterInstance owner, FighterInstance opponent, MatchState match, PersonaState state)
    {
        var witchPos  = new HexCoord(owner.HexQ, owner.HexR);
        var occupied  = GetSpiritPositions(state);
        int inPlay    = occupied.Count;
        int inReserve = MaxSpirits - inPlay;

        // Build list of valid placement hexes: own hex + 6 neighbors, no existing spirit there
        var validHexes = new List<HexCoord>();
        validHexes.Add(witchPos);
        foreach (var dir in HexCoord.Directions)
        {
            var candidate = witchPos + dir;
            if (match.Board.IsValid(candidate))
                validHexes.Add(candidate);
        }
        validHexes = validHexes.Distinct().Where(h => !occupied.Contains(h)).ToList();

        if (validHexes.Count == 0) return null;

        var oppPos   = new HexCoord(opponent.HexQ, opponent.HexR);
        string status = inReserve > 0
            ? $"Place a spirit  |  In reserve: {inReserve}  |  In play: {inPlay}/{MaxSpirits}"
            : $"Reposition a spirit (recalls farthest)  |  In play: {inPlay}/{MaxSpirits}";

        var options = validHexes.Select(h =>
        {
            string id   = HexToOptionId(h);
            string loc  = h == witchPos ? "Here" : $"({h.Q},{h.R})";
            int distOpp = h.DistanceTo(oppPos);
            string desc = $"dist to opponent: {distOpp}";
            return new PersonaChoiceOption(id, loc, desc);
        }).ToList();

        return new PersonaChoiceRequest
        {
            Prompt     = status,
            Options    = options,
            CanSkip    = true,
            HeaderTint = (180, 100, 255),
        };
    }

    public override void OnPreRoundSelfChoiceSelected(
        FighterInstance owner, string? optionId, MatchState match, PersonaState state)
    {
        if (optionId == null) return; // skipped

        if (!TryParseHexOptionId(optionId, out var targetHex)) return;
        if (!match.Board.IsValid(targetHex)) return;

        var witchPos = new HexCoord(owner.HexQ, owner.HexR);
        var spirits  = GetSpiritEffects(state);

        // If at max, recall the farthest spirit first
        if (spirits.Count >= MaxSpirits)
        {
            var farthest = spirits
                .OrderByDescending(s => s.Position?.DistanceTo(witchPos) ?? 0)
                .First();
            state.ActiveEffects.Remove(farthest);
        }

        // Summon a new spirit at chosen hex (only if not already occupied by a spirit)
        var occupied = GetSpiritPositions(state);
        if (!occupied.Contains(targetHex))
        {
            state.ActiveEffects.Add(new PersonaArenaEffect
            {
                EffectType     = SpiritEffectType,
                Position       = targetHex,
                TurnsRemaining = -1,
            });
        }
    }

    public override string? ResolveAiPreRoundSelfChoice(
        FighterInstance owner, FighterInstance opponent, MatchState match, PersonaState state)
    {
        var witchPos  = new HexCoord(owner.HexQ, owner.HexR);
        var oppPos    = new HexCoord(opponent.HexQ, opponent.HexR);
        var occupied  = GetSpiritPositions(state);

        // Valid placement hexes
        var validHexes = new List<HexCoord> { witchPos };
        foreach (var dir in HexCoord.Directions)
        {
            var c = witchPos + dir;
            if (match.Board.IsValid(c)) validHexes.Add(c);
        }
        validHexes = validHexes.Where(h => !occupied.Contains(h)).ToList();

        if (validHexes.Count == 0) return null;

        // Pick the valid hex closest to the opponent
        var best = validHexes.OrderBy(h => h.DistanceTo(oppPos)).First();
        return HexToOptionId(best);
    }

    // ── Round resolution start: apply unique card bonuses ────────────────────

    public override void OnRoundResolutionStart(
        RoundState round, MatchState match,
        FighterInstance ownerFighter, FighterInstance opponent, PersonaState state)
    {
        // Store the played unique ID for OnRoundResolutionComplete post-processing
        var pair     = ReferenceEquals(ownerFighter, match.FighterA) ? match.SelectedPairA : match.SelectedPairB;
        string? uid  = pair?.Unique?.Id;
        if (uid != null) state.CustomData[KeyPendingUnique] = uid;
        else             state.CustomData.Remove(KeyPendingUnique);

        var witchPos  = new HexCoord(ownerFighter.HexQ, ownerFighter.HexR);
        var oppPos    = new HexCoord(opponent.HexQ, opponent.HexR);
        var spirits   = GetSpiritPositions(state);

        int nearWitch  = spirits.Count(p => p.DistanceTo(witchPos) <= 2);
        int nearOpp    = spirits.Count(p => p.DistanceTo(oppPos) <= 2);
        bool onOppHex  = spirits.Contains(oppPos);
        int adjOpp     = spirits.Count(p => p.DistanceTo(oppPos) <= 1);

        switch (uid)
        {
            case "witch_u_spectral_strike":
                if (nearOpp > 0)
                {
                    ownerFighter.RoundPowerModifier += nearOpp;
                    round.Log.Add($"  [WITCH] Spectral Strike: {nearOpp} spirit(s) near opponent. +{nearOpp} Power.");
                }
                break;

            case "witch_u_spirit_ward":
                if (nearWitch > 0)
                {
                    ownerFighter.RoundDefenseModifier += nearWitch;
                    round.Log.Add($"  [WITCH] Spirit Ward: {nearWitch} spirit(s) nearby. +{nearWitch} Defense.");
                }
                break;

            case "witch_u_grave_hex":
                if (onOppHex)
                {
                    ownerFighter.RoundPowerModifier += 2;
                    round.Log.Add($"  [WITCH] Grave Hex: a spirit haunts the opponent's hex! +2 Power.");
                }
                break;

            case "witch_u_haunting_shroud":
                if (nearWitch >= 2)
                {
                    ownerFighter.RoundDefenseModifier += 2;
                    round.Log.Add($"  [WITCH] Haunting Shroud: {nearWitch} spirits envelop the Witch. +2 Defense.");
                }
                break;

            case "witch_u_soul_drain":
            {
                var spiritEffects = GetSpiritEffects(state);
                if (spiritEffects.Count > 0)
                {
                    var nearest = spiritEffects
                        .OrderBy(s => s.Position?.DistanceTo(witchPos) ?? int.MaxValue)
                        .First();
                    state.ActiveEffects.Remove(nearest);
                    ownerFighter.RoundPowerModifier += 3;
                    int remaining = GetSpiritPositions(state).Count;
                    round.Log.Add($"  [WITCH] Soul Drain: spirit consumed. +3 Power. ({remaining}/{MaxSpirits} spirits remain.)");
                }
                else
                {
                    round.Log.Add($"  [WITCH] Soul Drain: no spirits to consume.");
                }
                break;
            }

            case "witch_u_curse_chain":
                if (adjOpp >= 1)
                {
                    ownerFighter.RoundPowerModifier += 2;
                    round.Log.Add($"  [WITCH] Curse Chain: {adjOpp} spirit(s) adjacent to opponent! +2 Power.");
                }
                break;

            // witch_u_soul_swap — handled in OnRoundResolutionComplete
            // witch_u_phantom_rush — spirits get extra movement in OnRoundResolutionComplete
        }
    }

    // ── Round resolution complete: spirit damage, Soul Swap, spirit movement ──

    public override void OnRoundResolutionComplete(
        RoundState round, MatchState match,
        FighterInstance ownerFighter, FighterInstance opponent, PersonaState state)
    {
        var rng    = GetRng(state);
        var oppPos = new HexCoord(opponent.HexQ, opponent.HexR);

        // Step 1: spirits on opponent's hex → 2 random damage steps, then return to pool
        var damagingSpirits = GetSpiritEffects(state)
            .Where(s => s.Position.HasValue && s.Position.Value == oppPos)
            .ToList();
        foreach (var spirit in damagingSpirits)
        {
            round.Log.Add($"  [WITCH] A spirit haunts {opponent.DisplayName}'s hex! 2 random damage steps!");
            var targetable = opponent.LocationStates.Values
                .Where(ls => ls.State != DamageState.Disabled && ls.Location != BodyLocation.Stance)
                .ToList();
            for (int i = 0; i < 2; i++)
            {
                if (targetable.Count == 0) break;
                var loc = targetable[rng.Next(targetable.Count)];
                loc.ApplyDamage(1);
                round.Log.Add($"    {opponent.DisplayName}'s {loc.Location} takes spirit damage. ({loc.State})");
            }
            state.ActiveEffects.Remove(spirit);
            int remaining = GetSpiritPositions(state).Count;
            round.Log.Add($"  [WITCH] Spirit returns to the pool. ({remaining}/{MaxSpirits} on board)");
        }

        // Step 2: Soul Swap — witch teleports to nearest spirit, spirit comes here
        string? pendingUnique = state.CustomData.TryGetValue(KeyPendingUnique, out var pu) ? (string)pu : null;
        state.CustomData.Remove(KeyPendingUnique);

        if (pendingUnique == "witch_u_soul_swap")
        {
            var witchPos    = new HexCoord(ownerFighter.HexQ, ownerFighter.HexR);
            var spiritList  = GetSpiritEffects(state);

            if (spiritList.Count > 0)
            {
                var nearest    = spiritList.OrderBy(s => s.Position?.DistanceTo(witchPos) ?? int.MaxValue).First();
                var spiritPos  = nearest.Position!.Value;

                // Swap
                nearest.Position   = witchPos;
                ownerFighter.HexQ  = spiritPos.Q;
                ownerFighter.HexR  = spiritPos.R;
                round.Log.Add($"  [WITCH] Soul Swap: {ownerFighter.DisplayName} teleports {witchPos} → {spiritPos}. Spirit now at {witchPos}.");
            }
            else
            {
                round.Log.Add($"  [WITCH] Soul Swap: no spirits to swap with.");
            }
        }

        // Step 3: Move all spirits toward opponent (extra steps if Phantom Rush was played)
        int extraMoves = pendingUnique == "witch_u_phantom_rush" ? 2 : 0;
        MoveSpiritsTowardOpponent(state, match.Board, oppPos, SpiritMovePerRound + extraMoves, round);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        int inPlay    = GetSpiritPositions(state).Count;
        int inReserve = MaxSpirits - inPlay;
        var positions = GetSpiritPositions(state);
        string posStr = positions.Count > 0
            ? string.Join(" ", positions.Select(p => $"({p.Q},{p.R})"))
            : "none";
        return new()
        {
            $"WITCH | Spirits: {inPlay}/{MaxSpirits}  Reserve: {inReserve}",
            $"  At: {posStr}",
        };
    }

    // ── Board overlays (spirits rendered on the hex grid) ─────────────────────

    public override List<BoardOverlay> GetBoardOverlays(PersonaState state)
    {
        var result = new List<BoardOverlay>();
        foreach (var effect in GetSpiritEffects(state))
        {
            if (effect.Position.HasValue)
                result.Add(new BoardOverlay(effect.Position.Value, 160, 60, 220, 160, "~"));
        }
        return result;
    }

    // ── Spirit movement ───────────────────────────────────────────────────────

    private static void MoveSpiritsTowardOpponent(
        PersonaState state, HexBoard board, HexCoord oppPos, int steps, RoundState round)
    {
        var spirits  = GetSpiritEffects(state);
        if (spirits.Count == 0) return;

        // Track occupied-by-other-spirit positions (updated as each spirit moves)
        var occupied = spirits.Select(s => s.Position!.Value).ToList();

        foreach (var spirit in spirits)
        {
            var oldPos = spirit.Position!.Value;
            occupied.Remove(oldPos); // free this spirit's spot so others can use it

            var newPos = StepToward(oldPos, oppPos, steps, board, occupied);
            spirit.Position = newPos;
            occupied.Add(newPos); // mark new spot as taken

            if (newPos != oldPos)
                round.Log.Add($"  [WITCH] Spirit moves {oldPos} -> {newPos}.");
        }
    }

    /// <summary>
    /// Greedy hex pathfinding: step toward target, avoiding other spirits' positions.
    /// Spirits CAN land on fighter hexes (they do not block movement).
    /// </summary>
    private static HexCoord StepToward(
        HexCoord from, HexCoord target, int steps, HexBoard board, List<HexCoord> blocked)
    {
        var pos     = from;
        var visited = new HashSet<HexCoord> { from }; // prevent backtracking mid-move

        for (int i = 0; i < steps; i++)
        {
            if (pos == target) break;

            var best     = pos;
            int bestDist = pos.DistanceTo(target);

            for (int d = 0; d < 6; d++)
            {
                var n = pos.Neighbor(d);
                if (!board.IsValid(n))     continue;
                if (blocked.Contains(n))   continue; // another spirit is there
                if (visited.Contains(n))   continue; // already visited this move
                int dist = n.DistanceTo(target);
                if (dist < bestDist) { bestDist = dist; best = n; }
            }

            if (best == pos) break; // can't move closer
            visited.Add(best);
            pos = best;
        }

        return pos;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<PersonaArenaEffect> GetSpiritEffects(PersonaState state) =>
        state.ActiveEffects.Where(e => e.EffectType == SpiritEffectType).ToList();

    private static List<HexCoord> GetSpiritPositions(PersonaState state) =>
        state.ActiveEffects
             .Where(e => e.EffectType == SpiritEffectType && e.Position.HasValue)
             .Select(e => e.Position!.Value)
             .ToList();

    private static string HexToOptionId(HexCoord h) => $"hex_{h.Q}_{h.R}";

    private static bool TryParseHexOptionId(string id, out HexCoord hex)
    {
        hex = HexCoord.Zero;
        if (!id.StartsWith("hex_")) return false;
        var parts = id.Split('_');
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[1], out int q)) return false;
        if (!int.TryParse(parts[2], out int r)) return false;
        hex = new HexCoord(q, r);
        return true;
    }

    private static Random GetRng(PersonaState state)
    {
        if (state.CustomData.TryGetValue("rng", out var r) && r is Random rng) return rng;
        var fresh = new Random();
        state.CustomData["rng"] = fresh;
        return fresh;
    }
}
