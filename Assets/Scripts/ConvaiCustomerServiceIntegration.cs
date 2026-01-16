using UnityEngine;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Integrates ConvaiNPC with the Customer Service Simulator
/// </summary>
public class ConvaiCustomerServiceIntegration : MonoBehaviour
{
    [Header("ConvaiNPC Integration")]
    public ConvaiNPC[] customerNPCs; // Array of different customer NPCs with backstories
    public RushSession rushSession; // Reference to your existing rush session
    public ChatBoxUI playerChat; // Reference to the chat UI
    public CustomerServiceMetrics customerServiceMetrics; // Metrics tracking system
    
    [Header("Interaction Settings")]
    public float responseTimeout = 15f; // How long to wait for NPC response
    public float delayBetweenCustomers = 2f; // Pause between customers
    public float apiCallDelay = 2f; // Minimum delay between API calls to prevent rate limiting
    
    [Header("UI Elements")]
    public GameObject transitionScreen; // Transition screen for customer changes
    public CanvasGroup transitionCanvasGroup; // Canvas group for smooth fading
    public float fadeDuration = 1f; // Duration for fade in/out animations
    
    [Header("NPC Positioning")]
    public Transform npcAnchorPoint; // Transform to position NPCs at (more flexible than hardcoded coords)
    public Vector3 npcPositionOffset = Vector3.zero; // Optional offset from anchor point
    public Vector3 npcRotationEuler = new Vector3(0f, 98.835f, 0f); // NPC rotation in Euler angles

    private bool isWaitingForNPCResponse = false;
    private int currentCustomerIndex = 0;
    private ConvaiNPC currentCustomerNPC;
    private string capturedComplaint = ""; // Captured complaint text for dynamic responses
    private readonly List<ChatBoxUI> _subscribedChatBoxes = new();
    private int complaintExchangeCount = 0; // Track number of complaint exchanges
    private const int maxComplaintExchanges = 3; // Maximum exchanges before moving to next customer
    private float lastApiCallTime = 0f; // Track last API call time for rate limiting

    public GameObject welcomeCanvas;


    private CustomerServiceMetrics Metrics =>
    (rushSession != null && rushSession.customerServiceMetrics != null)
        ? rushSession.customerServiceMetrics
        : customerServiceMetrics;

    void Start()
    {
        // Single source of truth: prefer RushSession metrics if available
        if (rushSession != null && rushSession.customerServiceMetrics != null)
        {
            customerServiceMetrics = rushSession.customerServiceMetrics;
        }

        // Validate NPC array
        if (customerNPCs == null || customerNPCs.Length == 0)
        {
            Debug.LogError("No customer NPCs assigned! Please assign NPCs in the inspector.");
            return;
        }

        // Log available NPCs
        Debug.Log($"ConvaiCustomerServiceIntegration initialized with {customerNPCs.Length} customer NPCs");
        for (int i = 0; i < customerNPCs.Length; i++)
        {
            if (customerNPCs[i] != null)
            {
                Debug.Log($"Customer NPC {i + 1}: {customerNPCs[i].characterName}");
            }
        }

        // Initialize the system
        StartCoroutine(InitializeConvaiSystem());
    }

    private IEnumerator InitializeConvaiSystem()
    {
        // Wait for ConvaiNPC components to be ready
        yield return new WaitForSeconds(0.5f);

        // Find and set up ChatBoxUI if not assigned
        if (playerChat == null)
        {
            playerChat = FindObjectOfType<ChatBoxUI>(true);
            if (playerChat != null)
            {
                Debug.Log("Found ChatBoxUI automatically");
                RegisterChatBox(playerChat);
            }
        }

        Debug.Log("ConvaiCustomerServiceIntegration initialized successfully");
    }

    private void RegisterChatBox(ChatBoxUI chatBox)
    {
        if (!_subscribedChatBoxes.Contains(chatBox))
        {
            _subscribedChatBoxes.Add(chatBox);
            Debug.Log($"Registered new ChatBoxUI: {chatBox.GetInstanceID()}");
            
            // Set as primary if we don't have one
            if (playerChat == null)
            {
                playerChat = chatBox;
            }
        }
    }
    

