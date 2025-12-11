using System;

[Serializable]
public class ShopItem
{
    public string itemName;    // ¤•i–¼
    public int price;          // ‰¿Ši
    public string description; // à–¾
    public Action onBuy;       // ”ƒ‚Á‚½‚ÌŒø‰Ê

    public ShopItem(string name, int price, string desc, Action onBuyAction)
    {
        itemName = name;
        price = price;
        description = desc;
        onBuy = onBuyAction;
    }
}