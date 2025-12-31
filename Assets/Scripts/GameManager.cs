using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // List操作用
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

    void AssignFriendConditions()
    {
        // 条件リスト定義
        List<ScoutConditionType> conditions = new List<ScoutConditionType>
        {
            ScoutConditionType.MaleContact,
            ScoutConditionType.HighGP_Tile,
            ScoutConditionType.LowGP_Tile,
            ScoutConditionType.DiceOne,
            ScoutConditionType.RichGP,
            ScoutConditionType.HighSpend,
            ScoutConditionType.HighFriends,
            ScoutConditionType.HighSteps,
            ScoutConditionType.Solitude,
            ScoutConditionType.HighStatus
        };

        // シャッフル
        conditions = conditions.OrderBy(x => Random.value).ToList();

        // アイ以外の友達を抽出
        var randomFriends = allFriends.Where(f => f.effectType != FriendEffectType.ScoreDoubler).ToList();
        randomFriends = randomFriends.OrderBy(x => Random.value).ToList();

        // 割り当て (5人が教室、4人が特殊条件)
        for (int i = 0; i < randomFriends.Count; i++)
        {
            FriendData f = randomFriends[i];
            f.isRecruited = false; // リセット

            if (i < 5)
            {
                // A: 教室タイプ
                f.assignedCondition = ScoutConditionType.Classroom;
                // ※本来は各階の教室マスにこのキャラを紐付ける処理が必要
            }
            else
            {
                // B: 特殊条件タイプ
                f.assignedCondition = conditions[i - 5];

                // 閾値の設定 (ハードコードまたは別途データ化)
                switch (f.assignedCondition)
                {
                    case ScoutConditionType.MaleContact: f.conditionThreshold = 4; break;
                    case ScoutConditionType.HighGP_Tile: f.conditionThreshold = 5; break;
                    case ScoutConditionType.LowGP_Tile: f.conditionThreshold = 3; break;
                    case ScoutConditionType.DiceOne: f.conditionThreshold = 3; break;
                    case ScoutConditionType.RichGP: f.conditionThreshold = 3000; break;
                    case ScoutConditionType.HighSpend: f.conditionThreshold = 4000; break;
                    case ScoutConditionType.HighFriends: f.conditionThreshold = 20; break;
                    case ScoutConditionType.HighSteps: f.conditionThreshold = 80; break;
                    case ScoutConditionType.Solitude: f.conditionThreshold = 3; break;
                    case ScoutConditionType.HighStatus: f.conditionThreshold = 2; break;
                }
            }
        }

        // アイの設定 (固定)
        var ai = allFriends.FirstOrDefault(f => f.effectType == FriendEffectType.ScoreDoubler);
        if (ai != null)
        {
            ai.isRecruited = false;
            ai.assignedCondition = ScoutConditionType.Fixed_Ai;
        }
    }

    // スカウト判定
    public FriendData CheckScoutableFriend()
    {
        PlayerStats s = PlayerStats.Instance;

        foreach (var f in allFriends)
        {
            if (f.isRecruited) continue;
            if (f.assignedCondition == ScoutConditionType.Classroom) continue; // 教室タイプはイベントマスでは出ない

            bool met = false;
            switch (f.assignedCondition)
            {
                case ScoutConditionType.Fixed_Ai:
                    if (s.maleContactCount >= 8 || s.gp >= 5000) met = true;
                    break;
                case ScoutConditionType.MaleContact:
                    if (s.maleContactCount >= f.conditionThreshold) met = true;
                    break;
                case ScoutConditionType.HighGP_Tile:
                    if (s.gpPlusTileCount >= f.conditionThreshold) met = true;
                    break;
                // ... 他の条件も同様に記述 ...
                case ScoutConditionType.HighStatus:
                    if (s.commuLv >= 2 && s.galLv >= 2 && s.lemonLv >= 2) met = true;
                    break;
            }

            if (met) return f;
        }
        return null;
    }
}
IEnumerator InitPosition()
    {
        yield return null;
        if (boardManager) boardManager.MovePlayerPiece(0);
    }

    // --- アイテム使用ロジック (修正版) ---
    // GameManager.cs の UseItem メソッドのみ抜粋・修正
    // 既存の UseItem メソッドをこれで上書きしてください

    public void UseItem(ItemData item)
    {
        // 移動中の使用禁止
        if (isMoving) { AddLog("移動中は使えません。"); return; }

        AddLog($"アイテム使用: {item.itemName}");

        // インベントリから削除（PlayerStats側で処理）
        PlayerStats.Instance.RemoveItem(item);

        switch (item.itemType)
        {
            case ItemType.MoveCard:
                StartCoroutine(TurnSequence(item.moveSteps));
                break;

            case ItemType.Recovery:
                PlayerStats.Instance.gp += item.effectValue;
                AddLog($"GPが {item.effectValue} 回復しました！");
                UpdateMainUI();
                break;

            case ItemType.StatusUp:
                switch (item.targetStatus)
                {
                    case TargetStatus.Commu:
                        PlayerStats.Instance.commuLv += item.effectValue;
                        AddLog($"コミュ力が {item.effectValue} 上がった！");
                        break;
                    case TargetStatus.Gal:
                        PlayerStats.Instance.galLv += item.effectValue;
                        AddLog($"ギャル力が {item.effectValue} 上がった！");
                        break;
                    case TargetStatus.Lemon:
                        PlayerStats.Instance.lemonLv += item.effectValue;
                        AddLog($"レモン力が {item.effectValue} 上がった！");
                        break;
                }
                UpdateMainUI();
                break;

            case ItemType.Special:
                // 卒業証書: モブ親友昇格（仕様書：友達1人をモブ親友(30点相当)にする）
                // 簡易実装として、友達数を減らさずにスコア用の隠しパラメータを加算、または単に友達+30する
                PlayerStats.Instance.friends += item.effectValue;
                AddLog($"卒業証書を使いました！ 友達評価が大幅アップ (+{item.effectValue})");
                UpdateMainUI();
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