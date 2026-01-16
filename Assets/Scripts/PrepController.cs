using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PrepController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text briefingText;
    public TMP_Text timerText;
    public TMP_Text loadoutText;
    public TMP_Text resultText;

    [Header("Tool Buttons (set in Inspector)")]
    public Button emfButton;
    public Button spiritBoxButton;
    public Button saltButton;
    public Button thermalButton;
    public Button uvButton;
    public Button ironButton;

    [Header("Audio")]
    public AudioSource ambientSource;
    public AudioSource sfxSource;
    public AudioClip clickSfx;
    public AudioClip warningSfx;
    public AudioClip lockInSfx;

    [Header("Prototype Settings")]
    public int maxTools = 3;
    public float prepTimeSeconds = 45f;

    [Header("Debug")]
    public Button debugSkipTimerButton;
    public Button debugResetSceneButton;

    private float timeLeft;
    private bool locked;
    private readonly List<string> selected = new();

    void Start()
    {
        timeLeft = prepTimeSeconds;
        locked = false;
        resultText.text = "";

        briefingText.text =
            "HOTEL: The Marrowlight\n" +
            "FLOOR: 5 is reported 'wrong'.\n" +
            "ENTITY: A woman seen entering the elevator.\n" +
            "RULE: When the doors close, the dimension seals.\n\n" +
            "Select 3 tools before departure.";

        HookupButtons();
        UpdateLoadoutUI();
        UpdateTimerUI();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug_ResetScene();
            return;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitApplication();
            return;
        }

        if (locked) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            LockIn();
        }
        else
        {
            // Simple audio escalation: raise volume as time runs out
            if (ambientSource != null && prepTimeSeconds > 0.1f)
            {
                float t = 1f - (timeLeft / prepTimeSeconds); // 0 -> 1
                ambientSource.volume = Mathf.Lerp(0.25f, 0.9f, t);
                ambientSource.pitch = Mathf.Lerp(1.0f, 1.1f, t);
            }

            // Warning sting at 10 seconds
            if (Mathf.Abs(timeLeft - 10f) < 0.02f)
                PlayOneShot(warningSfx);
        }

        UpdateTimerUI();
    }

    void HookupButtons()
    {
        emfButton.onClick.AddListener(() => ToggleTool("EMF Reader"));
        spiritBoxButton.onClick.AddListener(() => ToggleTool("Spirit Box"));
        saltButton.onClick.AddListener(() => ToggleTool("Salt Pouch"));
        thermalButton.onClick.AddListener(() => ToggleTool("Thermal Camera"));
        uvButton.onClick.AddListener(() => ToggleTool("UV Light"));
        ironButton.onClick.AddListener(() => ToggleTool("Crucifix"));
        debugSkipTimerButton.onClick.AddListener(() => Debug_SkipTimer());
        debugResetSceneButton.onClick.AddListener(() => Debug_ResetScene());
    }

    void ToggleTool(string toolName)
    {
        if (locked) return;

        if (selected.Contains(toolName))
        {
            selected.Remove(toolName);
            PlayOneShot(clickSfx);
        }
        else
        {
            if (selected.Count >= maxTools)
            {
                // soft fail feedback: warning sound if full
                PlayOneShot(warningSfx);
                return;
            }

            selected.Add(toolName);
            PlayOneShot(clickSfx);
        }

        UpdateLoadoutUI();
    }

    void LockIn()
    {
        locked = true;
        PlayOneShot(lockInSfx);

        // Disable buttons
        emfButton.interactable = false;
        spiritBoxButton.interactable = false;
        saltButton.interactable = false;
        thermalButton.interactable = false;
        uvButton.interactable = false;
        ironButton.interactable = false;

        // Simple "risk forecast" result text
        bool hasProtection = selected.Contains("Salt Pouch") || selected.Contains("Crucifix");
        bool hasSpiritBox = selected.Contains("Spirit Box");
        int evidenceTools = 0;
        if (selected.Contains("EMF Reader")) evidenceTools++;
        if (selected.Contains("Thermal Camera")) evidenceTools++;
        if (selected.Contains("UV Light")) evidenceTools++;

        string risk = "MODERATE";
        if (!hasProtection) risk = "HIGH";
        if (hasSpiritBox && !hasProtection) risk = "VERY HIGH";

        string payout = evidenceTools >= 2 ? "HIGH" : (evidenceTools == 1 ? "MEDIUM" : "LOW");

        resultText.text =
            "ELEVATOR DOORS CLOSING...\n\n" +
            "Loadout Locked:\n- " + string.Join("\n- ", selected) + "\n\n" +
            "Risk Forecast: " + risk + "\n" +
            "Payout Potential: " + payout + "\n\n" +
            "Press R to retry.";
    }

    void UpdateLoadoutUI()
    {
        // Displays 3 slots
        string[] slots = new string[maxTools];
        for (int i = 0; i < maxTools; i++)
            slots[i] = i < selected.Count ? selected[i] : "[empty]";

        loadoutText.text = $"Loadout ({selected.Count}/{maxTools}):\n- {string.Join("\n- ", slots)}";
    }

    void UpdateTimerUI()
    {
        int sec = Mathf.CeilToInt(timeLeft);
        timerText.text = $"Elevator departs in: 00:{sec:00}";
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }


    public void Debug_SkipTimer()
    {
        if (locked) return;
        timeLeft = 0f;
        UpdateTimerUI();
        LockIn();
    }

    public void Debug_ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitApplication()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}
