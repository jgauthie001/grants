namespace Grants.Models.Fighter;

/// <summary>
/// Immunity flags that can be applied to a fighter for the current round.
/// Populated by persona hooks (OnRoundResolutionStart) and stage hooks.
/// Cleared at the top of ResolutionEngine.ResolveFirstHalf before hooks fire.
/// 
/// Any engine or keyword effect should check the relevant flag before applying.
/// This allows future personas and stages to grant immunity cleanly without
/// altering individual keyword implementations.
/// </summary>
public enum CombatImmunity
{
    /// <summary>Cannot be moved away from attacker (Knockback, Push effects).</summary>
    Push,

    /// <summary>Cannot be moved toward attacker (CursePull, Pull effects).</summary>
    Pull,

    /// <summary>Defense cannot be reduced by attacker keywords (ArmorBreak, Piercing, CurseWeaken).</summary>
    DefenseReduction,

    /// <summary>Power cannot be reduced by attacker keywords or persona effects.</summary>
    PowerReduction,

    /// <summary>Speed cannot be reduced by attacker keywords or persona effects.</summary>
    SpeedReduction,

    /// <summary>Stagger keyword has no effect on this fighter.</summary>
    Stagger,

    /// <summary>Bleed keyword has no effect on this fighter.</summary>
    Bleed,

    /// <summary>Cannot receive Curse tokens (from The Cursed's pool transfer).</summary>
    CurseToken,
}
