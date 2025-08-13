using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enhanced response generation utility - generates contextual customer service responses
/// This class ONLY generates responses and does NOT handle NPC interaction or listening
/// </summary>
public class ConvaiResponseGenerator : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDetailedLogging = true; // Toggle for detailed response generation logs
    
    // Enums for better context analysis
    public enum EmotionLevel { Low, Medium, High }
    public enum UrgencyLevel { Low, Medium, High }
    public enum IssueType 
    { 
        OrderDelay, WrongOrder, Temperature, MilkType, StaffAttitude, 
        Pricing, Cleanliness, Size, Missing, WiFi, Noise, Seating, 
        Loyalty, Payment, ConversationEnd, Multiple, Unknown 
    }
    
    // Analysis result structure
    public class ComplaintAnalysis
    {
        public List<IssueType> detectedIssues = new List<IssueType>();
        public EmotionLevel emotionLevel = EmotionLevel.Medium;
        public UrgencyLevel urgencyLevel = UrgencyLevel.Medium;
        public bool mentionsTimeFrame = false;
        public bool isRepeatComplaint = false;
        public string originalText = "";
        public Dictionary<string, int> keywordCounts = new Dictionary<string, int>();
    }
    
    /// <summary>
    /// Checks if the complaint indicates the customer is done with their concerns
    /// </summary>
    /// <param name="complaint">The complaint text to analyze</param>
    /// <returns>True if the customer appears to be finished</returns>
    public bool IsConversationEnding(string complaint)
    {
        if (string.IsNullOrEmpty(complaint)) return false;
        
        var analysis = AnalyzeComplaint(complaint);
        bool isEnding = analysis.detectedIssues.Contains(IssueType.ConversationEnd);
        
        if (enableDetailedLogging)
        {
            Debug.Log($"[ResponseGen] Conversation ending check for: '{complaint}' → {isEnding}");
        }
        
        return isEnding;
    }
    
    /// <summary>
    /// Analyzes a complaint to understand context, emotion, and specific issues
    /// </summary>
    private ComplaintAnalysis AnalyzeComplaint(string complaint)
    {
        var analysis = new ComplaintAnalysis
        {
            originalText = complaint
        };
        
        string lowerComplaint = complaint.ToLower();
        
        // Detect specific issues
        analysis.detectedIssues = DetectIssueTypes(lowerComplaint);
        
        // Analyze emotion level
        analysis.emotionLevel = AnalyzeEmotionLevel(lowerComplaint);
        
        // Analyze urgency level
        analysis.urgencyLevel = AnalyzeUrgencyLevel(lowerComplaint);
        
        // Check for time frame mentions
        analysis.mentionsTimeFrame = ContainsTimeFrameKeywords(lowerComplaint);
        
        // Count keywords for better understanding
        analysis.keywordCounts = CountKeywords(lowerComplaint);
        
        return analysis;
    }
    
    private List<IssueType> DetectIssueTypes(string lowerComplaint)
    {
        var issues = new List<IssueType>();
        
        // More sophisticated pattern matching
        var issuePatterns = new Dictionary<IssueType, string[]>
        {
            { IssueType.OrderDelay, new[] { "wait", "waiting", "long", "time", "slow", "delayed", "taking forever", "still waiting", "how long" } },
            { IssueType.WrongOrder, new[] { "wrong", "mistake", "not what i ordered", "incorrect", "not mine", "different", "mix up" } },
            { IssueType.Temperature, new[] { "cold", "hot", "warm", "temperature", "lukewarm", "burning", "too hot", "too cold" } },
            { IssueType.MilkType, new[] { "milk", "dairy", "soy", "almond", "oat", "lactose", "non-dairy", "milk alternative" } },
            { IssueType.StaffAttitude, new[] { "rude", "attitude", "unprofessional", "dismissive", "ignored", "staff", "service", "employee" } },
            { IssueType.Pricing, new[] { "price", "cost", "expensive", "overpriced", "charge", "money", "bill", "receipt" } },
            { IssueType.Cleanliness, new[] { "dirty", "clean", "mess", "spill", "gross", "unsanitary", "filthy", "sticky" } },
            { IssueType.Size, new[] { "size", "small", "large", "medium", "wrong size", "bigger", "smaller" } },
            { IssueType.Missing, new[] { "missing", "forgot", "forgotten", "not included", "left out", "didn't get" } },
            { IssueType.WiFi, new[] { "wifi", "wi-fi", "internet", "connection", "network", "password" } },
            { IssueType.Noise, new[] { "noise", "loud", "music", "volume", "quiet", "sound" } },
            { IssueType.Seating, new[] { "seat", "table", "chair", "sitting", "spot", "place to sit" } },
            { IssueType.Loyalty, new[] { "reward", "points", "loyalty", "card", "account", "member" } },
            { IssueType.Payment, new[] { "pay", "payment", "card", "charge", "transaction", "billing", "refund" } },
            { IssueType.ConversationEnd, new[] { 
                "that's all", "thats all", "that's it", "thats it", "that'll be all", "thatll be all",
                "nothing else", "i'm good", "im good", "i'm done", "im done", "that's everything", "thats everything",
                "no more", "all good", "we're good", "were good", "that covers it", "finished",
                "for now", "that's all for now", "thats all for now", "enough for now", "done for now",
                "thank you", "thanks", "appreciate it", "satisfied", "all set", "good to go",
                "resolved", "fixed", "sorted", "handled", "taken care of"
            } }
        };
        
        foreach (var kvp in issuePatterns)
        {
            foreach (var pattern in kvp.Value)
            {
                if (lowerComplaint.Contains(pattern))
                {
                    if (!issues.Contains(kvp.Key))
                    {
                        issues.Add(kvp.Key);
                    }
                }
            }
        }
        
        // If multiple issues detected, add Multiple flag
        if (issues.Count > 1)
        {
            issues.Add(IssueType.Multiple);
        }
        
        // If no specific issues detected, mark as unknown
        if (issues.Count == 0)
        {
            issues.Add(IssueType.Unknown);
        }
        
        return issues;
    }
    
    private EmotionLevel AnalyzeEmotionLevel(string lowerComplaint)
    {
        var highEmotionWords = new[] { "terrible", "awful", "horrible", "worst", "hate", "furious", "outrageous", "ridiculous", "unacceptable", "disgusting" };
        var mediumEmotionWords = new[] { "frustrated", "upset", "annoyed", "disappointed", "unhappy", "dissatisfied", "irritated" };
        
        int highEmotionCount = highEmotionWords.Count(word => lowerComplaint.Contains(word));
        int mediumEmotionCount = mediumEmotionWords.Count(word => lowerComplaint.Contains(word));
        
        // Check for multiple exclamation marks or all caps (indicators of high emotion)
        bool hasMultipleExclamation = Regex.IsMatch(lowerComplaint, @"!{2,}");
        bool hasAllCaps = Regex.IsMatch(lowerComplaint, @"\b[A-Z]{3,}\b");
        
        if (highEmotionCount > 0 || hasMultipleExclamation || hasAllCaps)
            return EmotionLevel.High;
        else if (mediumEmotionCount > 0)
            return EmotionLevel.Medium;
        else
            return EmotionLevel.Low;
    }
    
    private UrgencyLevel AnalyzeUrgencyLevel(string lowerComplaint)
    {
        var urgentWords = new[] { "immediately", "right now", "urgent", "emergency", "asap", "quickly", "hurry", "need this now" };
        var timeIndicators = new[] { "late", "running late", "in a hurry", "meeting", "appointment", "flight" };
        
        int urgentCount = urgentWords.Count(word => lowerComplaint.Contains(word));
        int timeCount = timeIndicators.Count(word => lowerComplaint.Contains(word));
        
        if (urgentCount > 0)
            return UrgencyLevel.High;
        else if (timeCount > 0)
            return UrgencyLevel.Medium;
        else
            return UrgencyLevel.Low;
    }
    
    private bool ContainsTimeFrameKeywords(string lowerComplaint)
    {
        var timeFrameWords = new[] { "minutes", "hours", "ago", "waiting for", "been here", "since", "already" };
        return timeFrameWords.Any(word => lowerComplaint.Contains(word));
    }
    
    private Dictionary<string, int> CountKeywords(string lowerComplaint)
    {
        var keywords = new[] { "order", "wait", "wrong", "cold", "hot", "staff", "price", "dirty" };
        var counts = new Dictionary<string, int>();
        
        foreach (var keyword in keywords)
        {
            int count = Regex.Matches(lowerComplaint, $@"\b{keyword}\b").Count;
            if (count > 0)
            {
                counts[keyword] = count;
            }
        }
        
        return counts;
    }

    /// <summary>
    /// Enhanced response generation with better context analysis
    /// </summary>
    /// <param name="complaint">The full complaint text from the NPC</param>
    /// <param name="npcName">Name of the NPC for more personalized responses</param>
    /// <param name="conversationHistory">Previous exchanges for context (optional)</param>
    public string GenerateGoodResponse(string complaint, string npcName = "", List<string> conversationHistory = null)
    {
        if (string.IsNullOrEmpty(complaint))
        {
            return "I understand, let's see what we can do to get that sorted for you.";
        }

        // Analyze the complaint for better context
        var analysis = AnalyzeComplaint(complaint);
        
        if (enableDetailedLogging)
        {
            Debug.Log($"[ResponseGen] Analyzing complaint: '{complaint}'");
            Debug.Log($"[ResponseGen] Detected issues: {string.Join(", ", analysis.detectedIssues)}");
            Debug.Log($"[ResponseGen] Emotion: {analysis.emotionLevel}, Urgency: {analysis.urgencyLevel}");
        }

        // Try enhanced response first
        string enhancedResponse = GenerateContextualGoodResponse(analysis, npcName);
        if (!string.IsNullOrEmpty(enhancedResponse))
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[ResponseGen] Generated enhanced response: '{enhancedResponse}'");
            }
            return enhancedResponse;
        }

        // Fallback to original logic for compatibility
        return GenerateOriginalGoodResponse(complaint);
    }
    
    private string GenerateContextualGoodResponse(ComplaintAnalysis analysis, string npcName)
    {
        string customerAddress = !string.IsNullOrEmpty(npcName) ? npcName : "";
        string addressSuffix = !string.IsNullOrEmpty(customerAddress) ? $", {customerAddress}" : "";
        
        // Handle multiple issues in one complaint
        if (analysis.detectedIssues.Contains(IssueType.Multiple))
        {
            return $"I can see there are a few things going on here{addressSuffix}. Let me address each of these concerns for you.";
        }
        
        // Handle high emotion/urgency complaints first
        if (analysis.emotionLevel == EmotionLevel.High || analysis.urgencyLevel == UrgencyLevel.High)
        {
            string emotionalResponse = GetEmotionalGoodResponse(analysis, addressSuffix);
            if (!string.IsNullOrEmpty(emotionalResponse)) return emotionalResponse;
        }
        
        // Handle specific issues with context
        foreach (var issue in analysis.detectedIssues)
        {
            if (issue == IssueType.Multiple || issue == IssueType.Unknown) continue;
            
            string specificResponse = GetSpecificGoodResponse(issue, analysis, addressSuffix);
            if (!string.IsNullOrEmpty(specificResponse)) return specificResponse;
        }
        
        // Fallback response
        return $"I understand your concern{addressSuffix}. Let's work on getting this resolved for you right away.";
    }
    
    private string GetEmotionalGoodResponse(ComplaintAnalysis analysis, string addressSuffix)
    {
        if (analysis.emotionLevel == EmotionLevel.High)
        {
            return $"I can see this is really frustrating for you{addressSuffix}, and I completely understand. Let me take care of this personally.";
        }
        
        if (analysis.urgencyLevel == UrgencyLevel.High)
        {
            return $"I can see this needs immediate attention{addressSuffix}. Let me handle this right now.";
        }
        
        return "";
    }
    
    private string GetSpecificGoodResponse(IssueType issue, ComplaintAnalysis analysis, string addressSuffix)
    {
        switch (issue)
        {
            case IssueType.OrderDelay:
                if (analysis.mentionsTimeFrame)
                    return $"I see you've been waiting quite a while{addressSuffix}. Let me check on your order status immediately and get it expedited.";
                return $"Let me look into your order right away{addressSuffix} and see what's causing the delay.";
                
            case IssueType.WrongOrder:
                return $"I'll get that corrected for you immediately{addressSuffix}. We'll make sure you get exactly what you ordered.";
                
            case IssueType.Temperature:
                if (analysis.originalText.ToLower().Contains("cold"))
                    return $"I'll have a fresh, hot replacement ready for you in just a moment{addressSuffix}.";
                return $"Let me get that at the perfect temperature for you{addressSuffix}.";
                
            case IssueType.MilkType:
                return $"I'll get that remade with the correct milk type right away{addressSuffix}. We want to make sure it's exactly how you like it.";
                
            case IssueType.StaffAttitude:
                return $"I sincerely apologize for that experience{addressSuffix}. That's not the level of service we strive for, and I'll address this with the team.";
                
            case IssueType.Pricing:
                return $"I understand your concern about the pricing{addressSuffix}. Let me explain our value and see if there's anything I can do for you.";
                
            case IssueType.Cleanliness:
                return $"I'll have that area cleaned immediately{addressSuffix}. Thank you for bringing this to our attention.";
                
            case IssueType.Size:
                return $"I'll get that switched to the correct size for you right away{addressSuffix}.";
                
            case IssueType.Missing:
                return $"Let me grab that missing item for you immediately{addressSuffix}. I'll make sure you have everything you ordered.";
                
            case IssueType.WiFi:
                return $"I'll get you the correct network information{addressSuffix} and make sure you're connected properly.";
                
            case IssueType.Noise:
                return $"I can definitely adjust the volume for you{addressSuffix}. We want everyone to be comfortable.";
                
            case IssueType.Seating:
                return $"Let me find you a better spot{addressSuffix}. I'll check what tables we have available.";
                
            case IssueType.Loyalty:
                return $"I'll double-check your rewards account{addressSuffix} and make sure all your points are properly credited.";
                
            case IssueType.Payment:
                return $"Let me review that transaction for you{addressSuffix} and get this billing issue sorted out.";
                
            case IssueType.ConversationEnd:
                return $"Perfect! I'm glad we could get everything sorted out for you{addressSuffix}. Is there anything else I can help you with today?";
                
            default:
                return "";
        }
    }
    
    /// <summary>
    /// Original response logic for backward compatibility
    /// </summary>
    private string GenerateOriginalGoodResponse(string complaint)
    {
        if (string.IsNullOrEmpty(complaint))
        {
            return "I understand, let's see what we can do to get that sorted for you.";
        }

        string c = complaint.ToLower();

        if (c.Contains("order") || c.Contains("mobile") || c.Contains("app"))
        {
            return "I'll take a quick look to see where your order is in the queue and make sure it's moving along.";
        }
        else if (c.Contains("wait") || c.Contains("long") || c.Contains("time"))
        {
            return "I see the wait's been longer than expected. Let's try to get you taken care of soon.";
        }
        else if (c.Contains("wrong") || c.Contains("mistake") || c.Contains("name"))
        {
            return "Let me double-check and fix that so you get exactly what you ordered.";
        }
        else if (c.Contains("cold") || c.Contains("temperature") || c.Contains("hot"))
        {
            return "I can have that remade so it's at the right temperature for you.";
        }
        else if (c.Contains("milk") || c.Contains("dairy") || c.Contains("soy") || c.Contains("almond") || c.Contains("oat"))
        {
            return "I'll get that adjusted to your preferred milk type.";
        }
        else if (c.Contains("rude") || c.Contains("service") || c.Contains("staff"))
        {
            return "I'm sorry to hear that. I'll mention it to the team so we can improve.";
        }
        else if (c.Contains("price") || c.Contains("cost") || c.Contains("expensive"))
        {
            return "I understand your concern. We use premium ingredients, but I'll note your feedback.";
        }
        else if (c.Contains("dirty") || c.Contains("clean") || c.Contains("mess"))
        {
            return "I'll have someone check that area shortly.";
        }
        else if (c.Contains("size") || c.Contains("small") || c.Contains("large"))
        {
            return "I can switch that out for the size you wanted.";
        }
        else if (c.Contains("missing") || c.Contains("forgot") || c.Contains("not included"))
        {
            return "Let me grab the missing item for you right now.";
        }
        else if (c.Contains("wifi") || c.Contains("internet"))
        {
            return "I can confirm the network info for you if you'd like.";
        }
        else if (c.Contains("noise") || c.Contains("loud") || c.Contains("music"))
        {
            return "We can try lowering the volume a bit for you.";
        }
        else if (c.Contains("seat") || c.Contains("table") || c.Contains("chair"))
        {
            return "I can check if there's a cleaner table available.";
        }
        else if (c.Contains("reward") || c.Contains("points") || c.Contains("loyalty"))
        {
            return "I'll check your rewards and make sure they're added correctly.";
        }
        else if (c.Contains("pay") || c.Contains("card") || c.Contains("charge"))
        {
            return "I'll review the transaction so we can clear this up.";
        }
        else
        {
            return "Let's work on getting this resolved for you.";
        }
    }

    // Subtly poor responses — not openly rude, but dismissive, minimal effort, or avoiding responsibility
    public string GenerateBadResponse(string complaint, string npcName = "")
    {
        if (string.IsNullOrEmpty(complaint))
        {
            return "That happens sometimes, but it should be fine now.";
        }

        // Analyze the complaint for context (same analysis as good responses)
        var analysis = AnalyzeComplaint(complaint);
        
        if (enableDetailedLogging)
        {
            Debug.Log($"[ResponseGen] Generating bad response for: '{complaint}'");
            Debug.Log($"[ResponseGen] Issues detected: {string.Join(", ", analysis.detectedIssues)}");
        }

        // Try enhanced bad response first
        string enhancedResponse = GenerateContextualBadResponse(analysis, npcName);
        if (!string.IsNullOrEmpty(enhancedResponse))
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[ResponseGen] Generated enhanced bad response: '{enhancedResponse}'");
            }
            return enhancedResponse;
        }

        // Fallback to original logic
        return GenerateOriginalBadResponse(complaint);
    }
    
    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public string GenerateBadResponse(string complaint)
    {
        return GenerateBadResponse(complaint, "");
    }
    
    private string GenerateContextualBadResponse(ComplaintAnalysis analysis, string npcName)
    {
        // Handle high emotion complaints with more dismissive responses
        if (analysis.emotionLevel == EmotionLevel.High)
        {
            return "I understand you're upset, but these things happen in a busy place like this.";
        }
        
        // Handle urgent complaints with deflection
        if (analysis.urgencyLevel == UrgencyLevel.High)
        {
            return "We're doing our best, but we can only move so fast during peak hours.";
        }
        
        // Handle specific issues with dismissive context
        foreach (var issue in analysis.detectedIssues)
        {
            if (issue == IssueType.Multiple || issue == IssueType.Unknown) continue;
            
            string specificResponse = GetSpecificBadResponse(issue, analysis);
            if (!string.IsNullOrEmpty(specificResponse)) return specificResponse;
        }
        
        return ""; // Fallback to original logic
    }
    
    private string GetSpecificBadResponse(IssueType issue, ComplaintAnalysis analysis)
    {
        switch (issue)
        {
            case IssueType.OrderDelay:
                if (analysis.mentionsTimeFrame)
                    return "Orders do take time, especially when we're busy. It'll be ready when it's ready.";
                return "Mobile orders can take a while depending on the rush — yours should come up eventually.";
                
            case IssueType.WrongOrder:
                return "If it's close enough to what you ordered, it might be fine as is.";
                
            case IssueType.Temperature:
                return "The temperature changes quickly once it's made — that's just how coffee works.";
                
            case IssueType.MilkType:
                return "We usually use the standard milk unless you specifically mention otherwise.";
                
            case IssueType.StaffAttitude:
                return "I'm sure no one meant anything by it. Everyone's just trying to do their job.";
                
            case IssueType.Pricing:
                return "Our prices are standard for this area and reflect the quality of ingredients we use.";
                
            case IssueType.Cleanliness:
                return "It gets cleaned regularly — you might have just caught it before the next cleaning cycle.";
                
            case IssueType.Size:
                return "That's the standard size we serve for that drink — it's consistent with our menu.";
                
            case IssueType.Missing:
                return "Sometimes small things get missed during busy periods, but it shouldn't affect the overall experience.";
                
            case IssueType.WiFi:
                return "The Wi-Fi has its moments during peak hours. You might try reconnecting.";
                
            case IssueType.Noise:
                return "It's not unusually loud for this time of day — coffee shops tend to have ambient noise.";
                
            case IssueType.Seating:
                return "Seating just depends on who's here at the time. You're welcome to wait for something to open up.";
                
            case IssueType.Loyalty:
                return "If the points didn't go through this time, they'll probably add correctly next time.";
                
            case IssueType.Payment:
                return "Charges sometimes take a while to process properly — the system will sort itself out.";
                
            case IssueType.ConversationEnd:
                return "Alright then. Let me know if anything else comes up.";
                
            default:
                return "";
        }
    }
    
    /// <summary>
    /// Original bad response logic for backward compatibility
    /// </summary>
    private string GenerateOriginalBadResponse(string complaint)
    {
        if (string.IsNullOrEmpty(complaint))
        {
            return "That happens sometimes, but it should be fine now.";
        }

        string c = complaint.ToLower();

        if (c.Contains("order") || c.Contains("mobile") || c.Contains("app"))
        {
            return "Mobile orders can take a while depending on the rush — yours should come up eventually.";
        }
        else if (c.Contains("wait") || c.Contains("long") || c.Contains("time"))
        {
            return "It's just a busy time right now; we can't speed it up much.";
        }
        else if (c.Contains("wrong") || c.Contains("mistake") || c.Contains("name"))
        {
            return "If it's close enough, it might be fine as is.";
        }
        else if (c.Contains("cold") || c.Contains("temperature") || c.Contains("hot"))
        {
            return "The temperature changes quickly once it's made — that's normal.";
        }
        else if (c.Contains("milk") || c.Contains("dairy") || c.Contains("soy") || c.Contains("almond") || c.Contains("oat"))
        {
            return "We usually use the standard unless otherwise specified.";
        }
        else if (c.Contains("rude") || c.Contains("service") || c.Contains("staff"))
        {
            return "I'm sure no one meant anything by it.";
        }
        else if (c.Contains("price") || c.Contains("cost") || c.Contains("expensive"))
        {
            return "Our prices are standard for this area.";
        }
        else if (c.Contains("dirty") || c.Contains("clean") || c.Contains("mess"))
        {
            return "It gets cleaned regularly — maybe you just caught it before the next sweep.";
        }
        else if (c.Contains("size") || c.Contains("small") || c.Contains("large"))
        {
            return "That's the size we have listed for that drink.";
        }
        else if (c.Contains("missing") || c.Contains("forgot") || c.Contains("not included"))
        {
            return "Sometimes small things get left out, but it shouldn't affect much.";
        }
        else if (c.Contains("wifi") || c.Contains("internet"))
        {
            return "The Wi-Fi has its moments; maybe try reconnecting.";
        }
        else if (c.Contains("noise") || c.Contains("loud") || c.Contains("music"))
        {
            return "It's not unusually loud for this time of day.";
        }
        else if (c.Contains("seat") || c.Contains("table") || c.Contains("chair"))
        {
            return "Seating just depends on who's here at the time.";
        }
        else if (c.Contains("reward") || c.Contains("points") || c.Contains("loyalty"))
        {
            return "If it didn't go through, it'll probably add next time.";
        }
        else if (c.Contains("pay") || c.Contains("card") || c.Contains("charge"))
        {
            return "Charges sometimes take a while to update — it'll sort itself out.";
        }
        else
        {
            return "That's just how things go sometimes.";
        }
    }
}
