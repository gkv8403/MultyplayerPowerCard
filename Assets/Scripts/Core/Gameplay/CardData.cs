using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single card's data
/// </summary>
[Serializable]
public class CardData
{
    public int id;
    public string name;
    public int cost;
    public int power;
    public List<string> abilities;

    public CardData(int id, string name, int cost, int power, List<string> abilities)
    {
        this.id = id;
        this.name = name;
        this.cost = cost;
        this.power = power;
        this.abilities = abilities ?? new List<string>();
    }

    // Default constructor for JSON deserialization
    public CardData()
    {
        abilities = new List<string>();
    }

    public bool HasAbility(string abilityName)
    {
        return abilities != null && abilities.Contains(abilityName);
    }

    public override string ToString()
    {
        string abilitiesStr = abilities != null && abilities.Count > 0
            ? string.Join(", ", abilities)
            : "None";
        return $"{name} (ID:{id}) | Cost:{cost} | Power:{power} | Abilities:[{abilitiesStr}]";
    }
}

/// <summary>
/// Container for loading card array from JSON
/// </summary>
[Serializable]
public class CardDataList
{
    public List<CardData> cards;
}