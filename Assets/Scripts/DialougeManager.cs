using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialougeController : MonoBehaviour
{
    [Header("UI References for Testing")]
    public TMP_Text dialogueText;      // Just the line of text to show
    public Button[] choiceButtons;     // Two buttons
    public TMP_Text[] choiceLabels;    // Their labels

    /// <summary>
    /// Presents a line and two options, then invokes onChoice(true) for the first (good) or false for the second (bad).
    /// </summary>
    public void PresentChoices(string npcLine, string goodOption, string badOption, Action<bool> onChoice)
    {
        // Show the line
        if (dialogueText != null)
        {
            dialogueText.text = npcLine;
            Debug.Log($"Updated dialogue text: {npcLine}");
        }

        // Pack & shuffle so positions swap each turn
        var options = new[]
        {
            new { text = goodOption, isGood = true },
            new { text = badOption,  isGood = false }
        };

        // simple Fisher–Yates shuffle
        for (int i = options.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = options[i];
            options[i] = options[j];
            options[j] = tmp;
        }

        // Wire up two buttons
        for (int i = 0; i < 2 && i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                // Activate the button gameobject
                choiceButtons[i].gameObject.SetActive(true);

                // Make sure the button component is enabled
                choiceButtons[i].enabled = true;

                // Update the button text
                if (choiceLabels[i] != null)
                {
                    choiceLabels[i].text = options[i].text;
                    Debug.Log($"Updated button {i} text: '{options[i].text}'");
                }

                // Clear previous listeners and add new one
                choiceButtons[i].onClick.RemoveAllListeners();

                // Debug: Log what we're setting up
                Debug.Log($"Button {i}: '{options[i].text}' -> isGood: {options[i].isGood}");

                // Capture the value properly to avoid closure issues
                SetupButton(choiceButtons[i], options[i].isGood, onChoice);
            }
        }

        Debug.Log("Response choices presented and buttons activated");
    }

    private void SetupButton(Button button, bool isGoodChoice, Action<bool> onChoice)
    {
        button.onClick.AddListener(() => HandleChoice(isGoodChoice, onChoice));
    }

    private void HandleChoice(bool wasGood, Action<bool> onChoice)
    {
        Debug.Log($"Choice made: wasGood = {wasGood}");

        // Hide buttons after choice is made
        HideChoiceButtons();

        onChoice?.Invoke(wasGood);
    }

    /// <summary>
    /// Hides the choice buttons when not needed
    /// </summary>
    public void HideChoiceButtons()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
        Debug.Log("Choice buttons hidden");
    }

    /// <summary>
    /// Clears the dialogue text
    /// </summary>
    public void ClearDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        HideChoiceButtons();
    }


    public void RestartScene()
    {
        // Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