    /// <summary>
    /// Starts interaction with the current customer using ConvaiNPC
    /// </summary>
    public void StartCustomerInteraction(int customerIndex)
    {
        StartCoroutine(StartCustomerInteractionCoroutine(customerIndex));
    }
    
    private IEnumerator StartCustomerInteractionCoroutine(int customerIndex)
    {
        Debug.Log($"=== [INTERACTION START] Customer {customerIndex} ===");
        
        // Validate customer index and NPC array
        if (customerNPCs == null || customerIndex >= customerNPCs.Length)
        {
            Debug.LogWarning($"Customer index {customerIndex} out of range or no NPCs assigned. Array length: {customerNPCs?.Length ?? 0}");
            yield return null;
        }

        currentCustomerIndex = customerIndex;
        currentCustomerNPC = customerNPCs[customerIndex];

        if (currentCustomerNPC == null)
        {
            Debug.LogError($"Customer NPC at index {customerIndex} is null!");
            PresentPlayerChoicesDirectly();
            yield return null;
        }

        Debug.Log($"Starting interaction with Customer {customerIndex + 1}/{customerNPCs.Length}: {currentCustomerNPC.characterName}");

        // MUST CLEAN UP PREVIOUS NPC FIRST
        yield return StartCoroutine(EnsureCleanNPCState());

        // Activate the current NPC and deactivate others
        ActivateCurrentNPC();
        
        // POSITION AND ACTIVATE
        PositionCurrentNPC();
        SubscribeToCurrentNPCAudio();

        // IMPORTANT: Wait for Convai's internal NPCManager to recognize the new active NPC
        // before sending the first prompt. This prevents the "Cancelled" GRPC error.
        yield return new WaitForSeconds(0.5f); 

        // Start metrics tracking for this customer interaction
        if (Metrics != null)
        {
            string complaintType = GetComplaintTypeFromNPC(currentCustomerNPC);
            Metrics.StartCustomerInteraction(complaintType);
            Debug.Log($"Started metrics tracking for customer interaction: {complaintType}");
        }

        // Send an initial prompt to get the NPC to start complaining (with rate limiting)
        string initialPrompt = "You're a customer at a coffee shop. Please tell me about your problem or complaint.";
        yield return StartCoroutine(SendTextToNPCWithRateLimit(currentCustomerNPC, initialPrompt, "Initial Prompt"));
        isWaitingForNPCResponse = true;
        complaintExchangeCount = 0; // Reset exchange counter

        // Start waiting for response
        StartCoroutine(WaitForNPCResponse());
    }

    private void SubscribeToCurrentNPCAudio()
    {
        if (currentCustomerNPC?.AudioManager != null)
        {
            // Unsubscribe from previous NPC if any
            UnsubscribeFromNPCAudio();
            
            // Subscribe to the current NPC's audio transcript
            currentCustomerNPC.AudioManager.OnAudioTranscriptAvailable += HandleAITranscript;
            Debug.Log($"Subscribed to audio transcript for {currentCustomerNPC.characterName}");
        }
        else
        {
            Debug.LogWarning("Current NPC or AudioManager is null");
        }
    }

    private void UnsubscribeFromNPCAudio()
    {
        // Unsubscribe from all NPCs to avoid duplicate events
        foreach (var npc in customerNPCs)
        {
            if (npc?.AudioManager != null)
            {
                npc.AudioManager.OnAudioTranscriptAvailable -= HandleAITranscript;
            }
        }
    }

    /// <summary>
    /// Handles AI transcript from ConvaiNPC AudioManager - this is the proper way to capture speech
    /// </summary>
    private void HandleAITranscript(string aiText)
    {
        if (string.IsNullOrEmpty(aiText))
        {
            Debug.Log("[AI Transcript] Received empty transcript");
            return;
        }

        // Clean the transcript (remove extra quotes, fix contractions, etc.)
        string cleanedText = CleanTranscript(aiText);
        Debug.Log($"[AI Transcript] Raw: '{aiText}' → Cleaned: '{cleanedText}'");

        // Always capture transcript if we're waiting for response, regardless of keywords
        if (isWaitingForNPCResponse)
        {
            // Check if this looks like a customer complaint
            if (ContainsComplaintKeywords(cleanedText.ToLower()))
            {
                capturedComplaint = cleanedText;
                Debug.Log($"✓ [AI Transcript] Captured complaint (keyword match): {capturedComplaint}");
            }
            else
            {
                // Even if no keywords match, still capture if it's substantial content
                if (cleanedText.Length > 10) // Minimum meaningful length
                {
                    capturedComplaint = cleanedText;
                    Debug.Log($"✓ [AI Transcript] Captured substantial content (no keywords): {capturedComplaint}");
                }
                else
                {
                    Debug.Log($"⚠ [AI Transcript] Short text, no keywords: '{cleanedText}'");
                }
            }
        }
        else
        {
            Debug.Log($"[AI Transcript] Not waiting for response, ignoring: '{cleanedText}'");
        }
    }

