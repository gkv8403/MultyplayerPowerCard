/// <summary>
/// All available card abilities
/// </summary>
public enum AbilityType
{
    None,
    GainPoints,        // Add +2 points to player score
    StealPoints,       // Take 1 point from opponent
    BlockNextAttack,   // Nullify opponent's card power this turn
    DoublePower,       // Double this card's power
    DrawExtraCard      // Draw one additional card
}

/// <summary>
/// Helper class to convert string to AbilityType
/// </summary>
public static class AbilityTypeExtensions
{
    public static AbilityType ParseAbility(string abilityName)
    {
        switch (abilityName)
        {
            case "GainPoints": return AbilityType.GainPoints;
            case "StealPoints": return AbilityType.StealPoints;
            case "BlockNextAttack": return AbilityType.BlockNextAttack;
            case "DoublePower": return AbilityType.DoublePower;
            case "DrawExtraCard": return AbilityType.DrawExtraCard;
            default: return AbilityType.None;
        }
    }

    public static string GetDescription(this AbilityType ability)
    {
        switch (ability)
        {
            case AbilityType.GainPoints: return "+2 points";
            case AbilityType.StealPoints: return "Steal 1 point";
            case AbilityType.BlockNextAttack: return "Block opponent";
            case AbilityType.DoublePower: return "2x power";
            case AbilityType.DrawExtraCard: return "Draw +1 card";
            default: return "";
        }
    }
}