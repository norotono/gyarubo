using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("--- Managers ---")]
    public BoardManager board;
    public ShopManager shop;
    public EventManager events;
    public RelationshipManager relation;
    public UIManager ui;

    [Header("--- Controls ---")]
    public Button diceButton;
    public Image diceImage;
    public Sprite[] diceSprites;

    private PlayerStats stats;
    private int currentTileIndex = 0;
    private bool isMoving = false;

    // シングルトン
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        stats = PlayerStats.Instance;

        // 初期化処理
        if (relation != null) relation.Initialize();
        if (board != null) board.InitializeBoard(stats.currentGrade);
        if (shop != null) shop.SetupItems(stats.currentGrade);

        currentTileIndex = 0;
        StartCoroutine(InitPosition());

        if (ui != null) ui.UpdateDisplay(stats);
        if (diceButton != null) diceButton.onClick.AddListener(OnDiceClick);
    }

    IEnumerator InitPosition()
    {
        yield return null;
        if (board != null) board.MovePlayerPiece(0);
    }

    void OnDiceClick()
    {
        if (isMoving) return;
        StartCoroutine(TurnSequence());
    }

    // --- 1ターンの流れ ---
    IEnumerator TurnSequence()
    {
        isMoving = true;
        diceButton.interactable = false;

        // 1. サイコロを振る
        int steps = 0;
        yield return StartCoroutine(RollDice(val => steps = val));

        // 2. コマ移動
        yield return StartCoroutine(MovePiece(steps));

        // 3. マスごとのイベント実行 (※これが終わるまで待つ)
        yield return StartCoroutine(HandleTileEvent());

        // 4. ターン終了処理 (給料計算など)
        EndTurnProcess();

        isMoving = false;
        diceButton.interactable = true;
    }

    IEnumerator RollDice(System.Action<int> callback)
    {
        float t = 0;
        while (t < 0.5f)
        {
            if (diceImage && diceSprites.Length > 0)
                diceImage.sprite = diceSprites[Random.Range(0, diceSprites.Length)];
            yield return new WaitForSeconds(0.05f);
            t += 0.05f;
        }
        int res = Random.Range(1, 7);
        if (diceImage && diceSprites.Length > 0) diceImage.sprite = diceSprites[res - 1];
        callback(res);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator MovePiece(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            currentTileIndex++;

            // 進級・ゴール判定
            if (currentTileIndex >= board.totalTiles)
            {
                if (stats.currentGrade < 3)
                {
                    stats.currentGrade++;
                    currentTileIndex = 0;
                    ui.Log($"進級！ {stats.currentGrade}年生");
                    board.InitializeBoard(stats.currentGrade);
                    shop.SetupItems(stats.currentGrade);
                    yield return null;
                    board.MovePlayerPiece(0);
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }
                else
                {
                    currentTileIndex = board.totalTiles - 1;
                    board.MovePlayerPiece(currentTileIndex);
                    ui.Log("ゴール！");
                    break;
                }
            }

            board.MovePlayerPiece(currentTileIndex);
            if (ui) ui.UpdateDisplay(stats);

            // 購買部通過チェック
            if (board.BoardLayout[currentTileIndex] == "Shop" && i < steps - 1)
            {
                ui.Log("購買部通過");
                // 通過イベントも待機させる
                yield return StartCoroutine(shop.OpenShop(false));
                if (ui) ui.UpdateDisplay(stats);
            }

            // 中間地点チェック
            if (currentTileIndex == 24)
            {
                ui.Log("中間地点到着");
                break;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    // ★イベント処理
    IEnumerator HandleTileEvent()
    {
        string type = board.BoardLayout[currentTileIndex];

        // 親友効果の計算
        int mult = (relation.HasEffect(FriendEffectType.DoubleTileEffect) &&
                   (type.Contains("Plus") || type.Contains("Minus"))) ? 2 : 1;

        // 完了フラグ
        bool eventDone = false;

        switch (type)
        {
            // ▼ 選択肢が出るイベント (UI操作待ち) ▼
            case "Event":
                string[] eLabels = { "同伴(未)", "スカウト", "一人で遊ぶ" };
                bool[] eActive = { false, true, true };
                FriendData target = relation.CheckScoutableFriend(stats);
                if (target == null) eActive[1] = false;

                events.ShowChoices("イベント", eLabels, new UnityEngine.Events.UnityAction[] {
                    () => { ui.Log("同伴イベント(未実装)"); eventDone = true; },
                    // ★修正: target.friendName -> target.characterName
                    () => { relation.RecruitFriend(target); ui.Log($"スカウト成功: {target.characterName}"); eventDone = true; },
                    () => { StartCoroutine(WrapRoulette(() => eventDone = true)); } // ルーレット後に完了
                }, eActive);

                // ボタンが押されるまでここで待機
                while (!eventDone) yield return null;
                break;

            case "Male":
                string[] mLabels = { "情報", "友達になる", "会話 (GP+300)" };
                events.ShowChoices("男子生徒", mLabels, new UnityEngine.Events.UnityAction[] {
                    () => { ui.Log("男子から情報を聞いた"); eventDone = true; },
                    () => { stats.maleFriendCount++; ui.Log("男子と友達になった！"); eventDone = true; },
                    () => { stats.gp += 300; ui.Log("会話して300GPもらった"); eventDone = true; }
                });

                while (!eventDone) yield return null;
                break;

            case "Middle":
                string[] midLabels = { "GP+800", "親密度+40", "モブ昇格" };
                events.ShowChoices("中間地点", midLabels, new UnityEngine.Events.UnityAction[] {
                    () => { stats.gp += 800; ui.Log("臨時収入 +800GP"); eventDone = true; },
                    () => { stats.present += 40; ui.Log("親密度アップ +40"); eventDone = true; },
                    () => { ui.Log("モブ昇格(未実装)"); eventDone = true; }
                });

                while (!eventDone) yield return null;
                break;

            case "Shop":
                ui.Log("購買部(割引)");
                yield return StartCoroutine(shop.OpenShop(true));
                break;

            // ▼ 自動で進むイベント (ログを出して少し待つ) ▼
            case "GPPlus":
                int gVal = (stats.currentGrade * 150 + stats.galLv * 100) * mult;
                stats.gp += gVal;
                ui.Log($"バイト代が入った！ +{gVal} GP");
                yield return new WaitForSeconds(0.8f);
                break;

            case "GPMinus":
                if (!relation.HasEffect(FriendEffectType.NullifyGPMinus))
                {
                    int gLoss = (stats.currentGrade * 100) * mult;
                    stats.gp = Mathf.Max(0, stats.gp - gLoss);
                    ui.Log($"無駄遣いしてしまった... -{gLoss} GP");
                }
                else
                {
                    ui.Log("親友が奢ってくれた！ (出費回避)");
                }
                yield return new WaitForSeconds(0.8f);
                break;

            case "FriendPlus":
                int fAdd = (stats.currentGrade + stats.commuLv) * mult;
                stats.friends += fAdd;
                ui.Log($"友達が増えた！ +{fAdd}人");
                yield return new WaitForSeconds(0.8f);
                break;

            case "FriendMinus":
                if (stats.friends > 0)
                {
                    stats.friends -= 1 * mult;
                    ui.Log("友達と疎遠になった... -1人");
                }
                yield return new WaitForSeconds(0.8f);
                break;
        }
    }

    IEnumerator WrapRoulette(System.Action onComplete)
    {
        yield return StartCoroutine(events.PlayRoulette(stats, relation.HasEffect(FriendEffectType.BadEventToGP)));
        onComplete();
    }

    public void FinishTurn()
    {
    }

    void EndTurnProcess()
    {
        int shinyu = relation.GetRecruitedCount();
        stats.gp += stats.CalculateSalary(shinyu);
        stats.currentMonth++;
        if (stats.currentMonth > 12) stats.currentMonth -= 12;
        if (ui) ui.UpdateDisplay(stats);
    }
}