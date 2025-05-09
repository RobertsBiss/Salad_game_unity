using UnityEngine;
using TMPro; // Import TextMeshPro namespace

public class MoneyManager : MonoBehaviour
{
    public int playerMoney = 0; // Starting money
    public TextMeshProUGUI moneyText; // Reference to the TextMeshProUGUI component

    void Start()
    {
        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        playerMoney += amount;
        UpdateMoneyUI();
    }

    public void SubtractMoney(int amount)
    {
        playerMoney -= amount;
        UpdateMoneyUI();
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "Balance " + playerMoney + "$";
        }
    }
}