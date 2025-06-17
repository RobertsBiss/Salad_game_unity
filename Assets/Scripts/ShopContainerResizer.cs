using UnityEngine;
using UnityEngine.UI;

public class ShopContainerResizer : MonoBehaviour
{
    public RectTransform shopContainer; // Assign in inspector
    public HorizontalLayoutGroup layoutGroup; // Assign in inspector

    void Update()
    {
        ResizeItems();
    }

    void ResizeItems()
    {
        int itemCount = shopContainer.childCount;
        if (itemCount == 0) return;

        float totalWidth = shopContainer.rect.width;
        float spacing = layoutGroup.spacing;
        float padding = layoutGroup.padding.left + layoutGroup.padding.right;

        // Calculate available width for all items
        float availableWidth = totalWidth - padding - (spacing * (itemCount - 1));
        float itemWidth = availableWidth / itemCount;

        // Set each item's preferred width
        foreach (RectTransform child in shopContainer)
        {
            LayoutElement le = child.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = itemWidth;
            }
        }
    }
}