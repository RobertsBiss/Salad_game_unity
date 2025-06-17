using UnityEngine;

public class ShopUIManager : MonoBehaviour
{
    public GameObject shopItemPrefab; // Assign ShopItemContainer prefab
    public Transform shopContainer;   // Parent for items
    private ShopInventory currentInventory;

    public void OpenShop(ShopInventory inventory)
    {
        currentInventory = inventory;
        PopulateShop();
        gameObject.SetActive(true);
    }

    void PopulateShop()
    {
        // Clear previous items
        foreach (Transform child in shopContainer)
            Destroy(child.gameObject);

        // Add new items
        foreach (var entry in currentInventory.items)
        {
            var go = Instantiate(shopItemPrefab, shopContainer);
            var itemUI = go.GetComponent<ShopItemUI>();
            if (itemUI != null)
                itemUI.Setup(entry);
        }
    }
}