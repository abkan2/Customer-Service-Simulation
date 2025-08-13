using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
// If you’re using TextMeshPro instead of UI.Text, uncomment the line below and the TMP_Text field
// using TMPro;

[RequireComponent(typeof(Canvas))]
public class RushTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Duration of the rush in seconds (default: 600s = 10min)")]
    public float duration = 600f;

    [Header("UI References")]
    [Tooltip("Drag your UI Text (or TMP_Text) component here")]
    public TextMeshProUGUI timerText;
    // public TMP_Text timerText;  // for TextMeshPro users, comment out the UI.Text field above

    [Header("Events")]
    [Tooltip("Called when the timer reaches zero")]
    public UnityEvent onTimerEnd;

    private float remainingTime;
    public bool isRunning = false;

    public void GameStarted()
    {
        ResetTimer();
        StartTimer();
    }

    

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            onTimerEnd?.Invoke();
        }

        UpdateUIText();
    }

    /// <summary>
    /// Begins the countdown.
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
    }

    /// <summary>
    /// Stops the countdown (pauses it).
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Resets the timer back to the full duration.
    /// </summary>
    public void ResetTimer()
    {
        remainingTime = duration;
        UpdateUIText();
    }

    /// <summary>
    /// Formats remainingTime as MM:SS and writes it to the UI.
    /// </summary>
    private void UpdateUIText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
