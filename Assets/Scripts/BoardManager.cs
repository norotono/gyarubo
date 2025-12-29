using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    [Header("--- Map Settings ---")]
    public GameObject tilePrefab;
    public Transform boardParent;
    public int totalTiles = 48;

    [Header("--- Player Settings ---")]
    public GameObject playerPrefab;
    public Transform playerPiece;

    public string[] BoardLayout { get; private set; }

    public void InitializeBoard(int currentGrade)
    {
        // 古いマス削除
        foreach (Transform child in boardParent) Destroy(child.gameObject);

        BoardLayout = new string[totalTiles];

        // 1. 固定マス
        BoardLayout[0] = (currentGrade == 1) ? "Start" : "Normal";
        BoardLayout[24] = "Middle";

        if (currentGrade == 3)
        {
            BoardLayout[totalTiles - 1] = "Goal";
            BoardLayout[totalTiles - 2] = "Shop";
        }
        else
        {
            BoardLayout[totalTiles - 1] = "Shop";
        }

        // 教室
        if (currentGrade >= 2)
        {
            int[] cIdx = { 5, 13, 21, 29, 37, 45 };
            foreach (int i in cIdx)
                if (i < totalTiles && string.IsNullOrEmpty(BoardLayout[i]))
                    BoardLayout[i] = "Classroom";
        }

        // 2. ランダムマス
        List<string> randomTiles = new List<string>();
        if (currentGrade == 1)
        {
            AddTiles(randomTiles, "Shop", 1);
            AddTiles(randomTiles, "Male", 12); AddTiles(randomTiles, "Event", 12);
            AddTiles(randomTiles, "FriendPlus", 8); AddTiles(randomTiles, "GPPlus", 6);
            AddTiles(randomTiles, "GPMinus", 4); AddTiles(randomTiles, "FriendMinus", 2);
        }
        else
        {
            if (currentGrade == 2) AddTiles(randomTiles, "Shop", 1);
            AddTiles(randomTiles, "GPPlus", 10); AddTiles(randomTiles, "FriendPlus", 8);
            AddTiles(randomTiles, "Event", 12); AddTiles(randomTiles, "GPMinus", 4);
            AddTiles(randomTiles, "Male", 3); AddTiles(randomTiles, "FriendMinus", 2);
        }

        randomTiles = randomTiles.OrderBy(x => Random.value).ToList();

        int pIdx = 0;
        for (int i = 0; i < totalTiles; i++)
        {
            if (string.IsNullOrEmpty(BoardLayout[i]))
            {
                BoardLayout[i] = (pIdx < randomTiles.Count) ? randomTiles[pIdx++] : "Normal";
            }
        }

        // 3. プレイヤー生成
        if (playerPiece == null && playerPrefab != null)
        {
            GameObject pObj = Instantiate(playerPrefab);
            pObj.name = "PlayerPiece";
            playerPiece = pObj.transform;
            // 必要ならCanvas内に入れる等の処理が必要だが、今回はWorld座標移動前提とする
            // Canvas上で動かす場合は boardParent の子にするのも手
            // playerPiece.SetParent(boardParent, false); 
        }

        // 4. マス生成
        GenerateVisualTiles();
    }

    void AddTiles(List<string> list, string type, int count) { for (int i = 0; i < count; i++) list.Add(type); }

    void GenerateVisualTiles()
    {
        int columns = 8;
        for (int visualIndex = 0; visualIndex < totalTiles; visualIndex++)
        {
            int row = visualIndex / columns;
            int col = visualIndex % columns;

            // スネイク: 偶数はそのまま、奇数は反転
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

    public void MovePlayerPiece(int logicalIndex)
    {
        if (boardParent.childCount <= logicalIndex || playerPiece == null) return;

        int columns = 8;
        int row = logicalIndex / columns;
        int col = logicalIndex % columns;
        // 論理IDから視覚ID(子要素順)へ変換
        int visualIndex = (row % 2 == 0) ? logicalIndex : (row * columns) + (columns - 1 - col);

        if (visualIndex >= 0 && visualIndex < boardParent.childCount)
        {
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