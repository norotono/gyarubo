using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("--- Board Settings ---")]
    public GameObject tilePrefab;
    public Transform boardRoot;
    public float tileSpacing = 1.1f;
    public int tilesPerLine = 8;

    [Header("--- Tile Assets ---")]
    public Sprite startSprite;
    public Sprite goalSprite;
    // タイルの色はTileData側で管理、またはここから渡す設計も可能

    public List<string> BoardLayout { get; private set; }
    public int totalTiles = 48; // 1フロアのマス数
    private List<GameObject> spawnedTiles = new List<GameObject>();
    private GameObject playerPiece;

    public void InitializeBoard(int grade)
    {
        foreach (var t in spawnedTiles) Destroy(t);
        spawnedTiles.Clear();

        GenerateLayout(grade);

        for (int i = 0; i < BoardLayout.Count; i++)
        {
            GameObject tile = Instantiate(tilePrefab, boardRoot);
            tile.name = $"Tile_{i}";

            int row = i / tilesPerLine;
            int col = i % tilesPerLine;
            if (row % 2 == 1) col = (tilesPerLine - 1) - col;

            tile.transform.localPosition = new Vector3(col * tileSpacing, row * tileSpacing, 0);

            SetupTileVisual(tile, BoardLayout[i], i);
            spawnedTiles.Add(tile);
        }
    }

    void GenerateLayout(int grade)
    {
        BoardLayout = new List<string>();

        for (int i = 0; i < totalTiles; i++)
        {
            // ▼▼▼ 修正箇所：2階以降の最初のマスを「イベントマス」にする ▼▼▼
            if (i == 0)
            {
                if (grade == 1)
                {
                    BoardLayout.Add("Start"); // 1階はスタート地点
                }
                else
                {
                    BoardLayout.Add("Event"); // 2,3階の最初はイベントマス（エラー回避）
                }
            }
            // ▲▲▲ 修正箇所終わり ▲▲▲

            else if (i == totalTiles - 1) BoardLayout.Add("Goal");
            else if (i == 24) BoardLayout.Add("Middle");
            else if (i == 10 || i == 30) BoardLayout.Add("Shop");
            else
            {
                int r = Random.Range(0, 100);
                if (r < 25) BoardLayout.Add("GPPlus");
                else if (r < 45) BoardLayout.Add("GPMinus");
                else if (r < 60) BoardLayout.Add("FriendPlus");
                else if (r < 70) BoardLayout.Add("FriendMinus");
                else if (r < 85) BoardLayout.Add("Event");
                else BoardLayout.Add("Male");
            }
        }
    }

    void SetupTileVisual(GameObject tileObj, string typeStr, int index)
    {
        // 修正したTileDataのメソッドを呼び出す
        TileData td = tileObj.GetComponent<TileData>();
        if (td != null)
        {
            td.index = index;
            td.SetLabel(typeStr); // これでエラーCS1061は解消されます

            // 文字列からEnumへの変換（TileDataの色分けを機能させるため）
            switch (typeStr)
            {
                case "Start": td.type = TileType.Start; break;
                case "Goal": td.type = TileType.Goal; break;
                case "GPPlus": td.type = TileType.GP_Plus; break;
                case "GPMinus": td.type = TileType.GP_Minus; break;
                case "FriendPlus": td.type = TileType.Friend_Plus; break;
                case "FriendMinus": td.type = TileType.Friend_Minus; break;
                case "Event": td.type = TileType.Event; break;
                case "Male": td.type = TileType.Boy; break; // Male -> Boy
                case "Shop": td.type = TileType.Shop; break;
                case "Middle": td.type = TileType.Normal; break; // 中間はNormal扱いでindex判定
                default: td.type = TileType.Normal; break;
            }

            td.UpdateVisuals();
        }
        else
        {
            // TileDataがない場合のフォールバック（SpriteRenderer直変え）
            SpriteRenderer sr = tileObj.GetComponent<SpriteRenderer>();
            if (sr)
            {
                if (typeStr == "Start" && startSprite) sr.sprite = startSprite;
                else if (typeStr == "Goal" && goalSprite) sr.sprite = goalSprite;
                // 他の色設定などは必要に応じて追加
            }
        }
    }

    public void MovePlayerPiece(int tileIndex)
    {
        if (spawnedTiles.Count == 0) return;
        if (tileIndex >= spawnedTiles.Count) tileIndex = spawnedTiles.Count - 1;

        if (playerPiece == null)
        {
            playerPiece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerPiece.transform.localScale = Vector3.one * 0.5f;
            playerPiece.GetComponent<Renderer>().material.color = Color.magenta;
            Destroy(playerPiece.GetComponent<SphereCollider>());
        }

        Vector3 targetPos = spawnedTiles[tileIndex].transform.position;
        targetPos.z = -0.5f;
        playerPiece.transform.position = targetPos;
    }
}