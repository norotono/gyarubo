using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TileData : MonoBehaviour
{
    public TileType type;
    public int index;

    public void UpdateVisuals()
    {
        Image img = GetComponent<Image>();
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();

        if (img == null) return;

        string symbol = "";
        Color c = Color.white;

        switch (type)
        {
            case TileType.Start: c = Color.white; symbol = "S"; break;
            case TileType.Goal: c = Color.red; symbol = "G"; break;
            case TileType.GP_Plus: c = new Color(1f, 0.92f, 0.016f); symbol = "Å™"; break;
            case TileType.GP_Minus: c = new Color(0.5f, 0.5f, 0.5f); symbol = "Å´"; break;
            case TileType.Friend_Plus: c = new Color(1f, 0.6f, 0.8f); symbol = "Å™"; break;
            case TileType.Friend_Minus: c = new Color(0.5f, 0.2f, 0.2f); symbol = "Å´"; break;
            case TileType.Event: c = new Color(0.6f, 0.2f, 1f); symbol = "!"; break;
            case TileType.Boy: c = new Color(0.4f, 0.6f, 1f); symbol = "Åâ"; break;
            case TileType.Shop: c = new Color(0f, 0.8f, 0f); symbol = "$"; break;
            case TileType.Classroom: c = new Color(0.4f, 1f, 1f); symbol = "D"; break;
            default:
                if (index == 24) { c = Color.red; symbol = "M"; } // íÜä‘
                else { c = Color.white; symbol = ""; }
                break;
        }

        img.color = c;
        if (text != null) text.text = symbol;
    }
}