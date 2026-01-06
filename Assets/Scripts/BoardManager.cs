using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    [Header("--- Map Settings ---")]
    public GameObject tilePrefab;
    public Transform boardParent;
    public Transform playerPiece;
    public int totalTiles = 48;

    // 外部から現在のマップ情報を参照するためのプロパティ
    public string[] BoardLayout { get; private set; }

    // 初期化処理
    // 初期化処理
    public void InitializeBoard(int currentGrade)
    {
        // 既存のマスの削除
        foreach (Transform child in boardParent) Destroy(child.gameObject);

        BoardLayout = new string[totalTiles];

        // 1. 固定マスの配置
        // ★修正: 1年生は "Start"、2年生以降は "Event" にする
        if (currentGrade == 1)
        {
            BoardLayout[0] = "Start";
        }
        else
        {
            // 2,3年生はスタート地点がイベントマスになる
            BoardLayout[0] = "Event";
        }

        // 中間地点の設定（24番目のマス）
        // ※もし2年生以降、中間地点もイベントマスにするならここも調整可能ですが、
        //  要望にはなかったのでそのまま "Middle" にしておきます。
        BoardLayout[24] = "Middle";

        // 学年ごとのゴール・ショップ配置
        if (currentGrade == 3)
        {
            BoardLayout[totalTiles - 1] = "Goal";
            BoardLayout[totalTiles - 2] = "Shop";
        }
        else
        {
            BoardLayout[totalTiles - 1] = "Shop"; // 次の階へ続く
        }

        // 教室マスの配置 (2年生以上)
        if (currentGrade >= 2)
        {
            int[] cIdx = { 5, 13, 21, 29, 37, 45 };
            foreach (int i in cIdx)
            {
                if (i < totalTiles && string.IsNullOrEmpty(BoardLayout[i]))
                    BoardLayout[i] = "Classroom";
            }
        }

        // 2. ランダムマスの配置
        List<string> p = new List<string>();

        if (currentGrade == 1)
        {
            AddTiles(p, "Shop", 1);
            AddTiles(p, "Male", 12);
            AddTiles(p, "Event", 12);
            AddTiles(p, "FriendPlus", 8);
            AddTiles(p, "GPPlus", 6);
            AddTiles(p, "GPMinus", 4);
            AddTiles(p, "FriendMinus", 2);
        }
        else
        {
            // 2,3年生用バランス
            if (currentGrade == 2) AddTiles(p, "Shop", 1);
            AddTiles(p, "GPPlus", 10);
            AddTiles(p, "FriendPlus", 8);
            AddTiles(p, "Event", 12);
            AddTiles(p, "GPMinus", 4);
            AddTiles(p, "Male", 3);
            AddTiles(p, "FriendMinus", 2);
        }

        // シャッフル
        p = p.OrderBy(x => Random.value).ToList();

        // 空きマスに埋める
        int pIdx = 0;
        for (int i = 0; i < totalTiles; i++)
        {
            if (string.IsNullOrEmpty(BoardLayout[i]))
            {
                BoardLayout[i] = (pIdx < p.Count) ? p[pIdx++] : "Normal";
            }
        }

        // 3. 視覚的な生成（スネイク配置）
        GenerateVisualTiles();
    }

    void AddTiles(List<string> list, string type, int count)
    {
        for (int i = 0; i < count; i++) list.Add(type);
    }

    void GenerateVisualTiles()
    {
        int columns = 8; // 1行8マス
        for (int visualIndex = 0; visualIndex < totalTiles; visualIndex++)
        {
            int row = visualIndex / columns;
            int col = visualIndex % columns;

            // スネイク変換: 奇数行は「右→左」なので論理インデックスを計算して取得
            int logicalIndex = (row % 2 == 0) ? visualIndex : (row * columns) + (columns - 1 - col);

            GameObject t = Instantiate(tilePrefab, boardParent);
            t.name = $"Tile_{logicalIndex}_{BoardLayout[logicalIndex]}";

            TileData td = t.GetComponent<TileData>();
            if (td != null)
            {
                td.index = logicalIndex;
                td.type = ConvertStr(BoardLayout[logicalIndex]);
                td.UpdateVisuals();
            }
        }
    }

    // プレイヤーの駒を移動させる
    public void MovePlayerPiece(int logicalIndex)
    {
        if (boardParent.childCount <= logicalIndex) return;

        int columns = 8;
        int row = logicalIndex / columns;
        int col = logicalIndex % columns;
        int visualIndex;

        if (row % 2 == 0)
        {
            visualIndex = logicalIndex;
        }
        else
        {
            int rowStart = row * columns;
            visualIndex = rowStart + (columns - 1 - col);
        }

        if (visualIndex >= 0 && visualIndex < boardParent.childCount)
        {
            if (playerPiece != null)
                playerPiece.position = boardParent.GetChild(visualIndex).position;
        }
    }

    TileType ConvertStr(string s)
    {
        switch (s)
        {
            case "Start": return TileType.Start;
            case "Goal": return TileType.Goal;
            case "Male": return TileType.Boy;
            case "Event": return TileType.Event;
            case "Shop": return TileType.Shop;
            case "Classroom": return TileType.Classroom;
            case "FriendPlus": return TileType.Friend_Plus;
            case "FriendMinus": return TileType.Friend_Minus;
            case "GPPlus": return TileType.GP_Plus;
            case "GPMinus": return TileType.GP_Minus;
            default: return TileType.Normal;
        }
    }
}