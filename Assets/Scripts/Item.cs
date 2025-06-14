using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Item")]
public class Item : ScriptableObject
{
    [Header("Only gameplay")]
    public ItemType type;
    public Vector2Int range = new Vector2Int(5, 4);

    [Header("Only UI")]
    public bool stackable = true;

    [Header("Both")]
    public Sprite image;

    [Header("3D World Representation")]
    public GameObject itemPrefab; // Prefab to show in player's hand

    [Header("Value Settings")]
    [SerializeField] private Vector2 valueRange = new Vector2(1f, 5f); // Min and max value for this item
    [SerializeField] private bool useRandomValue = true; // Whether to use random value or fixed value
    [SerializeField] private float fixedValue = 1f; // Fixed value if not using random

    /// <summary>
    /// Gets a random value within the item's value range, or returns the fixed value
    /// </summary>
    public float GetValue()
    {
        if (useRandomValue)
        {
            return Random.Range(valueRange.x, valueRange.y + 1f); // +1 to include max value for integers
        }
        else
        {
            return fixedValue;
        }
    }

    /// <summary>
    /// Gets the minimum possible value for this item
    /// </summary>
    public float GetMinValue()
    {
        return useRandomValue ? valueRange.x : fixedValue;
    }

    /// <summary>
    /// Gets the maximum possible value for this item
    /// </summary>
    public float GetMaxValue()
    {
        return useRandomValue ? valueRange.y : fixedValue;
    }

    /// <summary>
    /// Gets the value range as a string for display purposes
    /// </summary>
    public string GetValueRangeString()
    {
        if (useRandomValue)
        {
            if (Mathf.Approximately(valueRange.x, valueRange.y))
            {
                return $"${valueRange.x:F0}";
            }
            else
            {
                return $"${valueRange.x:F0}-${valueRange.y:F0}";
            }
        }
        else
        {
            return $"${fixedValue:F0}";
        }
    }

    /// <summary>
    /// Sets the value range for this item (useful for runtime changes)
    /// </summary>
    public void SetValueRange(float minValue, float maxValue)
    {
        valueRange = new Vector2(minValue, maxValue);
    }

    /// <summary>
    /// Sets whether this item should use random values or a fixed value
    /// </summary>
    public void SetUseRandomValue(bool useRandom)
    {
        useRandomValue = useRandom;
    }

    /// <summary>
    /// Sets the fixed value for this item
    /// </summary>
    public void SetFixedValue(float value)
    {
        fixedValue = value;
    }
}

public enum ItemType
{
    Scrap,
    Ingredient,
    Consumable
}