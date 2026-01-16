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
    private bool sessionStarted = false;

    private bool reportCardShown = false;

    private CustomerServiceMetrics Metrics => customerServiceMetrics;

    public RushTimer rushTimer; // Reference to the RushTimer

    void Start()
    {
        // Ensure single source of truth references are wired even if not set in inspector
        if (customerServiceMetrics == null)
        {
            customerServiceMetrics = GetComponent<CustomerServiceMetrics>() ?? FindObjectOfType<CustomerServiceMetrics>(true);
        }

        if (reportCardUI == null)
        {
            reportCardUI = FindObjectOfType<ReportCardUI>(true);
        }

        // If using ConvaiNPC, let ConvaiCustomerServiceIntegration handle everything
        if (useConvaiNPC && convaiIntegration != null)
        {
            totalCustomers = convaiIntegration.GetCustomerCount();
            Debug.Log($"Using ConvaiNPC mode with {totalCustomers} customer NPCs");

            // Setup the ConvaiCustomerServiceIntegration
            convaiIntegration.rushSession = this;
            Debug.Log("ConvaiCustomerServiceIntegration configured");

            // Unify metrics source: RushSession.customerServiceMetrics is canonical
            if (customerServiceMetrics != null)
            {
                convaiIntegration.customerServiceMetrics = customerServiceMetrics;
            }
            
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
        // Only trigger the very first customer, then let the integration handle the loop
        if (rushTimer.isRunning && !sessionStarted)
        {
            sessionStarted = true;
            ShowFirstCustomer();
        }
    }

    private void ShowFirstCustomer()
    {
        if (useConvaiNPC && convaiIntegration != null)
        {
            customerInProgress = true;
            Debug.Log("Starting ConvaiNPC Rush Session. Handing control to ConvaiCustomerServiceIntegration.");
            convaiIntegration.TriggerCustomerComplaint(0); // Start the first interaction
        }
        else if (!useConvaiNPC)
        {
            // Logic for non-Convai mode if needed
        }
    }

    /// <summary>
    /// Called when a customer interaction is complete
    /// </summary>
    public void OnCustomerComplete()
    {
        if (useConvaiNPC)
        {
            // For ConvaiNPC mode, this means ALL customers are done
            Debug.Log("ConvaiNPC system reports all customers complete");
            served = totalCustomers;
            UpdateServedDisplay();
        }
        else
        {
            served++;
            UpdateServedDisplay();
        }

        // Check if the session is finished
        CheckForNextCustomer();
    }

    private void CheckForNextCustomer()
    {
        if (served < totalCustomers)
        {
            // If NOT using Convai, we would trigger the next hardcoded prompt here.
            // But if using Convai, the integration script is already handling the loop.
            if (!useConvaiNPC && rushTimer.isRunning)
            {
                customerInProgress = false;
                // You can add logic here for your legacy hardcoded prompts if needed
                Debug.Log("Moving to next hardcoded customer...");
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
        if (reportCardShown)
        {
            return;
        }

        if (customerServiceMetrics != null && reportCardUI != null)
        {
            Debug.Log("Generating and displaying report card...");

            // If something forgot to end the last interaction, end it now.
            // This avoids missing the last customer in the report.
            if (customerServiceMetrics.isTrackingInteraction)
            {
                customerServiceMetrics.EndCustomerInteraction();
            }
            
            // Generate the metrics report
            MetricsReport report = customerServiceMetrics.GenerateReportCard();
            
            // Display the report card
            reportCardUI.DisplayReportCard(report);

            reportCardShown = true;
        }
        else
        {
            Debug.LogWarning("CustomerServiceMetrics or ReportCardUI not assigned - cannot show report card");
        }
    }
}
