using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ShopItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Image iconImage;
    public Button purchaseButton;

    // ShopManager‚©‚çŒÄ‚Î‚ê‚é‰Šú‰»ƒƒ\ƒbƒh
    public void Setup(ItemData data, int price, UnityAction onClick)
    {
        if (nameText) nameText.text = data.itemName;
        if (priceText) priceText.text = $"{price:N0} GP";
        if (iconImage && data.icon) iconImage.sprite = data.icon;

        if (purchaseButton)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(onClick);
        }
    }
}