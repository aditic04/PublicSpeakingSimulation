using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Windows.Speech;

public class SpeechAnalyzer : MonoBehaviour
{
    // =========================================
    // SPEECH RECOGNITION
    // =========================================

    private DictationRecognizer dictationRecognizer;
    private AudioClip micClip;

    private float currentLoudness = 0f;

    // =====================================
    // NOISE FILTERING
    // =====================================

    private float smoothedLoudness = 0f;

    // Ignore tiny background sounds
    private float noiseThreshold = 0.008f;

    // Smoothing speed
    private float smoothingSpeed = 5f;

    // Speech gate timer
    private float voiceActiveTimer = 0f;

    // Minimum time required to classify speech
    private float speechGateTime = 0.15f;

    private string voiceStrength = "Silent";

    // =====================================
    // VOICE DURATION TRACKING
    // =====================================

    private float strongVoiceTime = 0f;

    private float normalVoiceTime = 0f;

    private float weakVoiceTime = 0f;

    private float silentVoiceTime = 0f;

    private const int sampleDataLength = 1024;

    private float[] clipSampleData =
        new float[sampleDataLength];
    // =========================================
    // RESULT PANEL UI
    // =========================================

    [Header("Result Panel")]

    public TMP_Text scoreText;
    public TMP_Text confidenceText;
    public TMP_Text clarityText;
    public TMP_Text fillerText;
    public TMP_Text wpmText;
    public TMP_Text feedbackText;

    // NEW METRICS
    public TMP_Text pauseText;
    public TMP_Text durationText;
    public TMP_Text voiceScoreText;

    public GameObject resultPanel;


    [Header("Live Analytics UI")]

    public GameObject liveAnalysisPanel;
    public TMP_Text liveConfidenceText;
    public TMP_Text liveWPMText;
    public TMP_Text liveFillerText;
    public TMP_Text liveStatusText;
    public TMP_Text voiceActivityText;
    public TMP_Text liveFeedbackText;
    // =========================================
    // SPEECH DATA
    // =========================================

    private string fullTranscript = "";

    private int totalWords = 0;
    private int fillerCount = 0;

    private float sessionStartTime;
    private float sessionEndTime;

    private bool sessionEnded = false;

    // =========================================
    // ADVANCED METRICS
    // =========================================

    private float lastSpeechTime = 0f;

    private int longPauseCount = 0;

    private float totalSpeakingTime = 0f;

    // =========================================
    // FILLER WORDS
    // =========================================

    private List<string> fillerWords = new List<string>()
    {
        "um",
        "uh",
        "like",
        "basically",
        "actually",
        "you know",
        "okay",
        "so"
    };

    // =========================================
    // START
    // =========================================

    void Start()
    {
        // Hide result panel initially
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        sessionStartTime = Time.time;

        // Delay before starting speech
        Invoke("StartSpeechRecognition", 2f);

    }

    void Update()
    {
        if (!sessionEnded)
        {
            AnalyzeMicrophoneVolume();

            TrackVoiceDurations();

            UpdateLiveAnalytics();
        }
    }
    // =========================================
    // START SPEECH RECOGNITION
    // =========================================

    public void StartSpeechRecognition()
    {
        // Start microphone
        micClip = Microphone.Start(null, true, 10, 44100);

        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.InitialSilenceTimeoutSeconds = 20f;
        dictationRecognizer.AutoSilenceTimeoutSeconds = 20f;

        // EVENTS
        dictationRecognizer.DictationResult += OnDictationResult;

        dictationRecognizer.DictationHypothesis += (text) =>
        {
            Debug.Log("HYPOTHESIS: " + text);
        };

        dictationRecognizer.DictationComplete += (cause) =>
        {
            Debug.Log("COMPLETED: " + cause);
        };

        dictationRecognizer.DictationError += (error, hresult) =>
        {
            Debug.LogError("DICTATION ERROR: " + error);
        };

        dictationRecognizer.Start();

        Debug.Log("Speech Recognition Started");
    }

    // =========================================
    // SPEECH DETECTED
    // =========================================

    void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        // Prevent updates after ending
        if (sessionEnded)
            return;

        Debug.Log("RECOGNIZED TEXT: " + text);

        // Store transcript internally
        fullTranscript += " " + text;

