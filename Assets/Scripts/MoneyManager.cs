using UnityEngine;
using TMPro; // Import TextMeshPro namespace
using System.Collections;

public class MoneyManager : MonoBehaviour
{
    [Header("Money Settings")]
    public int playerMoney = 0; // Starting money

    [Header("UI References")]
    public TextMeshProUGUI moneyText; // Reference to the main balance TextMeshProUGUI component
    public TextMeshProUGUI moneyGainText; // Reference to the money gain display TextMeshProUGUI component

    [Header("Money Gain Display Settings")]
    [SerializeField] private float gainDisplayDuration = 2f; // How long to show the gain text
    [SerializeField] private bool useAnimation = true; // Whether to animate the gain text
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // Fade animation curve
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 20f, 0f); // How much to move the text during animation
    [SerializeField] private Color gainTextColor = Color.green; // Color for positive money gains
    [SerializeField] private Color lossTextColor = Color.red; // Color for money losses (if you want to show losses too)

    private Coroutine currentGainDisplayCoroutine;
    private Vector3 originalGainTextPosition;
    private Color originalGainTextColor;

    void Start()
    {
        // Store original properties of the gain text
        if (moneyGainText != null)
        {
            originalGainTextPosition = moneyGainText.rectTransform.anchoredPosition;
            originalGainTextColor = moneyGainText.color;

            // Hide the gain text initially
            moneyGainText.gameObject.SetActive(false);
        }

        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        playerMoney += amount;
        UpdateMoneyUI();

        // Show the money gain display
        if (amount > 0)
        {
            ShowMoneyGain(amount);
        }

        // Update earn money missions immediately when money changes
        UpdateEarnMoneyMissions();
    }

    public void SubtractMoney(int amount)
    {
        playerMoney -= amount;
        UpdateMoneyUI();

        // Optionally show money loss (negative gain)
        if (amount > 0)
        {
            ShowMoneyGain(-amount);
        }

        // Update earn money missions immediately when money changes
        UpdateEarnMoneyMissions();
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + playerMoney;
        }
    }

    void ShowMoneyGain(int amount)
    {
        if (moneyGainText == null) return;

        // Stop any existing gain display coroutine
        if (currentGainDisplayCoroutine != null)
        {
            StopCoroutine(currentGainDisplayCoroutine);
        }

        // Start the new gain display coroutine
        currentGainDisplayCoroutine = StartCoroutine(DisplayMoneyGainCoroutine(amount));
    }

    IEnumerator DisplayMoneyGainCoroutine(int amount)
    {
        // Setup the gain text
        moneyGainText.gameObject.SetActive(true);

        // Set the text content
        string prefix = amount > 0 ? "+" : "";
        moneyGainText.text = prefix + amount + "$";

        // Set the color based on gain or loss
        Color textColor = amount > 0 ? gainTextColor : lossTextColor;
        moneyGainText.color = textColor;

        // Reset position
        moneyGainText.rectTransform.anchoredPosition = originalGainTextPosition;

        if (useAnimation)
        {
            // Animate the gain text
            float elapsedTime = 0f;
            Vector3 startPosition = originalGainTextPosition;
            Vector3 endPosition = originalGainTextPosition + moveOffset;

            while (elapsedTime < gainDisplayDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / gainDisplayDuration;

                // Evaluate the fade curve
                float curveValue = fadeCurve.Evaluate(progress);

                // Update position (move upward)
                Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);
                moneyGainText.rectTransform.anchoredPosition = currentPosition;

                // Update alpha (fade out)
                Color currentColor = textColor;
                currentColor.a = curveValue;
                moneyGainText.color = currentColor;

                yield return null;
            }
        }
        else
        {
            // Simple display without animation
            yield return new WaitForSeconds(gainDisplayDuration);
        }

        // Hide the gain text
        moneyGainText.gameObject.SetActive(false);

        // Reset properties
        moneyGainText.color = originalGainTextColor;
        moneyGainText.rectTransform.anchoredPosition = originalGainTextPosition;

        currentGainDisplayCoroutine = null;
    }

    // Public method to manually trigger money gain display (useful for testing)
    [ContextMenu("Test Money Gain Display")]
    public void TestMoneyGainDisplay()
    {
        ShowMoneyGain(100);
    }

    // Method to set custom gain display duration
    public void SetGainDisplayDuration(float duration)
    {
        gainDisplayDuration = duration;
    }

    // Method to enable/disable animation
    public void SetUseAnimation(bool useAnim)
    {
        useAnimation = useAnim;
    }

    // Method to set custom colors
    public void SetGainTextColors(Color gainColor, Color lossColor)
    {
        gainTextColor = gainColor;
        lossTextColor = lossColor;
    }

    // Update earn money missions when money changes
    private void UpdateEarnMoneyMissions()
    {
        if (MissionManager.Instance == null) return;

        // Check if there are any earn money missions
        bool hasEarnMoneyMissions = false;
        foreach (var mission in MissionManager.Instance.allMissions)
        {
            if (mission != null && mission.missionName.ToLower().Contains("earn money"))
            {
                hasEarnMoneyMissions = true;
                break;
            }
        }

        // If there are earn money missions, update them immediately
        if (hasEarnMoneyMissions)
        {
            MissionManager.Instance.UpdateEarnMoneyMission();
            MissionManager.Instance.NotifyActiveMissionDisplay();
        }
    }
}