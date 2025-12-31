using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // ランダム割り当てに必要

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
    public List<FriendData> allFriends; // 親友全10名を登録
    public ItemData normalMoveCard;     // リカが生成する「移動カード(通常)」のデータ参照

    private PlayerStats stats;
    private int currentTileIndex = 0;
    private bool isMoving = false;

    // --- 初期化 ---
    void Start()
    {
        stats = PlayerStats.Instance;

        // 親友条件のランダム割り当て (アイ以外)
        AssignFriendConditions();

        // 各マネージャー初期化
        if (phoneUI) phoneUI.Initialize(this);
        if (boardManager) boardManager.InitializeBoard(stats.currentGrade);
        if (shopManager) shopManager.SetupItems(stats.currentGrade);

        // プレイヤー位置初期化
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

    // --- 親友条件の割り当て ---
    // --- 1. 親友条件の割り当て (ランダム仕様対応) ---
    void AssignFriendConditions()
    {
        // ランダム割り当て用の条件プール
        List<ScoutConditionType> pool = new List<ScoutConditionType>
        {
            ScoutConditionType.MaleContact, ScoutConditionType.HighGP_Tile,
            ScoutConditionType.LowGP_Tile,  ScoutConditionType.DiceOne,
            ScoutConditionType.RichGP,      ScoutConditionType.HighSpend,
            ScoutConditionType.HighFriends, ScoutConditionType.HighSteps,
            ScoutConditionType.Solitude,    ScoutConditionType.HighStatus
        };
        pool = pool.OrderBy(x => Random.value).ToList();

        // アイ(固定枠)以外の9人をシャッフル
        var randomFriends = allFriends.Where(f => f.effectType != FriendEffectType.ScoreDoubler).OrderBy(x => Random.value).ToList();

        for (int i = 0; i < randomFriends.Count; i++)
        {
            FriendData f = randomFriends[i];
            f.isRecruited = false;

            if (i < 5)
            {
                // A: 教室タイプ (5名)
                f.conditionType = ScoutConditionType.Classroom;
            }
            else
            {
                // B: 特殊条件タイプ (4名)
                f.conditionType = pool[i - 5];

                // 閾値設定 (仕様書準拠)
                switch (f.conditionType)
                {
                    case ScoutConditionType.MaleContact: f.conditionValue = 4; break;
                    case ScoutConditionType.HighGP_Tile: f.conditionValue = 5; break;
                    case ScoutConditionType.LowGP_Tile: f.conditionValue = 3; break;
                    case ScoutConditionType.DiceOne: f.conditionValue = 3; break;
                    case ScoutConditionType.RichGP: f.conditionValue = 3000; break;
                    case ScoutConditionType.HighSpend: f.conditionValue = 4000; break;
                    case ScoutConditionType.HighFriends: f.conditionValue = 20; break;
                    case ScoutConditionType.HighSteps: f.conditionValue = 80; break;
                    case ScoutConditionType.Solitude: f.conditionValue = 3; break;
                    case ScoutConditionType.HighStatus: f.conditionValue = 2; break;
                }
            }
        }

        // アイ (固定)
        var ai = allFriends.FirstOrDefault(f => f.effectType == FriendEffectType.ScoreDoubler);
        if (ai != null)
        {
            ai.isRecruited = false;
            ai.conditionType = ScoutConditionType.Fixed_Ai;
            ai.conditionValue = 0;
        }
    }

    // --- 2. スカウト判定 (イベントマス用) ---
    public FriendData CheckScoutableFriend()
    {
        PlayerStats s = PlayerStats.Instance;
        foreach (var f in allFriends)
        {
            if (f.isRecruited) continue;
            if (f.conditionType == ScoutConditionType.Classroom) continue; // 教室タイプはイベントマスでは出ない

            bool met = false;
            switch (f.conditionType)
            {
                case ScoutConditionType.Fixed_Ai:
                    if (s.maleContactCount >= 8 || s.gp >= 5000) met = true; break;
                case ScoutConditionType.MaleContact:
                    if (s.maleContactCount >= f.conditionValue) met = true; break;
                case ScoutConditionType.HighGP_Tile:
                    if (s.gpPlusTileCount >= f.conditionValue) met = true; break;
                case ScoutConditionType.LowGP_Tile:
                    if (s.gpMinusTileCount >= f.conditionValue) met = true; break;
                case ScoutConditionType.DiceOne:
                    if (s.diceOneCount >= f.conditionValue) met = true; break;
                case ScoutConditionType.RichGP:
                    if (s.gp >= f.conditionValue) met = true; break;
                case ScoutConditionType.HighSpend:
                    if (s.shopSpendTotal >= f.conditionValue) met = true; break;
                case ScoutConditionType.HighFriends:
                    if (s.friends >= f.conditionValue) met = true; break;
                case ScoutConditionType.HighSteps:
                    if (s.totalSteps >= f.conditionValue) met = true; break;
                case ScoutConditionType.Solitude:
                    if (s.soloPlayConsecutive >= f.conditionValue) met = true; break;
                case ScoutConditionType.HighStatus:
                    if (s.commuLv >= f.conditionValue && s.galLv >= f.conditionValue && s.lemonLv >= f.conditionValue) met = true; break;
            }
            if (met) return f;
        }
        return null;
    }

    // --- 3. 親友加入処理 (アイの能力対応) ---
    public void RecruitFriend(FriendData f)
    {
        if (f == null) return;
        f.isRecruited = true;

        if (f.effectType == FriendEffectType.ScoreDoubler)
        {
            stats.friends *= 2;
            AddLog($"【最強】{f.characterName}が加入！ 友達数が2倍になりました！");
        }
        else
        {
            AddLog($"{f.characterName} が親友になりました！");
        }
        UpdateMainUI();
    }

    // --- 4. アイテム使用処理 (全アイテム対応) ---
    public void UseItem(ItemData item)
    {
        if (isMoving)
        {
            AddLog("移動中はアイテムを使えません。");
            return;
        }

        AddLog($"アイテム使用: {item.itemName}");
        stats.RemoveItem(item);

        switch (item.itemType)
        {
            case ItemType.MoveCard_Fixed: // 固定移動カード
                StartCoroutine(TurnSequence(item.moveSteps));
                break;

            case ItemType.MoveCard_RandomShop: // 念のため
                StartCoroutine(TurnSequence(-2));
                break;

            case ItemType.MoveCard_HighLow: // 倍額カード
                StartCoroutine(TurnSequence(-3));
                break;

            case ItemType.Recovery:
                stats.gp += item.effectValue;
                AddLog($"GPが {item.effectValue} 回復しました！");
                break;

            case ItemType.StatusUp:
                if (item.targetStatus == TargetStatus.All) { stats.commuLv++; stats.galLv++; stats.lemonLv++; }
                else if (item.targetStatus == TargetStatus.Commu) stats.commuLv++;
                else if (item.targetStatus == TargetStatus.Gal) stats.galLv++;
                else if (item.targetStatus == TargetStatus.Lemon) stats.lemonLv++;
                AddLog("ステータスがアップしました！");
                break;

            case ItemType.FriendBoost:
                stats.friends += item.effectValue;
                AddLog($"友達が {item.effectValue}人 増えました！");
                break;

            case ItemType.DynamicPrice:
                stats.friends += item.effectValue;
                stats.gradAlbumBuyCount++;
                AddLog($"アルバム効果で友達 +{item.effectValue}人！");
                break;

            case ItemType.ForceEvent:
                AddLog("イベント強制(未実装)");
                break;
        }
        UpdateMainUI();
    }

    // --- 5. マス到着時の処理 (親友能力の実装) ---
    void OnTileReached(int index)
    {
        string type = "Normal";
        if (boardManager != null && boardManager.BoardLayout != null && index < boardManager.BoardLayout.Count)
            type = boardManager.BoardLayout[index];

        // 親友能力チェック
        bool doubleEffect = HasFriendEffect(FriendEffectType.DoubleTileEffect); // ユナ
        bool nullifyMinus = HasFriendEffect(FriendEffectType.NullifyGPMinus);   // サオリ
        bool badToGP = HasFriendEffect(FriendEffectType.BadToGP);               // ノア (★変数名修正済み)
        bool gpBoost = HasFriendEffect(FriendEffectType.GPMultiplier);          // ミレイ
        bool hasEmi = HasFriendEffect(FriendEffectType.ShopDiscount);           // エミ

        int multiplier = doubleEffect ? 2 : 1;

        switch (type)
        {
            case "GPPlus":
                int gain = (stats.currentGrade * 100) * multiplier;
                if (gpBoost) gain = (int)(gain * 1.5f);
                stats.gp += gain;
                stats.gpPlusTileCount++;
                AddLog($"バイト代 +{gain}GP");
                break;

            case "GPMinus":
                int loss = (stats.currentGrade * 100) * multiplier;
                if (nullifyMinus) { loss = 0; AddLog("サオリ効果で回避！"); }
                else if (badToGP) { stats.gp += loss; AddLog($"ノア効果で +{loss}GP！"); loss = 0; }

                if (loss > 0)
                {
                    stats.gp = Mathf.Max(0, stats.gp - loss);
                    stats.gpMinusTileCount++;
                    AddLog($"出費... -{loss}GP");
                }
                break;

            case "Shop":
                AddLog("購買部に到着");
                StartCoroutine(shopManager.OpenShop(true || hasEmi)); // 停止時は割引
                break;

            case "Event":
                FriendData target = CheckScoutableFriend();
                if (target != null) RecruitFriend(target);
                else AddLog("ランダムイベント発生");
                break;
        }
        EndTurn();
    }

    // --- ゲーム進行 ---
    void OnDiceClicked()
    {
        if (isMoving) return;

        // カオルの能力: ダイスリロール (簡易実装: 確率で良い目が出るなど、今回は通常通り)
        StartCoroutine(TurnSequence(-1));
    }

    // mode: -1=Dice, -2=RandomCard, -3=HighCard, >0=Fixed
    IEnumerator TurnSequence(int mode)
    {
        isMoving = true;
        diceButton.interactable = false;

        int steps = 0;

        if (mode == -1 || mode == -2)
        {
            // ダイス演出
            float t = 0;
            while (t < 0.5f)
            {
                if (diceImage) diceImage.sprite = diceSprites[Random.Range(0, 6)];
                yield return new WaitForSeconds(0.05f);
                t += 0.05f;
            }
            steps = Random.Range(1, 7);
            if (diceImage) diceImage.sprite = diceSprites[steps - 1];

            // 統計
            if (mode == -1)
            {
                stats.totalSteps += steps;
                if (steps == 1) stats.diceOneCount++;
            }
            yield return new WaitForSeconds(0.5f);
        }
        else if (mode == -3) // High Card
        {
            steps = Random.Range(4, 7);
            AddLog($"Highカード発動！ {steps}マス進みます。");
        }
        else if (mode > 0) // 固定移動
        {
            steps = mode;
            AddLog($"カード効果で {steps}マス進みます。");
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

            // 通過チェック: 購買部
            string tileType = GetTileType(currentTileIndex);
            if (tileType == "Shop" && i < steps - 1)
            {
                // エミの能力: 割引
                bool hasEmi = HasFriendEffect(FriendEffectType.ShopDiscount);
                AddLog("購買部を通過します。");
                yield return StartCoroutine(shopManager.OpenShop(hasEmi)); // 通過でも割引有効ならtrue
            }

            yield return new WaitForSeconds(0.3f);
        }
        OnTileReached(currentTileIndex);
    }

    // --- 5. マス到着時の処理 (親友能力の実装) ---
    void OnTileReached(int index)
    {
        string type = "Normal";
        if (boardManager != null && boardManager.BoardLayout != null && index < boardManager.BoardLayout.Count)
            type = boardManager.BoardLayout[index];

        // 親友能力チェック
        bool doubleEffect = HasFriendEffect(FriendEffectType.DoubleTileEffect); // ユナ
        bool nullifyMinus = HasFriendEffect(FriendEffectType.NullifyGPMinus);   // サオリ
        bool badToGP = HasFriendEffect(FriendEffectType.BadToGP);               // ノア (★変数名修正済み)
        bool gpBoost = HasFriendEffect(FriendEffectType.GPMultiplier);          // ミレイ
        bool hasEmi = HasFriendEffect(FriendEffectType.ShopDiscount);           // エミ

        int multiplier = doubleEffect ? 2 : 1;

        switch (type)
        {
            case "GPPlus":
                int gain = (stats.currentGrade * 100) * multiplier;
                if (gpBoost) gain = (int)(gain * 1.5f);
                stats.gp += gain;
                stats.gpPlusTileCount++;
                AddLog($"バイト代 +{gain}GP");
                break;

            case "GPMinus":
                int loss = (stats.currentGrade * 100) * multiplier;
                if (nullifyMinus) { loss = 0; AddLog("サオリ効果で回避！"); }
                else if (badToGP) { stats.gp += loss; AddLog($"ノア効果で +{loss}GP！"); loss = 0; }

                if (loss > 0)
                {
                    stats.gp = Mathf.Max(0, stats.gp - loss);
                    stats.gpMinusTileCount++;
                    AddLog($"出費... -{loss}GP");
                }
                break;

            case "Shop":
                AddLog("購買部に到着");
                StartCoroutine(shopManager.OpenShop(true || hasEmi)); // 停止時は割引
                break;

            case "Event":
                FriendData target = CheckScoutableFriend();
                if (target != null) RecruitFriend(target);
                else AddLog("ランダムイベント発生");
                break;
        }
        EndTurn();
    }

    void HandleEventTile()
    {
        // 1. 同伴 (未実装)
        // 2. 親友スカウト
        FriendData target = CheckScoutableFriend();
        if (target != null)
        {
            RecruitFriend(target);
        }
        else
        {
            // 3. ランダムイベント
            AddLog("イベント発生 (内容はランダム)");
        }
    }

    void EndTurn()
    {
        // レナ: 毎ターン友達+1
        if (HasFriendEffect(FriendEffectType.AutoFriend))
        {
            stats.friends++;
        }

        // リカ: 12ターン毎にカード生成
        if (HasFriendEffect(FriendEffectType.CardGenerator))
        {
            if (stats.turnCount > 0 && stats.turnCount % 12 == 0)
            {
                if (!stats.IsMoveCardFull() && normalMoveCard != null)
                {
                    stats.moveCards.Add(normalMoveCard);
                    AddLog("リカが移動カードを作ってくれました！");
                }
            }
        }

        stats.turnCount++;
        UpdateMainUI();
    }

    // --- 補助メソッド ---
    string GetTileType(int index)
    {
        if (boardManager != null && boardManager.BoardLayout != null && index < boardManager.BoardLayout.Count)
            return boardManager.BoardLayout[index];
        return "Normal";
    }

    bool HasFriendEffect(FriendEffectType type)
    {
        return allFriends.Any(f => f.isRecruited && f.effectType == type);
    }

    void UpdateMainUI()
    {
        if (dateText) dateText.text = $"{stats.currentGrade}年";
        if (assetText) assetText.text = $"GP: {stats.gp:N0}\n友: {stats.friends}人";
    }

    void AddLog(string msg)
    {
        Debug.Log(msg);
        if (phoneUI) phoneUI.AddLog(msg);
    }
}