    /// <summary>
    /// Cleans the AI transcript by removing extra quotes and fixing contractions
    /// </summary>
    private string CleanTranscript(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return rawText;

        // Remove double quotes that sometimes appear
        string cleaned = rawText.Replace("''", "'");
        
        // Basic contraction fixes that are commonly mispronounced by AI
        var contractions = new Dictionary<string, string>
        {
            {"we'll", "we will"}, {"i'll", "i will"}, {"you'll", "you will"},
            {"we're", "we are"}, {"i'm", "i am"}, {"you're", "you are"},
            {"don't", "do not"}, {"won't", "will not"}, {"can't", "cannot"},
            {"isn't", "is not"}, {"aren't", "are not"}, {"wasn't", "was not"},
            {"doesn't", "does not"}, {"didn't", "did not"},
            {"haven't", "have not"}, {"hasn't", "has not"},
            {"wouldn't", "would not"}, {"shouldn't", "should not"},
            {"couldn't", "could not"}, {"that's", "that is"}
        };

        foreach (var kvp in contractions)
        {
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b",
                kvp.Value,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return cleaned;
    }

    private void ActivateCurrentNPC()
    {
        // 1. Deactivate ALL NPCs in the manager's eyes first
        foreach (var npc in customerNPCs)
        {
            if (npc != null)
            {
                npc.isCharacterActive = false;
                // If the NPC has a Group Controller, tell it to stop talking
                if (npc.AudioManager != null) npc.AudioManager.StopAllAudioPlayback();
            }
        }
        
        // 2. Small yield for the ConvaiNPCManager to process the "No one is active" state
        // (This happens automatically if you do this at the start of the Coroutine)
        
        // 3. Activate only the current customer NPC
        if (currentCustomerNPC != null)
        {
            currentCustomerNPC.gameObject.SetActive(true);
            currentCustomerNPC.isCharacterActive = true;
            Debug.Log($"Activated {currentCustomerNPC.characterName}");
        }
    }

    private IEnumerator WaitForNPCResponse()
    {
        float elapsed = 0f;
        capturedComplaint = ""; // Reset captured complaint
        
        Debug.Log("Starting to wait for NPC response...");
        
        // Wait for NPC to start talking (if they haven't yet)
        while (currentCustomerNPC != null && !currentCustomerNPC.IsCharacterTalking && elapsed < 10f)
        {
            Debug.Log($"Waiting for NPC to start talking... ({elapsed:F1}s)");
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        if (elapsed >= 10f)
        {
            Debug.LogWarning("Timeout waiting for NPC to start talking");
        }
        else
        {
            Debug.Log("NPC started talking");
        }
        
        // Reset elapsed for next phase
        elapsed = 0f;
        
        // Wait for NPC to finish talking
        while (currentCustomerNPC != null && currentCustomerNPC.IsCharacterTalking && elapsed < responseTimeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (elapsed >= responseTimeout)
        {
            Debug.LogWarning("NPC response timeout, proceeding with choices");
        }
        else
        {
            Debug.Log("NPC finished talking");
        }
        
        // NOW stop waiting for response (so HandleAITranscript stops processing new transcripts)
        isWaitingForNPCResponse = false;
        
        // Give MORE time for final transcript processing (this is crucial!)
        Debug.Log("Waiting for transcript processing to complete...");
        yield return new WaitForSeconds(3f); // Increased from 1f to 3f
        
        // Check multiple times if we have captured complaint
        int attempts = 0;
        while (string.IsNullOrEmpty(capturedComplaint) && attempts < 5)
        {
            Debug.Log($"Attempt {attempts + 1}: Checking for captured complaint...");
            yield return new WaitForSeconds(1f);
            attempts++;
        }
        
        // Log what we captured
        if (!string.IsNullOrEmpty(capturedComplaint))
        {
            Debug.Log($"✓ Successfully captured complaint: '{capturedComplaint}'");
        }
        else
        {
            Debug.LogWarning("⚠ No complaint text captured after multiple attempts, using fallback");
            capturedComplaint = "Customer has made a complaint about the service";
        }

        // Present the response choices to the player
        PresentPlayerChoicesDirectly();
    }
    
    /// <summary>
    /// Checks if the text contains keywords that indicate it's a customer complaint
    /// </summary>
    private bool ContainsComplaintKeywords(string text)
    {
        string[] complaintKeywords = {
            // Original keywords
            "order", "mobile", "app", "wait", "waiting", "long", "time", "slow",
            "wrong", "mistake", "name", "cold", "hot", "temperature", "warm",
            "milk", "dairy", "soy", "almond", "oat", "lactose",
            "rude", "service", "staff", "attitude", "unprofessional",
            "price", "cost", "expensive", "charge", "money",
            "dirty", "clean", "mess", "spill", "bathroom",
            "problem", "issue", "complaint", "upset", "angry", "frustrated",
            "terrible", "awful", "horrible", "worst", "bad", "disappointed",
            
            // Enhanced WiFi and connectivity keywords
            "wifi", "wi-fi", "internet", "connection", "network", "password", 
            "connect", "connected", "connecting", "disconnect", "disconnected",
            "signal", "speed", "slow internet", "can't connect", "won't connect",
            "login", "access", "network name", "router", "bandwidth",
            
            // Additional service keywords
            "noise", "loud", "music", "volume", "quiet", "sound",
            "seat", "table", "chair", "sitting", "spot", "place",
            "reward", "points", "loyalty", "card", "account", "member",
            "pay", "payment", "transaction", "billing", "refund",
            "size", "small", "large", "medium", "bigger", "smaller",
            "missing", "forgot", "forgotten", "not included", "left out"
        };
        
        int keywordMatches = 0;
        List<string> foundKeywords = new List<string>();
        
        foreach (string keyword in complaintKeywords)
        {
            if (text.Contains(keyword))
            {
                keywordMatches++;
                foundKeywords.Add(keyword);
            }
        }
        
        bool hasKeywords = keywordMatches > 0;
        
        if (hasKeywords)
        {
            Debug.Log($"[Keyword Detection] Found {keywordMatches} keyword(s): {string.Join(", ", foundKeywords)}");
        }
        else
        {
            Debug.Log($"[Keyword Detection] No complaint keywords found in: '{text}'");
        }
        
        return hasKeywords;
    }

    private void PresentPlayerChoicesDirectly()
    {
        Debug.Log($"=== PresentPlayerChoicesDirectly called ===");
        
        // Ensure we have the ConvaiResponseGenerator available
        if (rushSession == null || rushSession.convaiResponseGenerator == null)
        {
            Debug.LogError("ConvaiResponseGenerator not available! Cannot present player choices.");
            return;
        }

        var responseGenerator = rushSession.convaiResponseGenerator;
        
        // Get the NPC name for more personalized responses
        string npcName = currentCustomerNPC?.characterName ?? "";
        
        Debug.Log($"📝 Final captured complaint: '{capturedComplaint}'");
        Debug.Log($"👤 NPC Name for context: '{npcName}'");
        Debug.Log($"📊 Complaint length: {capturedComplaint?.Length ?? 0} characters");
        
        // Check if this is a conversation ending statement
        bool isConversationEnding = responseGenerator.IsConversationEnding(capturedComplaint);
        if (isConversationEnding)
        {
            Debug.Log($"🏁 [Conversation End] Customer appears to be finished: '{capturedComplaint}'");
            
            // Generate a final response 
            string finalResponse = responseGenerator.GenerateGoodResponse(capturedComplaint, npcName);
            Debug.Log($"✅ Final Response: '{finalResponse}'");
            
            // Present the final response with a simple acknowledgment choice
            if (rushSession.dialogueController != null)
            {
                rushSession.dialogueController.PresentChoices(
                    capturedComplaint,
                    finalResponse,
                    "Thank you for coming in today.", // Alternative closing
                    wasGood =>
                    {
                        Debug.Log($"🎯 [Conversation End] Player chose final response, completing customer");
                        
                        // Track player choice in metrics
                        if (Metrics != null)
                        {
                            Metrics.OnPlayerChoice();
                        }
                        
                        // Update satisfaction (conversation ending is always positive)
                        if (rushSession.satisfactionSlider != null)
                        {
                            rushSession.satisfactionSlider.UpdateSatisfaction(true);
                        }
                        
                        // Send the chosen response to NPC if available
                        string chosenResponse = wasGood ? finalResponse : "Thank you for coming in today.";
                        if (currentCustomerNPC != null)
                        {
                            StartCoroutine(SendTextToNPCWithRateLimit(currentCustomerNPC, chosenResponse, "Final Response"));
                            Debug.Log($"Sent final response to NPC: {chosenResponse}");
                        }
                        
                        // Complete the interaction
                        StartCoroutine(CompleteAfterDelay(2.0f)); // Give time for the response to be processed
                    }
                );
            }
            else
            {
                Debug.Log("🎯 [Conversation End] No dialogue controller, completing immediately");
                OnCustomerInteractionComplete();
            }
            return;
        }
        
        // Generate enhanced responses using the improved ConvaiResponseGenerator with context
        string goodOption = responseGenerator.GenerateGoodResponse(capturedComplaint, npcName);
        string badOption = responseGenerator.GenerateBadResponse(capturedComplaint, npcName);

        Debug.Log($"✅ Generated Good Response: '{goodOption}'");
        Debug.Log($"❌ Generated Bad Response: '{badOption}'");

        // Use the captured complaint or a generic prompt
        string customerPrompt = !string.IsNullOrEmpty(capturedComplaint) 
            ? capturedComplaint 
            : $"{currentCustomerNPC?.characterName ?? "Customer"} has made their complaint. How do you respond?";
        
        Debug.Log($"💬 Customer Prompt: '{customerPrompt}'");
        
        // Use the existing dialogue controller
        if (rushSession.dialogueController != null)
        {
            Debug.Log("Presenting choices to dialogue controller...");
            rushSession.dialogueController.PresentChoices(
                customerPrompt,
                goodOption,
                badOption,
                wasGood =>
                {
                    Debug.Log($"Player chose: {(wasGood ? "Good" : "Bad")} response");
                    
                    // Track player choice in metrics
                    if (Metrics != null)
                    {
                        Metrics.OnPlayerChoice();
                    }
                    
                    // Update satisfaction using existing system
                    if (rushSession.satisfactionSlider != null)
                    {
                        rushSession.satisfactionSlider.UpdateSatisfaction(wasGood);
                    }
                    
                    // Construct the response string
                    string playerResponse = wasGood ? goodOption : badOption;

                    // COMBINED PROMPT: Append the transition question to the player's choice 
                    // This creates a single API call instead of two, preventing GRPC race conditions.
                    // We only do this if we aren't on the very last complaint exchange.
                    if (complaintExchangeCount < maxComplaintExchanges - 1)
                    {
                        playerResponse += " What else is bothering you? Please tell me clearly.";
                    
                    }

                    if (currentCustomerNPC != null)
                    {
                        // Send the bundled text as one single data packet
                        StartCoroutine(SendTextToNPCWithRateLimit(currentCustomerNPC, playerResponse, "Combined Player Response"));
                        
                        // Simply wait for the NPC's natural reaction/next complaint
                        StartCoroutine(PromptForNextComplaint());
                    }
                    else
                    {
                        // Notify rush session that interaction is complete if no NPC
                        OnCustomerInteractionComplete();
                    }
                }
            );
        }
        else
        {
            Debug.LogError("DialogueController not available! Cannot present choices.");
        }
    }

    private IEnumerator PromptForNextComplaint()
    {
        // 1. Increment the exchange count for the turn just completed
        complaintExchangeCount++;
        
        // 2. Check if we've reached the maximum exchanges
        if (complaintExchangeCount >= maxComplaintExchanges)
        {
            Debug.Log($"Maximum complaint exchanges reached ({maxComplaintExchanges}). Moving to next customer.");
            OnCustomerInteractionComplete();
            yield break;
        }
        
        Debug.Log($"🔄 [Exchange {complaintExchangeCount}] Listening for NPC's reaction to player...");

        // 3. SECURE THE STATE: Reset these BEFORE starting the listener
        isWaitingForNPCResponse = true;
        capturedComplaint = "";
        
        // 4. Start waiting for the response triggered by the Player's last choice.
        // We don't need to send "Whats your next issue" because the NPC already knows to continue.
        yield return StartCoroutine(WaitForNPCResponse());
    }

    void Update()
    {
        if (playerChat != null && welcomeCanvas.activeInHierarchy)
        {
            playerChat.gameObject.SetActive(false);
        }
        else
        {
            playerChat.gameObject.SetActive(true);
        }
    }

    private void OnCustomerInteractionComplete()
    {
        // This method is called when the current customer interaction is complete
        Debug.Log($"=== OnCustomerInteractionComplete called ===");
        Debug.Log($"Customer {currentCustomerIndex + 1}/{customerNPCs.Length} interaction complete");

        // Update the served count and UI display through RushSession
        if (rushSession != null)
        {
            rushSession.IncrementServedCount();
        }
        else
        {
            Debug.LogWarning("RushSession reference not found in ConvaiCustomerServiceIntegration!");
        }

        // Start the transition to next customer (NPC deactivation will happen after fade-in)
        StartCoroutine(DelayedNextCustomer());
    }
    
    /// <summary>
    /// Positions the current NPC at the designated anchor point
    /// </summary>
    private void PositionCurrentNPC()
    {
        if (currentCustomerNPC == null)
        {
            Debug.LogWarning("Cannot position NPC: currentCustomerNPC is null");
            return;
        }

        // Use anchor point if available, otherwise use hardcoded fallback
        if (npcAnchorPoint != null)
        {
            Vector3 targetPosition = npcAnchorPoint.position + npcPositionOffset;
            currentCustomerNPC.transform.position = targetPosition;
            Debug.Log($"Positioned {currentCustomerNPC.characterName} at anchor point: {targetPosition}");
        }
        else
        {
            // Fallback to hardcoded position if no anchor point is set
            Vector3 fallbackPosition = new Vector3(-1299.043f, -1061.185f, -7.879f);
            currentCustomerNPC.transform.position = fallbackPosition;
            Debug.LogWarning($"No anchor point set! Using fallback position: {fallbackPosition}");
        }

        // Set rotation
        Quaternion targetRotation = Quaternion.Euler(npcRotationEuler);
        currentCustomerNPC.transform.rotation = targetRotation;
        Debug.Log($"Set {currentCustomerNPC.characterName} rotation to: {npcRotationEuler}");
    }

    private IEnumerator DelayedNextCustomer()
    {
        // FIRST: End metrics tracking for current customer
        if (Metrics != null)
        {
            Metrics.EndCustomerInteraction();
            Debug.Log("Ended metrics tracking for current customer interaction");
        }

        // ABRUPT CHANGE: Removed the WaitUntil NPC finishes talking here 
        // to allow the screen to fade to black immediately.

        // Ensure transition screen is active but invisible
        if (transitionScreen != null)
        {
            transitionScreen.SetActive(true);
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 0f;
            }
        }

        // SECOND: Fade in to black smoothly (this is when user sees the transition start)
        yield return StartCoroutine(FadeTransition(0f, 1f));
        Debug.Log("Transition screen faded in");

        // THIRD: NOW properly terminate current NPC session and deactivate (during the black screen)
        if (currentCustomerNPC != null )
        {
            Debug.Log($"Properly terminating session for {currentCustomerNPC.characterName}");
            
            yield return StartCoroutine(SafelyTerminateNPC(currentCustomerNPC));
        }

        // Wait for transition duration (minus fade times)
        float waitTime = Mathf.Max(0.1f, delayBetweenCustomers - (fadeDuration * 2));
        yield return new WaitForSeconds(waitTime);

        // Check if we have more customers
        int nextCustomerIndex = currentCustomerIndex + 1;
        Debug.Log($"Current customer index: {currentCustomerIndex}, Next index: {nextCustomerIndex}, Total NPCs: {customerNPCs.Length}");
        
        if (nextCustomerIndex < customerNPCs.Length)
        {
            Debug.Log($"Moving to next customer: {nextCustomerIndex}/{customerNPCs.Length - 1}");
            
            // FOURTH: Start next customer interaction (during black screen)
            Debug.Log($"Starting interaction with next customer (index {nextCustomerIndex})");
            StartCustomerInteraction(nextCustomerIndex);
            
            // FIFTH: Fade out from black smoothly to reveal new customer
            yield return StartCoroutine(FadeTransition(1f, 0f));
            Debug.Log("Transition screen faded out");

            // Hide transition screen
            if (transitionScreen != null)
            {
                transitionScreen.SetActive(false);
            }
        }
        else
        {
            // All customers served - fade out and notify rush session
            yield return StartCoroutine(FadeTransition(1f, 0f));
            Debug.Log($"All customers have been served! Final customer was index {currentCustomerIndex} out of {customerNPCs.Length} total customers.");

            if (transitionScreen != null)
            {
                transitionScreen.SetActive(false);
            }

            // Notify rush session that all customers are complete
            if (rushSession != null)
            {
                rushSession.OnCustomerComplete();
            }
        }
    }

    /// <summary>
    /// Smoothly fades the transition screen using CanvasGroup
    /// </summary>
    /// <param name="startAlpha">Starting alpha value (0 = transparent, 1 = opaque)</param>
    /// <param name="endAlpha">Ending alpha value (0 = transparent, 1 = opaque)</param>
    private IEnumerator FadeTransition(float startAlpha, float endAlpha)
    {
        if (transitionCanvasGroup == null)
        {
            Debug.LogWarning("TransitionCanvasGroup not assigned! Using instant transition.");
            yield break;
        }

        float elapsed = 0f;
        transitionCanvasGroup.alpha = startAlpha;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            
            // Use smooth easing curve
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            
            yield return null;
        }
        
        // Ensure we end at exactly the target alpha
        transitionCanvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// Safely terminates an NPC session to prevent GRPC errors
    /// </summary>
    private IEnumerator SafelyTerminateNPC(ConvaiNPC npc)
    {
        if (npc == null) yield break;

        Debug.Log($"🚨 [Abrupt Termination] Stopping {npc.characterName} immediately");
        
        isWaitingForNPCResponse = false;
        capturedComplaint = "";

        // NEW: Stop audio and reset talking state BEFORE killing coroutines
        if (npc.AudioManager != null)
        {
            npc.AudioManager.StopAllAudioPlayback();
            npc.AudioManager.ClearResponseAudioQueue();
        }

        // STEP 1: Unsubscribe
        if (npc.AudioManager != null)
        {
            npc.AudioManager.OnAudioTranscriptAvailable -= HandleAITranscript;
            Debug.Log($"Unsubscribed from audio events for {npc.characterName}");
        }
        
        // STEP 2: Shut down the Convai session (closes GRPC)
        npc.isCharacterActive = false;
        
        // STEP 3: Force stop all internal NPC coroutines (lipsync, audio playback, etc.)
        npc.StopAllCoroutines();
        
        // STEP 4: Deactivate the GameObject immediately
        npc.gameObject.SetActive(false);
        
        // STEP 5: Very brief pause for thread cleanup instead of long waits
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"✅ Safe termination completed for {npc.characterName}");
    }

    /// <summary>
    /// Ensures all NPCs are in a clean state before starting a new interaction
    /// </summary>
    private IEnumerator EnsureCleanNPCState()
    {
        // Reset interaction variables immediately for the new interaction pool
        isWaitingForNPCResponse = false;
        capturedComplaint = "";
        complaintExchangeCount = 0;

        // Surgical cleanup: Only target the NPC that was previously active (at currentCustomerIndex - 1)
        if (currentCustomerIndex > 0)
        {
            ConvaiNPC previousNPC = customerNPCs[currentCustomerIndex - 1];
            
            if (previousNPC != null && previousNPC != currentCustomerNPC)
            {
                // FORCE RESET: Even if it's already deactivated, tell its audio manager
                // to signal the Group Controller that it is definitely NOT talking.
                if (previousNPC.AudioManager != null)
                {
                    previousNPC.AudioManager.StopAllAudioPlayback();
                }

                if (previousNPC.isCharacterActive || previousNPC.gameObject.activeSelf)
                {
                    Debug.Log($"🧹 [Cleanup] Surgically stopping previous NPC: {previousNPC.characterName}");
                    yield return StartCoroutine(SafelyTerminateNPC(previousNPC));
                }
            }
        }
        else
        {
            Debug.Log("🧹 [Cleanup] First interaction of session - skipping surgical cleanup.");
        }

        // Still unsubscribe from all events as a safety bridge
        UnsubscribeFromNPCAudio();
        
        // Very brief pause for internal Convai cleanup
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// Public method for RushSession to trigger customer interactions
    /// </summary>
    public void TriggerCustomerComplaint(int customerIndex)
    {
        StartCustomerInteraction(customerIndex);
    }

    /// <summary>
    /// Stops current NPC interaction
    /// </summary>
    public void StopCurrentInteraction()
    {
        isWaitingForNPCResponse = false;
        StopAllCoroutines();
        
        // Deactivate all NPCs
        if (customerNPCs != null)
        {
            foreach (var npc in customerNPCs)
            {
                if (npc != null)
                {
                    npc.isCharacterActive = false;
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        UnsubscribeFromNPCAudio();
    }

    private void OnDisable()
    {
        // Clean up subscriptions when disabled
        UnsubscribeFromNPCAudio();
    }

    /// <summary>
    /// Determines complaint type based on NPC character name or index
    /// </summary>
    private string GetComplaintTypeFromNPC(ConvaiNPC npc)
    {
        if (npc == null) return "general";
        
        // You can customize this based on your NPC setup
        string characterName = npc.characterName?.ToLower() ?? "";
        
        if (characterName.Contains("order") || characterName.Contains("mobile"))
            return "order_delay";
        else if (characterName.Contains("wait") || characterName.Contains("slow"))
            return "wait_time";
        else if (characterName.Contains("wrong") || characterName.Contains("mistake"))
            return "order_mistake";
        else if (characterName.Contains("cold") || characterName.Contains("temperature"))
            return "drink_quality";
        else if (characterName.Contains("milk") || characterName.Contains("allergy"))
            return "dietary_request";
        else
            return "general";
    }

    /// <summary>
    /// Gets the total number of available customer NPCs
    /// </summary>
    public int GetCustomerCount()
    {
        return customerNPCs?.Length ?? 0;
    }
    
    /// <summary>
    /// Coroutine to complete customer interaction after a delay
    /// </summary>
    private System.Collections.IEnumerator CompleteAfterDelay(float delay)
    {
        Debug.Log($"⏰ [Complete Delay] Waiting {delay} seconds before completing...");
        yield return new WaitForSeconds(delay);
        Debug.Log("⏰ [Complete Delay] Delay complete, finishing customer interaction");
        OnCustomerInteractionComplete();
    }
    
    /// <summary>
    /// Sends text to NPC with rate limiting to prevent API quota exceeded errors
    /// </summary>
    private IEnumerator SendTextToNPCWithRateLimit(ConvaiNPC npc, string text, string context = "")
    {
        if (npc == null)
        {
            Debug.LogWarning($"[API Rate Limit] Cannot send text - NPC is null. Context: {context}");
            yield break;
        }
        
        // Calculate time since last API call
        float timeSinceLastCall = Time.time - lastApiCallTime;
        
        // If we need to wait, do so
        if (timeSinceLastCall < apiCallDelay)
        {
            float waitTime = apiCallDelay - timeSinceLastCall;
            Debug.Log($"⏸️ [API Rate Limit] Waiting {waitTime:F1}s before sending: '{text}' (Context: {context})");
            yield return new WaitForSeconds(waitTime);
        }
        
        // Send the text
        Debug.Log($"📤 [API Call] Sending to {npc.characterName}: '{text}' (Context: {context})");
        npc.SendTextDataAsync(text);
        lastApiCallTime = Time.time;
        
        Debug.Log($"✅ [API Call] Sent successfully at {Time.time:F1}s");
    }
}
