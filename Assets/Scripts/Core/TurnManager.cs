using UnityEngine;

/// <summary>
/// Manages turn timing and flow
/// Separated from GameManager for modularity
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("Settings")]
    public float turnTimeLimit = 30f;
    public bool autoSubmitOnTimeout = true;

    [Header("Current State")]
    public float timeRemaining;
    public bool isTimerActive = false;
    public int currentTurn = 0;
    public int maxTurns = 6;

    private void Update()
    {
        if (isTimerActive)
        {
            timeRemaining -= Time.deltaTime;

            // Update timer display every frame
            GameEvents.TriggerTurnTimerUpdated(timeRemaining);

            // Check for timeout
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                OnTimerExpired();
            }
        }
    }

    /// <summary>
    /// Start the turn timer
    /// </summary>          
    public void StartTimer()
    {
        timeRemaining = turnTimeLimit;
        isTimerActive = true;
        Debug.Log($"Turn timer started: {turnTimeLimit}s");
    }

    /// <summary>
    /// Stop the turn timer
    /// </summary>
    public void StopTimer()
    {
        isTimerActive = false;
        Debug.Log("Turn timer stopped");
    }

    /// <summary>
    /// Reset the timer
    /// </summary>
    public void ResetTimer()
    {
        timeRemaining = turnTimeLimit;
        isTimerActive = false;
    }

    /// <summary>
    /// Called when timer reaches 0
    /// </summary>
    private void OnTimerExpired()
    {
        isTimerActive = false;
        Debug.Log("⏱️ Turn timer expired!");

        if (autoSubmitOnTimeout && GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnTimerExpired();
        }
    }

    /// <summary>
    /// Get remaining time as percentage (0-1)
    /// </summary>
    public float GetTimePercentage()
    {
        return Mathf.Clamp01(timeRemaining / turnTimeLimit);
    }

    /// <summary>
    /// Get formatted time string (MM:SS)
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Check if in warning time (last 10 seconds)
    /// </summary>
    public bool IsWarningTime()
    {
        return timeRemaining <= 10f && timeRemaining > 0f;
    }

    /// <summary>
    /// Increment turn counter
    /// </summary>
    public void IncrementTurn()
    {
        currentTurn++;
        Debug.Log($"Turn incremented: {currentTurn}/{maxTurns}");
    }

    /// <summary>
    /// Check if game should end
    /// </summary>
    public bool ShouldEndGame()
    {
        return currentTurn >= maxTurns;
    }

    /// <summary>
    /// Reset turn manager
    /// </summary>
    public void Reset()
    {
        currentTurn = 0;
        ResetTimer();
    }
}