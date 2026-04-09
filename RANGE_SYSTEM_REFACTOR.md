# Range System Refactor

## Summary of Changes

The range combat mechanic has been refactored to allow fighters more strategic control through **range modifiers on generic cards**.

### Old System
- Cards had a single `BaseRange` property
- All cards in a pair contributed to a single "effective range"
- Limited tactical variation (all attacks either in/out of range)

### New System
- **UniqueCards & SpecialCards** define a base attack range
- **GenericCards** provide range modifiers (+/- adjustments)
- **Effective Range** = UniqueCard.BaseRange + GenericCard.RangeModifier
- Attacks fail if `currentDistance > effectiveRange`

---

## How It Works

### Range Values

```
Adjacent  = 1 hex
Close     = 2 hexes
Mid       = 3 hexes
Far       = 4+ hexes
```

### Example Combinations

| Generic | Modifier | Unique | Base Range | Effective Range | Distance | Hit? |
|---------|----------|--------|------------|-----------------|----------|------|
| Jab | 0 | Haymaker | Adjacent (1) | 1 | 1 | ✓ |
| Jab | 0 | Haymaker | Adjacent (1) | 1 | 2 | ✗ |
| Footwork | +1 | Haymaker | Adjacent (1) | 2 | 2 | ✓ |
| Footwork | +1 | BullRush | Close (2) | 3 | 3 | ✓ |
| Jab | 0 | BullRush | Close (2) | 2 | 3 | ✗ |

## Tactical Impact

### Short-Range Combos (Power)
```
Generic: Jab (0 modifier) + Unique: Haymaker (Adjacent)
= 1 hex range
→ High risk, high reward (must be adjacent to hit)
→ Trades distance for extra damage
```

### Mid-Range Combos (Balanced)
```
Generic: Footwork (+1 modifier) + Unique: SideStep (Close)
= 3 hex range
→ Good for maintaining pressure while staying mobile
```

### Long-Range Combos (Reach)
```
Generic: Footwork (+1 modifier) + Unique: BullRush (Close)
= 3 hex range
→ Allows staying at range while building momentum
```

---

## Implementation Details

### CardPair.EffectiveRange

```csharp
public int EffectiveRangeValue
{
    get
    {
        int baseRange = (Unique?.BaseRange ?? Special?.BaseRange ?? RangeBracket.Adjacent) 
            => converts enum to int (1, 2, 3, 4)
        
        int modifier = Generic?.RangeModifier ?? 0;
        int effectiveRange = baseRange + modifier;
        
        return Math.Max(1, effectiveRange);  // Minimum Adjacent (1)
    }
}
```

### Attack Resolution

AttackEngine.Resolve() checks:
```csharp
int requiredRange = attackerPair.EffectiveRangeValue;  // Combine unique+generic
if (attackerPair.AllKeywords.ContainsKeyword(CardKeyword.Lunge))
    requiredRange++;  // Lunge provides +1 bonus

result.InRange = currentDistance <= requiredRange;
if (!result.InRange)
    return false;  // Miss!
```

---

## Generic Card Modifiers (Grants Fighter)

Currently all Grants generics use standard modifiers:

| Card | Body Part | Modifier | Notes |
|------|-----------|----------|-------|
| Head Strike | Head | 0 | Standard reach |
| Body Turn | Torso | 0 | Standard reach |
| Left Jab | Left Arm | 0 | Standard reach |
| Right Cross | Right Arm | 0 | Standard reach |
| Left Step | Left Leg | 0 | Standard reach |
| Right Kick | Right Leg | 0 | Standard reach |
| Center Drive | Core | 0 | Standard reach |
| Footwork | Stance | +1 | Extended reach (footwork advantage) |

### Future Expansion Ideas

Modifiers enable new card designs:

**Defensive Cards (Modifier -1)**
- "Pulling Guard" — Short range but high defense
- Encourages defensive positioning

**Aggressive Cards (Modifier +1)**
- "Long Step" — Extended reach attacks
- Allows poking from distance

**Neutral Cards (Modifier 0)**
- Baseline reach for most techniques

---

## Keywords That Affect Range

### Lunge (+1 range)
- Provided as a card keyword
- Overrides range limits temporarily
- Example: Haymaker does 1 hex, but with Lunge does 2 hex

### Future Possibilities
- **Knockback Path** — Range affects knockback destination
- **Range Penalty** — Stage modifier that reduces range
- **Extended Reach** — Upgrade that adds +1 modifier

---

## Testing Range Behavior

### Test Case 1: Standard Attack
```
Fighter A: Jab (0) + Haymaker (Adjacent=1)
Fighter B: Distance = 1 hex
→ In range, attack lands
```

### Test Case 2: Out of Range
```
Fighter A: Jab (0) + Haymaker (Adjacent=1)
Fighter B: Distance = 2 hexes
→ Out of range, attack misses
```

### Test Case 3: Extended Reach
```
Fighter A: Footwork (+1) + Haymaker (Adjacent=1) = 2 hexes
Fighter B: Distance = 2 hexes
→ In range (just barely), attack lands
```

### Test Case 4: Lunge Bonus
```
Fighter A: Jab (0) + BullRush (Close=2, has Lunge)
Total range = 2 + 1 (Lunge) = 3 hexes
Fighter B: Distance = 3 hexes
→ In range, attack lands
```

---

## Notes for Designers

When creating new fighters or cards:

1. **Unique/Special cards define base range** — Think about attack intent
   - Close-range slugger? Use Adjacent
   - Mid-range striker? Use Close
   - Long-range projectile? Use Mid/Far

2. **Generic cards modify range** — Think about positioning strategy
   - Aggressive stance? Use +1 (Footwork)
   - Defensive stance? Use -1 (future: Guard Crouch)
   - Neutral stance? Use 0 (standard cards)

3. **Combinations create archetypes**
   - All Adjacent base + 0 modifiers = Brawler (close fighter)
   - Close/Mid base + +1 modifiers = Striker (mid-range pressure)
   - Far base + 0 modifiers = Zoner (distance control)

---

## Files Modified

- `Models/Cards/GenericCard.cs` — Added `RangeModifier` property
- `Models/Cards/CardPair.cs` — Changed `EffectiveRange` to combine unique+generic
- `Engine/AttackEngine.cs` — Updated to use `EffectiveRangeValue`
- `Fighters/Grants/GrantsFighter.cs` — Removed `BaseRange` from generic cards, added `RangeModifier`

## Build Status
✅ 0 errors, 0 warnings
