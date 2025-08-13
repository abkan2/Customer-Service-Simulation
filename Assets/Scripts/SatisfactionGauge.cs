using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Slider))]
public class SatisfactionSlider : MonoBehaviour
{
    [Tooltip("Set maxValue to 100 in the Inspector")]
    public Slider slider;

    public GameObject goodSatisfaction;
    public GameObject badSatisfaction;

    private float currentSatisfaction = 50f; // Track the actual accumulated satisfaction

    void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        // Make sure slider range is 0–100
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 50f;  // Start at 50% satisfaction by default
        currentSatisfaction = slider.value;
    }

    /// <summary>
    /// Updates satisfaction based on choice and shows appropriate toast.
    /// </summary>
    public void UpdateSatisfaction(bool wasGood)
    {
        // Update accumulated satisfaction
        float oldSatisfaction = currentSatisfaction;
        
        if (wasGood)
        {
            currentSatisfaction = Mathf.Clamp(currentSatisfaction + 10f, 0f, 100f);
            Debug.Log($"Good response: satisfaction {oldSatisfaction} -> {currentSatisfaction} (+10)");
            StartCoroutine(ShowSatisfactionToast(goodSatisfaction));
        }
        else
        {
            currentSatisfaction = Mathf.Clamp(currentSatisfaction - 10f, 0f, 100f);
            Debug.Log($"Bad response: satisfaction {oldSatisfaction} -> {currentSatisfaction} (-10)");
            StartCoroutine(ShowSatisfactionToast(badSatisfaction));
        }
        
        // Update slider visual
        slider.value = currentSatisfaction;
    }

    private IEnumerator ShowSatisfactionToast(GameObject toast)
    {
        toast.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        toast.SetActive(false);
    }

    /// <summary>
    /// Gets the current satisfaction level for metrics tracking.
    /// </summary>
    /// <returns>Current satisfaction value (0-100)</returns>
    public int GetCurrentSatisfaction()
    {
        return Mathf.RoundToInt(currentSatisfaction);
    }
}
