using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a card instance in the game
/// Wraps CardData and adds runtime state
/// </summary>
public class Card
{
    public CardData data;
    public bool isSelected = false;
    public bool isPlayed = false;

    // Runtime modifications (for abilities like DoublePower)
    public int modifiedPower;
    public int modifiedCost;

    public Card(CardData cardData)
    {
        this.data = cardData;
        this.modifiedPower = cardData.power;
        this.modifiedCost = cardData.cost;
    }

    /// <summary>
    /// Get card ID
    /// </summary>
    public int ID => data.id;

    /// <summary>
    /// Get card name
    /// </summary>
    public string Name => data.name;

    /// <summary>
    /// Get current power (might be modified by abilities)
    /// </summary>
    public int Power => modifiedPower;

    /// <summary>
    /// Get current cost (might be modified)
    /// </summary>
    public int Cost => modifiedCost;

    /// <summary>
    /// Get abilities
    /// </summary>
    public List<string> Abilities => data.abilities;

    /// <summary>
    /// Check if card has specific ability
    /// </summary>
    public bool HasAbility(string abilityName)
    {
        return data.HasAbility(abilityName);
    }

    /// <summary>
    /// Check if card has any ability
    /// </summary>
    public bool HasAnyAbility()
    {
        return data.abilities != null && data.abilities.Count > 0;
    }

    /// <summary>
    /// Get parsed abilities as AbilityType enum
    /// </summary>
    public List<AbilityType> GetAbilityTypes()
    {
        List<AbilityType> types = new List<AbilityType>();
        if (data.abilities != null)
        {
            foreach (string abilityName in data.abilities)
            {
                AbilityType type = AbilityTypeExtensions.ParseAbility(abilityName);
                if (type != AbilityType.None)
                {
                    types.Add(type);
                }
            }
        }
        return types;
    }

    /// <summary>
    /// Double the power (for DoublePower ability)
    /// </summary>
    public void DoublePower()
    {
        modifiedPower = data.power * 2;
        Debug.Log($"{Name} power doubled: {data.power} → {modifiedPower}");
    }

    /// <summary>
    /// Reset power to original
    /// </summary>
    public void ResetPower()
    {
        modifiedPower = data.power;
    }

    /// <summary>
    /// Reset all modifications
    /// </summary>
    public void ResetModifications()
    {
        modifiedPower = data.power;
        modifiedCost = data.cost;
        isSelected = false;
        isPlayed = false;
    }

    /// <summary>
    /// Clone this card
    /// </summary>
    public Card Clone()
    {
        return new Card(data);
    }

    public override string ToString()
    {
        return $"{Name} (Cost:{Cost}, Power:{Power})";
    }
}