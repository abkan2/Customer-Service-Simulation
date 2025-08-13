using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CustomerInteraction
{
    public float startTime;
    public float endTime;
    public string complaintType;
    public int satisfactionStart;
    public int satisfactionEnd;
    public int choicesMade;
    public bool wasSuccessful;
}

[System.Serializable]
public class MetricsReport
{
    // Basic Performance
    public int totalCustomersServed;
    public float averageInteractionTime;
    public float averageSatisfactionChange;
    public int totalChoicesMade;
    
    // Success Metrics
    public int successfulInteractions;
    public float successRate;
    
    // Grading
    public char overallGrade;
    public float overallScore;
    public List<string> insights;
}

public class CustomerServiceMetrics : MonoBehaviour
{
    [Header("Metrics Data")]
    public List<CustomerInteraction> customerInteractions = new List<CustomerInteraction>();
    
    [Header("Current Session")]
    public CustomerInteraction currentInteraction;
    public bool isTrackingInteraction = false;
    
    [Header("References")]
    public SatisfactionSlider satisfactionSlider;
    
    private void Start()
    {
        if (satisfactionSlider == null)
        {
            satisfactionSlider = FindObjectOfType<SatisfactionSlider>();
        }
    }
    
    public void StartCustomerInteraction(string complaintType = "general")
    {
        if (isTrackingInteraction)
        {
            Debug.LogWarning("Already tracking an interaction. Ending previous one.");
            EndCustomerInteraction();
        }
        
        currentInteraction = new CustomerInteraction
        {
            startTime = Time.time,
            complaintType = complaintType,
            satisfactionStart = satisfactionSlider ? satisfactionSlider.GetCurrentSatisfaction() : 50,
            choicesMade = 0,
            wasSuccessful = false
        };
        
        isTrackingInteraction = true;
        Debug.Log($"Started tracking interaction with complaint type: {complaintType}");
    }
    
    public void OnPlayerChoice()
    {
        if (isTrackingInteraction && currentInteraction != null)
        {
            currentInteraction.choicesMade++;
        }
    }
    
    public void EndCustomerInteraction()
    {
        if (!isTrackingInteraction || currentInteraction == null) return;
        
        currentInteraction.endTime = Time.time;
        currentInteraction.satisfactionEnd = satisfactionSlider ? satisfactionSlider.GetCurrentSatisfaction() : currentInteraction.satisfactionStart;
        
        // Determine if interaction was successful (satisfaction improved or stayed high)
        currentInteraction.wasSuccessful = currentInteraction.satisfactionEnd >= currentInteraction.satisfactionStart || 
                                         currentInteraction.satisfactionEnd >= 70;
        
        customerInteractions.Add(currentInteraction);
        isTrackingInteraction = false;
        
        Debug.Log($"Ended interaction. Duration: {currentInteraction.endTime - currentInteraction.startTime:F1}s, " +
                 $"Satisfaction: {currentInteraction.satisfactionStart} → {currentInteraction.satisfactionEnd}");
    }
    
    public MetricsReport GenerateReportCard()
    {
        var report = new MetricsReport();
        report.insights = new List<string>();
        
        if (customerInteractions.Count == 0)
        {
            report.overallGrade = 'F';
            report.overallScore = 0f;
            report.insights.Add("No customer interactions completed.");
            return report;
        }
        
        // Basic Performance Metrics
        report.totalCustomersServed = customerInteractions.Count;
        
        float totalTime = 0f;
        float totalSatisfactionChange = 0f;
        int totalChoices = 0;
        int successCount = 0;
        
        foreach (var interaction in customerInteractions)
        {
            totalTime += interaction.endTime - interaction.startTime;
            totalSatisfactionChange += interaction.satisfactionEnd - interaction.satisfactionStart;
            totalChoices += interaction.choicesMade;
            if (interaction.wasSuccessful) successCount++;
        }
        
        report.averageInteractionTime = totalTime / customerInteractions.Count;
        report.averageSatisfactionChange = totalSatisfactionChange / customerInteractions.Count;
        report.totalChoicesMade = totalChoices;
        
        // Success Metrics
        report.successfulInteractions = successCount;
        report.successRate = (float)successCount / customerInteractions.Count * 100f;
        
        // Calculate Overall Score (0-100)
        float satisfactionScore = Mathf.Clamp01((report.averageSatisfactionChange + 50f) / 100f) * 40f; // 40% weight
        float successScore = report.successRate * 0.6f; // 60% weight
        
        report.overallScore = satisfactionScore + successScore;
        
        // Assign Grade
        if (report.overallScore >= 90f) report.overallGrade = 'A';
        else if (report.overallScore >= 80f) report.overallGrade = 'B';
        else if (report.overallScore >= 70f) report.overallGrade = 'C';
        else if (report.overallScore >= 60f) report.overallGrade = 'D';
        else report.overallGrade = 'F';
        
        // Generate Insights
        GenerateInsights(report);
        
        return report;
    }
    
    private void GenerateInsights(MetricsReport report)
    {
        // Success Rate Insights
        if (report.successRate >= 80f)
            report.insights.Add("Excellent customer satisfaction management!");
        else if (report.successRate >= 60f)
            report.insights.Add("Good customer service, with room for improvement.");
        else
            report.insights.Add("Focus on better understanding customer needs.");
        
        // Interaction Time Insights
        if (report.averageInteractionTime < 30f)
            report.insights.Add("Very efficient interaction times.");
        else if (report.averageInteractionTime > 60f)
            report.insights.Add("Consider being more decisive in responses.");
        
        // Satisfaction Change Insights
        if (report.averageSatisfactionChange > 10f)
            report.insights.Add("Great at improving customer satisfaction!");
        else if (report.averageSatisfactionChange < -10f)
            report.insights.Add("Work on maintaining customer satisfaction levels.");
        
        // Overall Performance
        switch (report.overallGrade)
        {
            case 'A':
                report.insights.Add("Outstanding customer service performance!");
                break;
            case 'B':
                report.insights.Add("Strong customer service skills demonstrated.");
                break;
            case 'C':
                report.insights.Add("Satisfactory performance with growth potential.");
                break;
            case 'D':
                report.insights.Add("Basic customer service skills need development.");
                break;
            case 'F':
                report.insights.Add("Significant improvement needed in customer service approach.");
                break;
        }
    }
    
    public void ResetMetrics()
    {
        customerInteractions.Clear();
        isTrackingInteraction = false;
        currentInteraction = null;
        Debug.Log("Metrics reset.");
    }
}
