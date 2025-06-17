using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text quantityText;
    public Button buyButton;

    private ShopItemEntry entry;
    private MoneyManager moneyManager;
    private InventoryManager inventoryManager;

    void Start()
    {
        moneyManager = FindObjectOfType<MoneyManager>();
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    public void Setup(ShopItemEntry entry)
    {
        this.entry = entry;
        if (icon != null) icon.sprite = entry.item.image;
        if (nameText != null) nameText.text = entry.item.name;
        if (priceText != null) priceText.text = entry.price + "$";
        if (quantityText != null) quantityText.text = "x" + entry.quantity;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(Buy);
            buyButton.interactable = entry.quantity > 0;
        }
    }

    void Buy()
    {
        if (moneyManager == null) moneyManager = FindObjectOfType<MoneyManager>();
        if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();
        if (entry.quantity <= 0) return;
        if (moneyManager.playerMoney < entry.price) return;

        // Subtract money
        moneyManager.SubtractMoney(entry.price);
        // Decrease quantity
        entry.quantity--;
        if (quantityText != null) quantityText.text = "x" + entry.quantity;

        // Try to fetch hand scale/offsets from an existing ItemPickup in the scene
        ItemPickup[] pickups = GameObject.FindObjectsOfType<ItemPickup>(true); // include inactive
        ItemPickup foundPickup = null;
        foreach (var pickup in pickups)
        {
            if (pickup.item == entry.item)
            {
                foundPickup = pickup;
                break;
            }
        }
        if (foundPickup != null)
        {
            ItemScaleData scaleData = new ItemScaleData
            {
                worldScale = foundPickup.transform.localScale,
                handScale = foundPickup.HandScale,
                handPositionOffset = foundPickup.HandPositionOffset,
                handRotationOffset = foundPickup.HandRotationOffset
            };
            inventoryManager.AddItemWithScales(entry.item, scaleData);
        }
        else
        {
            inventoryManager.AddItem(entry.item);
        }

        // Disable buy button if out of stock
        if (entry.quantity <= 0 && buyButton != null)
            buyButton.interactable = false;
    }
}