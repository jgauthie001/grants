using Grants.Fighters.Grants;

namespace Grants.Models.Cards;

/// <summary>
/// Global registry of all available card templates.
/// Cards here are never modified — always call Clone() (via CloneX helpers) before assigning to a fighter.
/// When a new fighter is created, they start with empty card lists and the builder
/// lets the user pick from this pool.
/// </summary>
public static class CardPool
{
    public static IReadOnlyList<GenericCard> Generics { get; }
    public static IReadOnlyList<UniqueCard>  Uniques  { get; }
    public static IReadOnlyList<SpecialCard> Specials { get; }

    static CardPool()
    {
        Generics = new List<GenericCard>
        {
            GrantsFighter.G_Head,
            GrantsFighter.G_Torso,
            GrantsFighter.G_LeftArm,
            GrantsFighter.G_RightArm,
            GrantsFighter.G_LeftLeg,
            GrantsFighter.G_RightLeg,
            GrantsFighter.G_Core,
            GrantsFighter.G_Stance,
        }.AsReadOnly();

        Uniques = new List<UniqueCard>
        {
            GrantsFighter.U_Haymaker,
            GrantsFighter.U_Clinch,
            GrantsFighter.U_CrossCounter,
            GrantsFighter.U_BullRush,
            GrantsFighter.U_LowSweep,
            GrantsFighter.U_Overhand,
            GrantsFighter.U_SideStep,
            GrantsFighter.U_BodyShot,
        }.AsReadOnly();

        Specials = new List<SpecialCard>
        {
            GrantsFighter.S_Obliterator,
            GrantsFighter.S_BerserkRush,
            GrantsFighter.S_KillTest,
        }.AsReadOnly();
    }

    /// <summary>Clone a generic from the pool with a fighter-scoped ID.</summary>
    public static GenericCard CloneGeneric(GenericCard template, string fighterId)
        => template.Clone(FighterScopedId(fighterId, template.Id));

    /// <summary>Clone a unique from the pool with a fighter-scoped ID.</summary>
    public static UniqueCard CloneUnique(UniqueCard template, string fighterId)
        => template.Clone(FighterScopedId(fighterId, template.Id));

    /// <summary>Clone a special from the pool with a fighter-scoped ID.</summary>
    public static SpecialCard CloneSpecial(SpecialCard template, string fighterId)
        => template.Clone(FighterScopedId(fighterId, template.Id));

    /// <summary>
    /// Compute the per-fighter card ID.
    /// If the template id already starts with the fighter prefix (canonical fighters),
    /// it is used as-is so existing save data and upgrade trees are unaffected.
    /// </summary>
    public static string FighterScopedId(string fighterId, string templateId)
    {
        if (templateId.StartsWith(fighterId + "_")) return templateId;
        return $"{fighterId}_{templateId}";
    }
}
