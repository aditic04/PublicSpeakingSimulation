using UnityEngine;
using TMPro;

public class SessionManager : MonoBehaviour
{
    [Header("Audience")]
    public AudienceManager audienceManager;

    [Header("UI")]
    public GameObject resultPanel;

    [Header("Timer")]
    public TMP_Text timerText;

    private bool sessionRunning = false;
    private float timer = 0f;

    void Start()
    {
        // Result panel hidden initially
        resultPanel.SetActive(false);

        // Audience starts idle
        audienceManager.StopClapping();
    }

    void Update()
    {
        if (sessionRunning)
        {
            timer += Time.deltaTime;

            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);

            timerText.text =
                minutes.ToString("00") + ":" +
                seconds.ToString("00");
        }
    }

    // START SESSION BUTTON
    public void StartSession()
    {
        sessionRunning = true;

        timer = 0f;

        resultPanel.SetActive(false);

        audienceManager.StopClapping();

        Debug.Log("Session Started");
    }

    // END SESSION BUTTON
    public void EndSession()
    {
        sessionRunning = false;

        resultPanel.SetActive(true);

        audienceManager.StartClapping();

        Debug.Log("Session Ended");
    }
}