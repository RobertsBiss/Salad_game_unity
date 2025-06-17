using UnityEngine;

[CreateAssetMenu(menuName = "Shop/ShopInventory")]
public class ShopInventory : ScriptableObject
{
    public ShopItemEntry[] items;
}

[System.Serializable]
public class ShopItemEntry
{
    public Item item;      // Reference to your Item ScriptableObject
    public int price;
    public int quantity;
}