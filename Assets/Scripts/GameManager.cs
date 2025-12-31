using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("--- Managers ---")]
    public BoardManager boardManager;
    public ShopManager shopManager;
    public EventManager eventManager;
    public PhoneUIManager phoneUI;

    [Header("--- UI References ---")]
    public Button diceButton;
    public Image diceImage;
    public Sprite[] diceSprites;
    public TextMeshProUGUI dateText, assetText;

    [Header("--- Data ---")]
    public List<FriendData> allFriends;

    private PlayerStats stats;
    private int currentTileIndex = 0;
    private bool isMoving = false;

    void Start()
    {
        stats = PlayerStats.Instance;
        foreach (var f in allFriends) f.isRecruited = false;

        if (phoneUI) phoneUI.Initialize(this);
        if (boardManager) boardManager.InitializeBoard(stats.currentGrade);
        if (shopManager) shopManager.SetupItems(stats.currentGrade); // ここはShopManagerの実装次第

        currentTileIndex = 0;
        StartCoroutine(InitPosition());
        UpdateMainUI();

        if (diceButton) diceButton.onClick.AddListener(OnDiceClicked);
    }

    IEnumerator InitPosition()
    {
        yield return null;
        if (boardManager) boardManager.MovePlayerPiece(0);
    }

    // --- アイテム使用ロジック (修正版) ---
    public void UseItem(ItemData item)
    {
        if (isMoving)
        {
            AddLog("移動中はアイテムを使えません。");
            return;
        }

        AddLog($"アイテム使用: {item.itemName}");

        // PlayerStats側で削除処理を行う
        stats.RemoveItem(item);

        switch (item.itemType)
        {
            case ItemType.MoveCard:
                // moveStepsを使用して移動
                StartCoroutine(TurnSequence(item.moveSteps));
                break;

            case ItemType.Recovery:
                // effectValueを使用して回復
                stats.gp += item.effectValue;
                AddLog($"GPが {item.effectValue} 回復しました。");
                UpdateMainUI();
                break;

            case ItemType.EventForce:
                AddLog("不思議な力が働いた！（未実装）");
                break;
        }
    }

    // --- ゲーム進行 ---
    void OnDiceClicked()
    {
        if (isMoving) return;
        StartCoroutine(TurnSequence(-1));
    }

    IEnumerator TurnSequence(int fixedSteps)
    {
        isMoving = true;
        diceButton.interactable = false;

        int steps = fixedSteps;

        // ダイスの場合 (-1)
        if (steps == -1)
        {
            float t = 0;
            while (t < 0.5f)
            {
                if (diceImage && diceSprites.Length > 0)
                    diceImage.sprite = diceSprites[Random.Range(0, diceSprites.Length)];
                yield return new WaitForSeconds(0.05f);
                t += 0.05f;
            }
            steps = Random.Range(1, 7);
            if (diceImage && diceSprites.Length > 0)
                diceImage.sprite = diceSprites[steps - 1];

            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            AddLog($"カード効果で {steps} マス進みます。");
        }

        yield return StartCoroutine(MovePlayer(steps));

        isMoving = false;
        diceButton.interactable = true;
    }

    IEnumerator MovePlayer(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            currentTileIndex++;
            if (boardManager) boardManager.MovePlayerPiece(currentTileIndex);

            // 進級・ゴール判定等はここに記述 (省略)

            yield return new WaitForSeconds(0.3f);
        }
        OnTileReached(currentTileIndex);
    }

    void OnTileReached(int index)
    {
        // マス効果の実装 (省略)
        // 必要に応じてEndTurnを呼ぶ
        EndTurn();
    }

    void EndTurn()
    {
        UpdateMainUI();
    }

    void UpdateMainUI()
    {
        if (dateText) dateText.text = $"{stats.currentGrade}年 {stats.currentMonth}月";
        if (assetText) assetText.text = $"GP: {stats.gp:N0}\n友: {stats.friends}";
    }

    void AddLog(string msg)
    {
        Debug.Log(msg);
        if (phoneUI) phoneUI.AddLog(msg);
    }
}