        // Analyze speech
        AnalyzeSpeech(text);
    }

    // =========================================
    // ANALYZE SPEECH
    // =========================================

    void AnalyzeSpeech(string text)
    {
        // =====================================
        // PAUSE DETECTION
        // =====================================

        float currentTime = Time.time;

        if (lastSpeechTime > 0)
        {
            float pauseDuration =
                currentTime - lastSpeechTime;

            // Long pause detected
            if (pauseDuration > 3f)
            {
                longPauseCount++;

                Debug.Log(
                    "Long Pause Detected: " +
                    pauseDuration + " seconds"
                );
            }
        }

        lastSpeechTime = currentTime;

        // =====================================
        // WORD ANALYSIS
        // =====================================

        string[] words = text.Split(' ');

        totalWords += words.Length;

        totalSpeakingTime += words.Length * 0.4f;

        foreach (string word in words)
        {
            string cleanWord = word
                .ToLower()
                .Trim(',', '.', '?', '!', ';', ':');

            if (fillerWords.Contains(cleanWord))
            {
                fillerCount++;
            }
        }
    }

    void UpdateLiveAnalytics()
    {
        float durationMinutes =
            (Time.time - sessionStartTime) / 60f;

        if (durationMinutes <= 0)
            return;

        // =====================================
        // LIVE WPM
        // =====================================

        int currentWPM =
            Mathf.RoundToInt(
                totalWords / durationMinutes
            );

        // =====================================
        // LIVE CONFIDENCE
        // =====================================

        int liveConfidence =
            CalculateConfidenceScore(
                currentWPM,
                fillerCount,
                totalWords,
                durationMinutes,
                longPauseCount
            );
        

        // =====================================
        // UPDATE CONFIDENCE
        // =====================================

        if (liveConfidenceText != null)
        {
            liveConfidenceText.text =
                "Confidence: " +
                liveConfidence + "%";
        }

        // =====================================
        // UPDATE WPM
        // =====================================

        if (liveWPMText != null)
        {
            liveWPMText.text =
                "Speaking Speed: " +
                currentWPM + " WPM";
        }

        // =====================================
        // UPDATE FILLERS
        // =====================================

        if (liveFillerText != null)
        {
            liveFillerText.text =
                "Fillers: " +
                fillerCount;
        }

        // =====================================
        // STATUS SYSTEM
        // =====================================

        if (liveStatusText != null)
        {
            if (liveConfidence >= 85)
            {
                liveStatusText.text =
                    "Status: Excellent";
            }
            else if (liveConfidence >= 70)
            {
                liveStatusText.text =
                    "Status: Good";
            }
            else if (liveConfidence >= 50)
            {
                liveStatusText.text =
                    "Status: Improving";
            }
            else
            {
                liveStatusText.text =
                    "Status: Nervous";
            }
        }
        if (voiceActivityText != null)
        {
            voiceActivityText.text =
                "Voice Level: " +
                voiceStrength;
        }
        // =====================================
        // LIVE FEEDBACK
        // =====================================

        if (liveFeedbackText != null)
        {
            liveFeedbackText.text =
                "Feedback: " +
                GenerateLiveFeedback(
                    currentWPM,
                    liveConfidence
                );
        }
    }

    string GetPaceState(int wpm)
    {
        if (wpm < 70)
            return "VERY_SLOW";

        if (wpm < 95)
            return "SLOW";

        if (wpm <= 140)
            return "IDEAL";

        if (wpm <= 165)
            return "SLIGHTLY_FAST";

        return "TOO_FAST";
    }

    string GetFluencyState()
    {
        float fillerRatio = 0f;

        if (totalWords > 0)
        {
            fillerRatio =
                (float)fillerCount /
                totalWords;
        }

        // MANY FILLERS
        if (fillerRatio > 0.08f)
        {
            return "POOR";
        }

        // SOME FILLERS
        if (fillerRatio > 0.04f)
        {
            return "AVERAGE";
        }

        return "GOOD";
    }

    string GenerateLiveFeedback(
    int currentWPM,
    int confidence)
    {
        // =====================================
        // NO SPEECH DETECTED
        // =====================================

        if (totalWords <= 0)
        {
            return
                "Please speak to provide feedback";
        }
        string paceState =
            GetPaceState(currentWPM);

        string fluencyState =
            GetFluencyState();

        // =====================================
        // PRIORITY 1 — SILENCE
        // =====================================

        if (voiceStrength == "SILENT")
        {
            return
                "Try maintaining speaking continuity";
        }

        // =====================================
        // PRIORITY 2 — WEAK VOICE
        // =====================================

        if (voiceStrength == "WEAK")
        {
            if (paceState == "TOO_FAST")
            {
                return
                    "You are rushing and speaking too softly";
            }

            return
                "Increase vocal projection";
        }

        // =====================================
        // PRIORITY 3 — VERY FAST
        // =====================================

        if (paceState == "TOO_FAST")
        {
            return
                "Slow down and articulate more clearly";
        }

        // =====================================
        // PRIORITY 4 — SLIGHTLY FAST
        // =====================================

        if (paceState == "SLIGHTLY_FAST")
        {
            return
                "Pace is slightly fast";
        }

        // =====================================
        // PRIORITY 5 — VERY SLOW
        // =====================================

        if (paceState == "VERY_SLOW")
        {
            return
                "Try speaking more confidently";
        }

        // =====================================
        // PRIORITY 6 — SLOW
        // =====================================

        if (paceState == "SLOW")
        {
            return
                "Maintain stronger speaking flow";
        }

        // =====================================
        // PRIORITY 7 — FLUENCY
        // =====================================

        if (fluencyState == "POOR")
        {
            return
                "Reduce filler words and hesitation";
        }

        if (fluencyState == "AVERAGE")
        {
            return
                "Try improving fluency";
        }

        // =====================================
        // PRIORITY 8 — EXCELLENT DELIVERY
        // =====================================

        if (confidence >= 85 &&
           voiceStrength == "STRONG")
        {
            return
                "Excellent vocal delivery";
        }

        // =====================================
        // PRIORITY 9 — GOOD DELIVERY
        // =====================================

        if (confidence >= 70)
        {
            return
                "Good speaking rhythm";
        }

        // =====================================
        // DEFAULT
        // =====================================

        return
            "Maintain steady delivery";
    }

    void AnalyzeMicrophoneVolume()
    {
        // Safety check
        if (micClip == null)
            return;

        int micPosition =
            Microphone.GetPosition(null) -
            sampleDataLength + 1;

        if (micPosition < 0)
            return;

        // Get microphone data
        micClip.GetData(
            clipSampleData,
            micPosition
        );

        // Calculate RMS loudness
        float levelMax = 0;

        for (int i = 0; i < sampleDataLength; i++)
        {
            float wavePeak =
                clipSampleData[i] *
                clipSampleData[i];

            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }

        // RAW loudness
        currentLoudness =
            Mathf.Sqrt(levelMax);

        // =====================================
        // NOISE FLOOR FILTER
        // =====================================

        if (currentLoudness < noiseThreshold)
        {
            currentLoudness = 0f;
        }

        // =====================================
        // SMOOTHING FILTER
        // =====================================

        smoothedLoudness =
            Mathf.Lerp(
                smoothedLoudness,
                currentLoudness,
                Time.deltaTime * smoothingSpeed
            );

        // =====================================
        // VOICE STRENGTH LEVELS
        // =====================================

        // =====================================
        // SPEECH GATE
        // =====================================

        if (smoothedLoudness > noiseThreshold)
        {
            voiceActiveTimer += Time.deltaTime;
        }
        else
        {
            voiceActiveTimer = 0f;
        }

        // =====================================
        // STABLE VOICE DETECTION
        // =====================================

        if (voiceActiveTimer < speechGateTime)
        {
            voiceStrength = "SILENT";
        }
        else if (smoothedLoudness < 0.02f)
        {
            voiceStrength = "WEAK";
        }
        else if (smoothedLoudness < 0.05f)
        {
            voiceStrength = "NORMAL";
        }
        else
        {
            voiceStrength = "STRONG";
        }
    }

    void TrackVoiceDurations()
    {
        if (sessionEnded)
            return;

        switch (voiceStrength)
        {
            case "STRONG":
                strongVoiceTime += Time.deltaTime;
                break;

            case "NORMAL":
                normalVoiceTime += Time.deltaTime;
                break;

            case "WEAK":
                weakVoiceTime += Time.deltaTime;
                break;

            case "SILENT":
                silentVoiceTime += Time.deltaTime;
                break;
        }
    }


    // =========================================
    // END SESSION
    // =========================================

    public void EndSession()
    {

        Debug.Log("=== SPEECH ANALYZER END SESSION RUNNING ===");
        // Prevent multiple calls
        if (sessionEnded)
            return;

        sessionEnded = true;

        sessionEndTime = Time.time;

        Debug.Log("ENDING SESSION");

        // =========================================
        // STOP DICTATION
        // =========================================

        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult -=
                OnDictationResult;

            if (dictationRecognizer.Status ==
               SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }

            dictationRecognizer.Dispose();

            dictationRecognizer = null;
        }

        // =========================================
        // STOP MICROPHONE
        // =========================================

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }

        micClip = null;

        Debug.Log("MICROPHONE FULLY STOPPED");

        // =========================================
        // CALCULATE RESULTS
        // =========================================

        CalculateResults();


        // =========================================
        // HIDE LIVE PANEL
        // =========================================

        if (liveAnalysisPanel != null)
        {
            Debug.Log("HIDING LIVE PANEL");

            liveAnalysisPanel.SetActive(false);
        }
        else
        {
            Debug.LogError(
                "LIVE ANALYSIS PANEL NOT ASSIGNED"
            );
        }

        // =========================================
        // SHOW RESULT PANEL
        // =========================================

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        Debug.Log("SESSION COMPLETED");
    }

    // =========================================
    // CALCULATE RESULTS
    // =========================================

    void CalculateResults()
    {
        Debug.Log("=== CALCULATE RESULTS RUNNING ===");
        float durationMinutes =
            (sessionEndTime - sessionStartTime) / 60f;

        if (durationMinutes <= 0)
        {
            durationMinutes = 1;
        }

        // =========================================
        // WPM
        // =========================================

        int wpm =
            Mathf.RoundToInt(totalWords / durationMinutes);

        // =========================================
        // ADVANCED CONFIDENCE SCORE
        // =========================================

        int confidenceScore =
            CalculateConfidenceScore(
                wpm,
                fillerCount,
                totalWords,
                durationMinutes,
                longPauseCount
            );
        int voiceScore =
    CalculateVoiceScore();

        // =========================================
        // CLARITY SCORE
        // =========================================

        int clarityScore =
            CalculateClarityScore(
                fillerCount,
                totalWords
            );

        // =========================================
        // OVERALL SCORE
        // =========================================

        int overallScore =
    Mathf.RoundToInt(
        (confidenceScore * 0.5f) +
        (clarityScore * 0.2f) +
        (voiceScore * 0.3f)
    );

        // =========================================
        // UPDATE RESULT PANEL
        // =========================================

        if (scoreText != null)
        {
            Debug.Log("UPDATING UI TEXTS");
            scoreText.text =
                "Final Score: " + overallScore;
        }

        if (confidenceText != null)
        {
            confidenceText.text =
                "Confidence: " + confidenceScore + "%";
        }

        if (clarityText != null)
        {
            if (clarityScore >= 80)
            {
                clarityText.text =
                    "Speech Clarity: Excellent";
            }
            else if (clarityScore >= 60)
            {
                clarityText.text =
                    "Speech Clarity: Good";
            }
            else
            {
                clarityText.text =
                    "Speech Clarity: Poor";
            }
        }

        if (fillerText != null)
        {
            fillerText.text =
                "Filler Words: " + fillerCount;
        }

        if (wpmText != null)
        {
            wpmText.text =
                "Speaking Speed: " + wpm + " WPM";
        }

        if (pauseText != null)
        {
            pauseText.text =
                "Long Pauses: " + longPauseCount;
        }

        if (durationText != null)
        {
            durationText.text =
                "Duration: " +
                durationMinutes.ToString("F1") +
                " mins";
        }

        if (voiceScoreText != null)
        {
            voiceScoreText.text =
                "Voice Confidence: " +
                voiceScore + "%";
        }


        if (feedbackText != null)
        {
            feedbackText.text =
                GenerateFeedback(
                    overallScore,
                    confidenceScore,
                    clarityScore,
                    fillerCount,
                    wpm
                );
        }

        Debug.Log("Results Calculated");
    }

    // =========================================
    // ADVANCED CONFIDENCE SCORE
    // =========================================

    int CalculateConfidenceScore(
        int wpm,
        int fillers,
        int totalWords,
        float durationMinutes,
        int longPauses)
    {
        float score = 100f;

        // =====================================
        // 1. SPEAKING PACE
        // =====================================

        if (wpm < 90)
        {
            score -= 25;
        }
        else if (wpm > 170)
        {
            score -= 20;
        }
        else if (wpm >= 110 && wpm <= 150)
        {
            score += 5;
        }

        // =====================================
        // 2. FILLER RATIO
        // =====================================

        float fillerRatio = 0f;

        if (totalWords > 0)
        {
            fillerRatio =
                (float)fillers / totalWords;
        }

        score -= fillerRatio * 100f;

        if (fillers > 10)
        {
            score -= 15;
        }

        // =====================================
        // 3. SESSION DURATION
        // =====================================

        if (durationMinutes < 1f)
        {
            score -= 20;
        }
        else if (durationMinutes < 2f)
        {
            score -= 10;
        }

        // =====================================
        // 4. LONG PAUSES
        // =====================================

        score -= longPauses * 5f;

        // =====================================
        // 5. SPEECH DENSITY
        // =====================================

        float wordsPerSecond =
            totalWords / (durationMinutes * 60f);

        if (wordsPerSecond < 1f)
        {
            score -= 10;
        }

        // =====================================
        // FINAL CLAMP
        // =====================================

        score = Mathf.Clamp(score, 0, 100);

        return Mathf.RoundToInt(score);
    }

    // =========================================
    // CLARITY SCORE
    // =========================================

    int CalculateClarityScore(
        int fillers,
        int words)
    {
        if (words <= 0)
            return 0;

        float fillerRatio =
            (float)fillers / words;

        int clarity =
            Mathf.RoundToInt(
                100 - (fillerRatio * 100)
            );

        return Mathf.Clamp(clarity, 0, 100);
    }

    int CalculateVoiceScore()
    {
        float totalVoiceTime =
            strongVoiceTime +
            normalVoiceTime +
            weakVoiceTime +
            silentVoiceTime;

        if (totalVoiceTime <= 0)
            return 0;

        // =====================================
        // VOICE QUALITY FORMULA
        // =====================================

        float strongRatio =
            strongVoiceTime / totalVoiceTime;

        float weakRatio =
            weakVoiceTime / totalVoiceTime;

        float silentRatio =
            silentVoiceTime / totalVoiceTime;

        float score = 100f;

        // Reward strong voice
        score += strongRatio * 20f;

        // Penalize weak voice
        score -= weakRatio * 30f;

        // Penalize silence heavily
        score -= silentRatio * 40f;

        score = Mathf.Clamp(score, 0, 100);

        return Mathf.RoundToInt(score);
    }

    // =========================================
    // FEEDBACK
    // =========================================

    string GenerateFeedback(
        int overall,
        int confidence,
        int clarity,
        int fillers,
        int wpm)
    {
        string feedback = "";

        // OVERALL
        if (overall >= 85)
        {
            feedback +=
                "Excellent public speaking performance.\n";
        }
        else if (overall >= 70)
        {
            feedback +=
                "Good presentation overall.\n";
        }
        else
        {
            feedback +=
                "Needs more speaking practice.\n";
        }

        // CONFIDENCE
        if (confidence < 60)
        {
            feedback +=
                "Try speaking more confidently.\n";
        }

        // CLARITY
        if (clarity < 70)
        {
            feedback +=
                "Reduce filler words for better clarity.\n";
        }

        // FILLERS
        if (fillers > 10)
        {
            feedback +=
                "Too many filler words detected.\n";
        }
        else
        {
            feedback +=
                "Good control over filler words.\n";
        }

        // WPM
        if (wpm < 90)
        {
            feedback +=
                "Speak slightly faster.\n";
        }
        else if (wpm > 170)
        {
            feedback +=
                "Slow down your speaking pace.\n";
        }
        else
        {
            feedback +=
                "Good speaking pace.\n";
        }

        if (voiceStrength == "WEAK")
        {
            feedback +=
                "Try speaking louder and with more projection.\n";
        }

        if (voiceStrength == "SILENT")
        {
            feedback +=
                "Increase vocal energy and engagement.\n";
        }

        return feedback;
    }

    // =========================================
    // CLEANUP
    // =========================================

    void OnDestroy()
    {
        if (dictationRecognizer != null)
        {
            if (dictationRecognizer.Status ==
               SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }

            dictationRecognizer.Dispose();
        }

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }
}