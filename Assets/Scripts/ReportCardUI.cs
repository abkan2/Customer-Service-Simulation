using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ReportCardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject reportCardPanel;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI customersServedText;
    public TextMeshProUGUI averageTimeText;
    public TextMeshProUGUI satisfactionChangeText;
    public TextMeshProUGUI successRateText;
    public TextMeshProUGUI insightsText;
    public Button closeButton;
    
    [Header("Visual Effects")]
    public float typewriterSpeed = 0.05f;
    
    private void Start()
    {
        if (reportCardPanel != null)
            reportCardPanel.SetActive(false);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseReportCard);
    }
    
    public void DisplayReportCard(MetricsReport report)
    {
        if (reportCardPanel == null)
        {
            Debug.LogError("Report Card Panel not assigned!");
            return;
        }
        
        reportCardPanel.SetActive(true);
        StartCoroutine(AnimateReportCard(report));
    }
    
    private IEnumerator AnimateReportCard(MetricsReport report)
    {
        // Set grade with color
        if (gradeText != null)
        {
            gradeText.text = report.overallGrade.ToString();
            gradeText.color = GetGradeColor(report.overallGrade);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Set score
        if (scoreText != null)
        {
            scoreText.text = $"Score: {report.overallScore:F1}/100";
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // Set basic metrics
        if (customersServedText != null)
        {
            customersServedText.text = $"{report.totalCustomersServed}";
        }
        
        yield return new WaitForSeconds(0.3f);
        
        if (averageTimeText != null)
        {
            averageTimeText.text = $"{report.averageInteractionTime:F1}s";
        }
        
        yield return new WaitForSeconds(0.3f);
        
        if (satisfactionChangeText != null)
        {
            satisfactionChangeText.text = $"{report.averageSatisfactionChange}";
            satisfactionChangeText.color = report.averageSatisfactionChange >= 0 ? Color.green : Color.red;
        }
        
        yield return new WaitForSeconds(0.3f);
        
        if (successRateText != null)
        {
            successRateText.text = $"Success Rate: {report.successRate:F1}%";
            successRateText.color = report.successRate >= 70f ? Color.green : 
                                  report.successRate >= 50f ? Color.yellow : Color.red;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Animate insights
        if (insightsText != null && report.insights != null)
        {
            string fullInsights = string.Join("\n• ", report.insights);
            fullInsights = "• " + fullInsights;
            
            yield return StartCoroutine(TypewriterEffect(insightsText, fullInsights));
        }
        
        LogReportCard(report);
    }
    
    private IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";
        
        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
    
    private Color GetGradeColor(char grade)
    {
        switch (grade)
        {
            case 'A': return Color.green;
            case 'B': return new Color(0.5f, 1f, 0.5f); // Light green
            case 'C': return Color.yellow;
            case 'D': return new Color(1f, 0.5f, 0f); // Orange
            case 'F': return Color.red;
            default: return Color.white;
        }
    }
    
    public void CloseReportCard()
    {
        if (reportCardPanel != null)
            reportCardPanel.SetActive(false);
    }
    
    /// <summary>
    /// Test method for Unity UI Button - generates sample data for testing
    /// </summary>
    public void TestDisplayReportCard()
    {
        Debug.Log("Testing Report Card with sample data...");
        
        // Create sample test data
        MetricsReport testReport = new MetricsReport
        {
            totalCustomersServed = 4,
            averageInteractionTime = 45.3f,
            averageSatisfactionChange = 12.5f,
            totalChoicesMade = 8,
            successfulInteractions = 3,
            successRate = 75.0f,
            overallGrade = 'B',
            overallScore = 82.5f,
            insights = new System.Collections.Generic.List<string>
            {
                "Great customer satisfaction management!",
                "Very efficient interaction times.",
                "Strong customer service skills demonstrated."
            }
        };
        
        DisplayReportCard(testReport);
    }
    
    private void LogReportCard(MetricsReport report)
    {
        Debug.Log("=== CUSTOMER SERVICE REPORT CARD ===");
        Debug.Log($"Overall Grade: {report.overallGrade} ({report.overallScore:F1}/100)");
        Debug.Log($"Customers Served: {report.totalCustomersServed}");
        Debug.Log($"Average Interaction Time: {report.averageInteractionTime:F1}s");
        Debug.Log($"Average Satisfaction Change: {report.averageSatisfactionChange:+F1}");
        Debug.Log($"Success Rate: {report.successRate:F1}%");
        Debug.Log("Insights:");
        foreach (var insight in report.insights)
        {
            Debug.Log($"  • {insight}");
        }
        Debug.Log("=====================================");
    }
}
