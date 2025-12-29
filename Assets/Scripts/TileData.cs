using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TileType
{
    Normal, GP_Plus, GP_Minus, Friend_Plus, Friend_Minus,
    Event, Boy, Shop, Classroom, Goal, Start
}

public class TileData : MonoBehaviour
{
    public TileType type;
    public int index;

    public void UpdateVisuals()
    {
        Image img = GetComponent<Image>();
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();

        if (img == null || text == null) return;

        string symbol = "";

        switch (type)
        {
            case TileType.Start:
                img.color = Color.white; symbol = "S"; break; // Start
            case TileType.Goal:
                img.color = Color.red; symbol = "G"; break;   // Goal
            case TileType.GP_Plus:
                img.color = new Color(1f, 0.92f, 0.016f); symbol = "Å™"; break;
            case TileType.GP_Minus:
                img.color = new Color(1f, 0.92f, 0.016f); symbol = "Å´"; break;
            case TileType.Friend_Plus:
                img.color = new Color(1f, 0.6f, 0.8f); symbol = "Å™"; break;
            case TileType.Friend_Minus:
                img.color = new Color(1f, 0.6f, 0.8f); symbol = "Å´"; break;
            case TileType.Event:
                img.color = new Color(0.6f, 0.2f, 1f); symbol = "!"; break;
            case TileType.Boy:
                img.color = new Color(0.4f, 0.6f, 1f); symbol = "Åâ"; break;
            case TileType.Shop:
                img.color = new Color(0f, 0.8f, 0f); symbol = "$"; break; // Shop
            case TileType.Classroom:
                img.color = new Color(0.4f, 1f, 1f); symbol = "D"; break; // Door
            default:
                if (index == 24)
                {
                    img.color = Color.red; symbol = "M"; // Middle (íÜä‘)
                }
                else
                {
                    img.color = Color.white; symbol = ""; // í èÌÉ}ÉXÇÕãÛóìÇ©êîéö
                }
                break;
        }
        text.text = symbol;
    }
}