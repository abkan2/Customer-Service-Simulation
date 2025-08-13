using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.Attributes;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.PlayerStats;
using Convai.Scripts.Runtime.UI;
using Convai.Scripts.Runtime.Core;
using Grpc.Core;
using Service;
using TMPro;
using UnityEngine.Events;

public class RushSession : MonoBehaviour
{
    [Header("UI & Settings")]
    public DialougeController dialogueController; // your minimal UI script
    public SatisfactionSlider satisfactionSlider;
    public int totalCustomers = 5;

    [Header("ConvaiNPC Integration (Optional)")]
    public ConvaiResponseGenerator convaiResponseGenerator; // Response generator system
    public ConvaiCustomerServiceIntegration convaiIntegration; // NPC interaction system
    public bool useConvaiNPC = false; // Toggle between ConvaiNPC and hardcoded prompts

    [Header("Metrics & Report Card")]
    public CustomerServiceMetrics customerServiceMetrics; // Metrics tracking system
    public ReportCardUI reportCardUI; // Report card display system

    public TMP_Text numberServedText; 

    // Stub prompts to cycle through
    private readonly string[] prompts = new[]
    {
        "Hey, my mobile order's stuck on Ready—what's taking so long?",
        "I've been here 10 minutes waiting for a latte—this is ridiculous.",
        "You spelled my name wrong again!",
        "My drink's ice cold. Can you fix it?",
        "I asked for almond milk, not soy."
    };

    private int served = 0;
    private bool customerInProgress = false; // Prevent multiple customer starts

    public RushTimer rushTimer; // Reference to the RushTimer

    void Start()
    {
        // If using ConvaiNPC, let ConvaiCustomerServiceIntegration handle everything
        if (useConvaiNPC && convaiIntegration != null)
        {
            totalCustomers = convaiIntegration.GetCustomerCount();
            Debug.Log($"Using ConvaiNPC mode with {totalCustomers} customer NPCs");

            // Setup the ConvaiCustomerServiceIntegration
            convaiIntegration.rushSession = this;
            Debug.Log("ConvaiCustomerServiceIntegration configured");
            
            // Setup the response generator if available
            if (convaiResponseGenerator != null)
            {
                Debug.Log("ConvaiResponseGenerator configured");
            }
        }
        else if (useConvaiNPC)
        {
            Debug.LogWarning("ConvaiCustomerServiceIntegration not available, falling back to hardcoded prompts");
            useConvaiNPC = false;
        }

        // Don't start automatically - wait for timer
        
        // Initialize metrics system
        if (customerServiceMetrics != null)
        {
            customerServiceMetrics.ResetMetrics();
            Debug.Log("CustomerServiceMetrics initialized and reset");
        }
        
        // Initialize the served display
        UpdateServedDisplay();
    }
    
    /// <summary>
    /// Updates the TextMeshPro display to show served/total customers
    /// </summary>
    private void UpdateServedDisplay()
    {
        if (numberServedText != null)
        {
            numberServedText.text = $"{served}/{totalCustomers}";
        }
    }
    
    /// <summary>
    /// Public method to increment served count and update display (for ConvaiNPC integration)
    /// </summary>
    public void IncrementServedCount()
    {
        served++;
        customerInProgress = false; // Clear the flag when customer is complete
        UpdateServedDisplay();
    }

    void Update()
    {
        if (rushTimer.isRunning)
        {
            ShowNextCustomer();
        }
    }

    private void ShowNextCustomer()
    {
        if (served >= totalCustomers)
        {
            Debug.Log($"Rush complete: Served {served}/{totalCustomers}.");
            return;
        }

        // Prevent multiple simultaneous customer interactions
        if (customerInProgress)
        {
            return; // Customer interaction already in progress
        }

        // Check if we should use ConvaiNPC integration
        if (useConvaiNPC && convaiIntegration != null)
        {
            // Set flag to prevent multiple calls
            customerInProgress = true;
            
            // Let ConvaiCustomerServiceIntegration handle everything
            Debug.Log($"Starting ConvaiNPC system for customer {served + 1}");
            convaiIntegration.TriggerCustomerComplaint(served);
        }
    }


    /// <summary>
    /// Called when a customer interaction is complete (either from ConvaiNPC or hardcoded)
    /// This should ONLY be called when ALL customers in ConvaiCustomerServiceIntegration are done
    /// </summary>
    public void OnCustomerComplete()
    {
        if (useConvaiNPC)
        {
            // For ConvaiNPC mode, this means ALL customers are done
            Debug.Log("ConvaiNPC system reports all customers complete");
            served = totalCustomers; // Set to max to end the session
        }
        else
        {
            // For hardcoded mode, increment normally
            served++;
            UpdateServedDisplay(); // Update the UI display
        }

        // Brief delay before checking if we're done
        Invoke(nameof(CheckForNextCustomer), 2f);
    }

    private void CheckForNextCustomer()
    {
        if (served < totalCustomers)
        {
            // Continue with next customer if timer is still running
            if (rushTimer.isRunning)
            {
                ShowNextCustomer();
            }
        }
        else
        {
            Debug.Log($"All customers served! Final count: {served}/{totalCustomers}");
            ShowReportCard();
        }
    }

    /// <summary>
    /// Shows the report card when all customers have been served
    /// </summary>
    private void ShowReportCard()
    {
        if (customerServiceMetrics != null && reportCardUI != null)
        {
            Debug.Log("Generating and displaying report card...");
            
            // Generate the metrics report
            MetricsReport report = customerServiceMetrics.GenerateReportCard();
            
            // Display the report card
            reportCardUI.DisplayReportCard(report);
        }
        else
        {
            Debug.LogWarning("CustomerServiceMetrics or ReportCardUI not assigned - cannot show report card");
        }
    }
}
