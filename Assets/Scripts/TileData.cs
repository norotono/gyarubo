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

    // ★追加: BoardManagerから呼ばれるメソッド
    public void SetLabel(string text)
    {
        // タイルのテキストを更新（デバッグ表示や種別表示用）
        TextMeshProUGUI tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
        }
    }

    public void UpdateVisuals()
    {
        // ImageはUI用、SpriteRendererは2D用。両対応できるようにチェック推奨
        Image img = GetComponent<Image>();
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();

        // コンポーネントがなければ何もしない（エラー回避）
        if (img == null && GetComponent<SpriteRenderer>() == null) return;

        // 色の設定（Image優先、なければSpriteRenderer）
        Color c = Color.white;
        string symbol = "";

        switch (type)
        {
            case TileType.Start:
                c = Color.white; symbol = "S"; break;
            case TileType.Goal:
                c = Color.red; symbol = "G"; break;
            case TileType.GP_Plus:
                c = new Color(1f, 0.92f, 0.016f); symbol = "↑"; break;
            case TileType.GP_Minus:
                c = new Color(1f, 0.92f, 0.016f); symbol = "↓"; break;
            case TileType.Friend_Plus:
                c = new Color(1f, 0.6f, 0.8f); symbol = "↑"; break;
            case TileType.Friend_Minus:
                c = new Color(1f, 0.6f, 0.8f); symbol = "↓"; break;
            case TileType.Event:
                c = new Color(0.6f, 0.2f, 1f); symbol = "!"; break;
            case TileType.Boy:
                c = new Color(0.4f, 0.6f, 1f); symbol = "♂"; break;
            case TileType.Shop:
                c = new Color(0f, 0.8f, 0f); symbol = "$"; break;
            case TileType.Classroom:
                c = new Color(0.4f, 1f, 1f); symbol = "D"; break;
            default:
                if (index == 24)
                {
                    c = Color.red; symbol = "M"; // Middle
                }
                else
                {
                    c = Color.white; symbol = "";
                }
                break;
        }

        if (img != null) img.color = c;
        else if (GetComponent<SpriteRenderer>() != null) GetComponent<SpriteRenderer>().color = c;

        if (text != null) text.text = symbol;
    }
}