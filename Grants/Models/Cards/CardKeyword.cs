namespace Grants.Models.Cards;

/// <summary>
/// Keywords that modify how a card resolves. Applied to both generic and unique cards.
/// </summary>
public enum CardKeyword
{
    None,

    // Damage modifiers
    Bleed,          // Target location takes +1 damage next turn (stacks)
    ArmorBreak,     // Reduces target's defense on the hit location by 1 this round
    Piercing,       // Ignores half of the defender's defense stat
    Crushing,       // Advances damage state by one extra step

    // Speed/timing modifiers
    Feint,          // Forces opponent to reveal their speed stat before committing
    Quickstep,      // Adds +1 to combined speed for this turn only
    Lunge,          // +1 range this turn, -1 defense this turn

    // Control
    Stagger,        // On hit: opponent's cooldowns all increase by 1 next turn
    Disrupt,        // On hit: opponent's unique card choice is cancelled (generic resolves alone)
    Knockback,      // On hit: pushes opponent 1 hex directly away

    // Defensive
    Guard,          // +2 defense this turn, cannot be used to attack
    Parry,          // If opponent attacks the same body part: counter triggers at +1 power
    Deflect,        // On being hit: redirect half damage to a random other location
    MaxDamageCap,   // On being hit: incoming damage cannot advance this location past the cap state (value: 1=Bruised, 2=Injured)

    // Positional
    Sidestep,       // Allows diagonal movement regardless of standard move rules
    Press,          // After landing hit: may move 1 hex toward opponent as free action
    Retreat,        // Movement cannot be prevented this turn

    // Post-attack movement (attacker repositions after their own attack)
    Recoil,         // Attacker retreats N hexes after their attack regardless of outcome
    FollowThrough,  // Attacker advances N hexes toward opponent, but only on a landed hit
    Disengage,      // Attacker retreats N hexes when their attack is out of range or misses

    // Debug/Testing
    Kill,           // Instantly disables all opponent body parts (TEST ONLY)

    // Curse persona interactions (The Cursed character)
    CurseGain,      // On hit: gain 1 extra Curse token to owner's pool (on top of base mechanic)
    CursePull,      // On hit: pull opponent N hexes toward self (N = opponent's curse token count)
    CurseEmpower,   // This attack gains +N power (N = owner's curse pool count)
    CurseWeaken,    // Reduce opponent's defense by N this attack (N = their curse token count)

    // Chivalrous persona interactions
    Pull,           // On hit: pull opponent 1 hex toward attacker
    ChivalryBonus,  // On hit: +N extra damage steps if defender holds >=1 chivalry token (N = value)
    DistanceGuard,  // When defending: if current distance >= value, gain +2 defense
}
