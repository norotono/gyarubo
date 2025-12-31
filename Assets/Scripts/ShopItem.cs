using System;

[Serializable]
public class ShopItem
{
    public string itemName;
    public int price;
    public string description;
    public Action onBuy;

    public ShopItem(string name, int price, string desc, Action onBuyAction)
    {
        itemName = name;
        this.price = price; // ‰¿Ši”½‰fƒoƒOC³Ï‚İ
        description = desc;
        onBuy = onBuyAction;
    }
}