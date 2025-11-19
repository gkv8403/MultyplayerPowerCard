using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves card abilities during the resolution phase
/// </summary>
public class AbilityResolver : MonoBehaviour
{
    public static AbilityResolver Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Resolve all abilities for both players
    /// </summary>
    public void ResolveAllAbilities(PlayerState player1, PlayerState player2)
    {
        Debug.Log("=== Resolving Abilities ===");

        // Phase 1: Process immediate abilities (DoublePower, BlockNextAttack)
        ProcessImmediateAbilities(player1);
        ProcessImmediateAbilities(player2);

        // Phase 2: Calculate final power (considering blocks)
        int player1Power = CalculateTotalPower(player1, player2.isBlockedThisTurn);
        int player2Power = CalculateTotalPower(player2, player1.isBlockedThisTurn);

        // Phase 3: Add power to scores
        player1.AddScore(player1Power);
        player2.AddScore(player2Power);

        // Phase 4: Process point manipulation abilities
        ProcessPointAbilities(player1, player2);

        // Phase 5: Process card draw abilities
        ProcessDrawAbilities(player1);
        ProcessDrawAbilities(player2);

        Debug.Log($"{player1.playerName} total power: {player1Power}");
        Debug.Log($"{player2.playerName} total power: {player2Power}");
    }

    /// <summary>
    /// Process immediate abilities (DoublePower, BlockNextAttack)
    /// </summary>
    private void ProcessImmediateAbilities(PlayerState player)
    {
        foreach (Card card in player.playedCardsThisTurn)
        {
            foreach (AbilityType ability in card.GetAbilityTypes())
            {
                switch (ability)
                {
                    case AbilityType.DoublePower:
                        card.DoublePower();
                        GameEvents.TriggerAbilityTriggered(ability, card.data, player.playerName);
                        GameEvents.TriggerPowerDoubled(card.data.power, card.Power, player.playerName);
                        break;

                    case AbilityType.BlockNextAttack:
                        // Mark opponent as blocked
                        PlayerState opponent = GetOpponent(player);
                        if (opponent != null)
                        {
                            opponent.isBlockedThisTurn = true;
                            GameEvents.TriggerAbilityTriggered(ability, card.data, player.playerName);
                            GameEvents.TriggerBlockActivated(player.playerName);
                            Debug.Log($"{player.playerName} blocked {opponent.playerName}'s attack!");
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Calculate total power for a player
    /// </summary>
    private int CalculateTotalPower(PlayerState player, bool isBlocked)
    {
        if (isBlocked)
        {
            Debug.Log($"{player.playerName} is blocked! Power nullified.");
            return 0;
        }

        int totalPower = 0;
        foreach (Card card in player.playedCardsThisTurn)
        {
            totalPower += card.Power; // Uses modified power if doubled
        }

        return totalPower;
    }

    /// <summary>
    /// Process point manipulation abilities (GainPoints, StealPoints)
    /// </summary>
    private void ProcessPointAbilities(PlayerState player1, PlayerState player2)
    {
        // Process player 1's abilities
        foreach (Card card in player1.playedCardsThisTurn)
        {
            foreach (AbilityType ability in card.GetAbilityTypes())
            {
                switch (ability)
                {
                    case AbilityType.GainPoints:
                        player1.AddScore(2);
                        GameEvents.TriggerAbilityTriggered(ability, card.data, player1.playerName);
                        GameEvents.TriggerPointsGained(2, player1.playerName);
                        Debug.Log($"{player1.playerName} gained 2 points from {card.Name}");
                        break;

                    case AbilityType.StealPoints:
                        player2.RemoveScore(1);
                        player1.AddScore(1);
                        GameEvents.TriggerAbilityTriggered(ability, card.data, player1.playerName);
                        GameEvents.TriggerPointsStolen(1, player2.playerName, player1.playerName);
                        Debug.Log($"{player1.playerName} stole 1 point from {player2.playerName}");
                        break;
                }
            }
        }

        // Process player 2's abilities
        foreach (Card card in player2.playedCardsThisTurn)
        {
            foreach (AbilityType ability in card.GetAbilityTypes())
            {
                switch (ability)
                {
                    case AbilityType.GainPoints:
                        player2.AddScore(2);
                        GameEvents.TriggerAbilityTriggered(ability, card.data, player2.playerName);
                        GameEvents.TriggerPointsGained(2, player2.playerName);
                        Debug.Log($"{player2.playerName} gained 2 points from {card.Name}");
                        break;

                    case AbilityType.StealPoints:
                        player1.RemoveScore(1);
                        player2.AddScore(1);
                        GameEvents.TriggerAbilityTriggered(ability, card.data, player2.playerName);
                        GameEvents.TriggerPointsStolen(1, player1.playerName, player2.playerName);
                        Debug.Log($"{player2.playerName} stole 1 point from {player1.playerName}");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Process card draw abilities
    /// </summary>
    private void ProcessDrawAbilities(PlayerState player)
    {
        foreach (Card card in player.playedCardsThisTurn)
        {
            foreach (AbilityType ability in card.GetAbilityTypes())
            {
                if (ability == AbilityType.DrawExtraCard)
                {
                    player.DrawCard();
                    GameEvents.TriggerAbilityTriggered(ability, card.data, player.playerName);
                    GameEvents.TriggerExtraCardDrawn(player.playerName);
                    Debug.Log($"{player.playerName} drew an extra card from {card.Name}");
                }
            }
        }
    }

    /// <summary>
    /// Get opponent player
    /// </summary>
    private PlayerState GetOpponent(PlayerState player)
    {
        if (GameManager.Instance == null) return null;

        if (player == GameManager.Instance.hostPlayer)
            return GameManager.Instance.clientPlayer;
        else
            return GameManager.Instance.hostPlayer;
    }

    /// <summary>
    /// Resolve a single ability (for testing or special cases)
    /// </summary>
    public void ResolveSingleAbility(AbilityType ability, Card card, PlayerState caster, PlayerState target)
    {
        switch (ability)
        {
            case AbilityType.GainPoints:
                caster.AddScore(2);
                break;

            case AbilityType.StealPoints:
                if (target != null)
                {
                    target.RemoveScore(1);
                    caster.AddScore(1);
                }
                break;

            case AbilityType.BlockNextAttack:
                if (target != null)
                {
                    target.isBlockedThisTurn = true;
                }
                break;

            case AbilityType.DoublePower:
                card.DoublePower();
                break;

            case AbilityType.DrawExtraCard:
                caster.DrawCard();
                break;
        }

        GameEvents.TriggerAbilityTriggered(ability, card.data, caster.playerName);
    }
}