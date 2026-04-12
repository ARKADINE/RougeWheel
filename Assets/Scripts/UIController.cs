using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// HTML5 のサイドパネル UI を Unity Canvas で再現。
/// 画面右端 200px パネル。フェーズ毎にセクション切り替え。
/// </summary>
public class UIController : MonoBehaviour
{
    GameManager _gm;
    Pocket[]    _pockets;
    Color[]     _pocketColors;

    Text   _coinsText, _phaseText, _hintText;
    Text   _betAmtText, _wallCountText, _resultText;

    GameObject _betSection, _placeSection, _resultSection;
    Button[]   _pocketBtns;
    Button     _confirmBtn, _nextBtn;

    int _betAmount       = 10;
    int _resultPocketIdx = -1;

    Font _font;

    // ── Colors (HTML5 palette) ─────────────────────────────────
    static readonly Color BG_DARK  = new(0.051f, 0.067f, 0.090f);  // #0d1117
    static readonly Color BG_PANEL = new(0.086f, 0.106f, 0.133f);  // #161b22
    static readonly Color C_RED    = new(0.914f, 0.271f, 0.376f);  // #e94560
    static readonly Color C_GOLD   = new(0.941f, 0.753f, 0.251f);  // #f0c040
    static readonly Color C_GREY   = new(0.545f, 0.580f, 0.620f);  // #8b949e
    static readonly Color C_DIM    = new(0.283f, 0.310f, 0.345f);  // #484f58
    static readonly Color C_LIGHT  = new(0.902f, 0.929f, 0.953f);  // #e6edf3
    static readonly Color C_GREEN  = new(0.270f, 0.773f, 0.369f);  // #4ade80
    static readonly Color C_WARN   = new(0.973f, 0.529f, 0.431f);  // #f87171
    static readonly Color C_BORDER = new(0.188f, 0.212f, 0.239f);  // #30363d

    public void Setup(GameManager gm, Pocket[] pockets, Color[] pocketColors, PlacementSystem ps)
    {
        _gm          = gm;
        _pockets     = pockets;
        _pocketColors = pocketColors;
        _font        = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureEventSystem();
        BuildCanvas();

        gm.OnPhaseChanged += OnPhaseChanged;
        gm.OnCoinsChanged += OnCoinsChanged;
        gm.OnResult       += OnResult;
    }

    // ── Canvas & panel ────────────────────────────────────────
    void BuildCanvas()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Right-side panel ───────────────────────────────────
        // anchorMin=(1,0), anchorMax=(1,1): full-height strip on right
        var panel = NewGO("Panel", canvasObj);
        var pRt   = SetRT(panel, new Vector2(1,0), new Vector2(1,1),
                         new Vector2(1,0.5f), new Vector2(0,0), new Vector2(200,0));
        NewImg(panel, BG_PANEL);

        // Left edge border
        var border = NewGO("Border", panel);
        SetRT(border, new Vector2(0,0), new Vector2(0,1),
              new Vector2(0,0.5f), new Vector2(0,0), new Vector2(1,0));
        NewImg(border, C_BORDER);

        // ── Fixed header (title, coins, phase badge) ───────────
        // Title
        NewTxt(panel, "RougeWheel",
               Anc(0.5f,1,0.5f,1), new Vector2(0,-20), new Vector2(184,26),
               20, C_RED, FontStyle.Bold, TextAnchor.MiddleCenter);
        // Subtitle
        NewTxt(panel, "prototype v0.1",
               Anc(0.5f,1,0.5f,1), new Vector2(0,-44), new Vector2(184,14),
               10, C_DIM, FontStyle.Normal, TextAnchor.MiddleCenter);

        // Coins row box (top=58)
        var coinsBox = NewBox(panel, Anc(0.5f,1,0.5f,1), new Vector2(0,-72), new Vector2(178,34), BG_DARK);
        AddOutline(coinsBox, C_BORDER);
        NewTxt(coinsBox, "🪙  COINS",
               Anc(0f,0.5f,0f,0.5f), new Vector2(8,0), new Vector2(90,30),
               11, C_GREY, TextAnchor.MiddleLeft);
        _coinsText = NewTxt(coinsBox, "100",
               Anc(1f,0.5f,1f,0.5f), new Vector2(-8,0), new Vector2(60,30),
               20, C_GOLD, FontStyle.Bold, TextAnchor.MiddleRight);

        // Phase badge (top=100)
        var badge = NewBox(panel, Anc(0.5f,1,0.5f,1), new Vector2(0,-112), new Vector2(178,22),
                          new Color(0.122f,0.161f,0.216f));
        AddOutline(badge, new Color(0.231f,0.259f,0.306f));
        _phaseText = NewTxt(badge, "PHASE: BET",
               Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero, new Vector2(178,22),
               11, C_GREY, FontStyle.Normal, TextAnchor.MiddleCenter);

        // ── Content sections (top = 140px from panel top) ──────
        // All three sections share the same top anchor position.
        // They use pivot=(0.5,1) so anchoredPosition.y = -140 aligns their TOP.
        float sectY = -140f;

        BuildBetSection(panel, sectY);
        BuildPlaceSection(panel, sectY);
        BuildResultSection(panel, sectY);

        // ── Hint text (pinned to bottom) ───────────────────────
        var hintBox = NewGO("HintBox", panel);
        SetRT(hintBox, new Vector2(0,0), new Vector2(1,0),
              new Vector2(0.5f,0), new Vector2(0,8), new Vector2(-16,72));
        var hintLine = NewGO("HintTop", hintBox);
        SetRT(hintLine, new Vector2(0,1), new Vector2(1,1),
              new Vector2(0.5f,1), Vector2.zero, new Vector2(0,1));
        NewImg(hintLine, new Color(0.13f,0.15f,0.18f));
        _hintText = NewTxt(hintBox, "ポケットを選んでベットしよう！",
               Anc(0.5f,0.5f,0.5f,0.5f), new Vector2(2,-2), new Vector2(174,68),
               10, C_DIM, FontStyle.Normal, TextAnchor.UpperLeft);

        _betSection.SetActive(false);
        _placeSection.SetActive(false);
        _resultSection.SetActive(false);
    }

    // ── BET section ───────────────────────────────────────────
    void BuildBetSection(GameObject panel, float y)
    {
        // Container: pivot=(0.5,1) so top aligns with y
        _betSection = NewGO("BetSection", panel);
        SetRT(_betSection, Anc(0.5f,1,0.5f,1).min, Anc(0.5f,1,0.5f,1).max,
              new Vector2(0.5f,1), new Vector2(0,y), new Vector2(184,315));

        // "ポケット選択" label (y from top: 4)
        NewTxt(_betSection, "ポケット選択",
               Anc(0f,1,0f,1), new Vector2(4,-4), new Vector2(176,14),
               10, C_GREY);

        // Pocket grid (6 buttons in 2 rows × 3 cols)
        _pocketBtns = new Button[_pockets.Length];
        float bw = 56f, bh = 40f, gap = 4f;
        float gridW = bw * 3 + gap * 2; // 176
        for (int i = 0; i < _pockets.Length; i++)
        {
            int   col = i % 3, row = i / 3;
            float bx  = -gridW * 0.5f + bw * 0.5f + col * (bw + gap);
            float by  = -22f - row * (bh + gap) - bh * 0.5f; // top anchor in parent
            Color pc  = _pocketColors[i];
            int   cap = i;

            var btn = NewBox(_betSection, Anc(0.5f,1,0.5f,1),
                            new Vector2(bx, by - bh*0.5f), new Vector2(bw, bh),
                            new Color(pc.r, pc.g, pc.b, 0.12f));
            AddOutline(btn, new Color(pc.r,pc.g,pc.b,0.45f));
            var b = btn.AddComponent<Button>();
            b.targetGraphic = btn.GetComponent<Image>();
            b.onClick.AddListener(() => OnPocketBtnClick(cap));

            NewTxt(btn, $"{i+1}",
                   Anc(0.5f,1,0.5f,1), new Vector2(0,-5), new Vector2(bw,18),
                   12, C_LIGHT, FontStyle.Bold, TextAnchor.MiddleCenter);
            NewTxt(btn, $"×{_pockets[i].Multiplier}",
                   Anc(0.5f,0,0.5f,0), new Vector2(0,6), new Vector2(bw,14),
                   9, C_GREY, FontStyle.Normal, TextAnchor.MiddleCenter);

            _pocketBtns[i] = b;
        }

        // Bet amount row (top: 106)
        NewTxt(_betSection, "ベット額",
               Anc(0f,1,0f,1), new Vector2(4,-108), new Vector2(80,14),
               10, C_GREY);
        _betAmtText = NewTxt(_betSection, "10",
               Anc(1f,1,1f,1), new Vector2(-4,-108), new Vector2(50,14),
               14, C_GOLD, FontStyle.Bold, TextAnchor.MiddleRight);

        // − / + buttons
        var minusBox = NewBox(_betSection, Anc(0f,1,0f,1), new Vector2(4,-132),
                             new Vector2(38,26), new Color(0.13f,0.15f,0.18f));
        AddOutline(minusBox, C_BORDER);
        var mb = minusBox.AddComponent<Button>(); mb.targetGraphic = minusBox.GetComponent<Image>();
        mb.onClick.AddListener(OnBetMinus);
        NewTxt(minusBox, "−", Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero,
               new Vector2(38,26), 16, C_LIGHT, FontStyle.Bold, TextAnchor.MiddleCenter);

        var plusBox = NewBox(_betSection, Anc(1f,1,1f,1), new Vector2(-4,-132),
                            new Vector2(38,26), new Color(0.13f,0.15f,0.18f));
        AddOutline(plusBox, C_BORDER);
        var pb = plusBox.AddComponent<Button>(); pb.targetGraphic = plusBox.GetComponent<Image>();
        pb.onClick.AddListener(OnBetPlus);
        NewTxt(plusBox, "+", Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero,
               new Vector2(38,26), 16, C_LIGHT, FontStyle.Bold, TextAnchor.MiddleCenter);

        // Confirm button (top: 170)
        var confBox = NewBox(_betSection, Anc(0.5f,1,0.5f,1), new Vector2(0,-175),
                            new Vector2(176,36), new Color(C_RED.r,C_RED.g,C_RED.b,0.35f));
        _confirmBtn = confBox.AddComponent<Button>();
        _confirmBtn.targetGraphic = confBox.GetComponent<Image>();
        _confirmBtn.interactable  = false;
        _confirmBtn.onClick.AddListener(() => _gm.ConfirmBet());
        NewTxt(confBox, "ベット確定", Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero,
               new Vector2(176,36), 13, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);

        var cColors = _confirmBtn.colors;
        cColors.normalColor      = new Color(C_RED.r, C_RED.g, C_RED.b, 0.35f);
        cColors.highlightedColor = C_RED;
        cColors.pressedColor     = new Color(C_RED.r*0.8f, C_RED.g*0.8f, C_RED.b*0.8f);
        cColors.disabledColor    = new Color(C_RED.r, C_RED.g, C_RED.b, 0.25f);
        _confirmBtn.colors = cColors;
    }

    // ── PLACE section ─────────────────────────────────────────
    void BuildPlaceSection(GameObject panel, float y)
    {
        _placeSection = NewGO("PlaceSection", panel);
        SetRT(_placeSection, Anc(0.5f,1,0.5f,1).min, Anc(0.5f,1,0.5f,1).max,
              new Vector2(0.5f,1), new Vector2(0,y), new Vector2(184,230));

        NewTxt(_placeSection, "壁を配置",
               Anc(0f,1,0f,1), new Vector2(4,-4), new Vector2(176,14), 10, C_GREY);

        // Info box (top: 22)
        var infoBox = NewBox(_placeSection, Anc(0.5f,1,0.5f,1), new Vector2(0,-56),
                            new Vector2(178,68), BG_DARK);
        AddOutline(infoBox, C_BORDER);
        _wallCountText = NewTxt(infoBox, "配置: 0 / 3 本\nドラッグ: 壁を引く\n右クリック: 最後の壁を削除",
               Anc(0.5f,0.5f,0.5f,0.5f), new Vector2(4,0), new Vector2(164,60),
               11, C_GREY, FontStyle.Normal, TextAnchor.UpperLeft);

        // Launch button (top: 100)
        var launchBox = NewBox(_placeSection, Anc(0.5f,1,0.5f,1), new Vector2(0,-118),
                              new Vector2(176,36), C_RED);
        var lb = launchBox.AddComponent<Button>(); lb.targetGraphic = launchBox.GetComponent<Image>();
        lb.onClick.AddListener(() => _gm.LaunchBall());
        NewTxt(launchBox, "発射！", Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero,
               new Vector2(176,36), 14, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);

        // Clear button (top: 144)
        var clearBox = NewBox(_placeSection, Anc(0.5f,1,0.5f,1), new Vector2(0,-160),
                             new Vector2(176,32), new Color(0.13f,0.15f,0.18f));
        AddOutline(clearBox, C_BORDER);
        var cb = clearBox.AddComponent<Button>(); cb.targetGraphic = clearBox.GetComponent<Image>();
        cb.onClick.AddListener(() => _gm.ClearAllPlaced());
        NewTxt(clearBox, "壁リセット", Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero,
               new Vector2(176,32), 12, C_GREY, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    // ── RESULT section ────────────────────────────────────────
    void BuildResultSection(GameObject panel, float y)
    {
        _resultSection = NewGO("ResultSection", panel);
        SetRT(_resultSection, Anc(0.5f,1,0.5f,1).min, Anc(0.5f,1,0.5f,1).max,
              new Vector2(0.5f,1), new Vector2(0,y), new Vector2(184,200));

        var resBox = NewBox(_resultSection, Anc(0.5f,1,0.5f,1), new Vector2(0,-48),
                           new Vector2(178,90), BG_DARK);
        AddOutline(resBox, C_BORDER);
        _resultText = NewTxt(resBox, "",
               Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero, new Vector2(164,84),
               13, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter);

        var nextBox = NewBox(_resultSection, Anc(0.5f,1,0.5f,1), new Vector2(0,-106),
                            new Vector2(176,36), C_RED);
        _nextBtn = nextBox.AddComponent<Button>(); _nextBtn.targetGraphic = nextBox.GetComponent<Image>();
        _nextBtn.onClick.AddListener(() => _gm.NextRound());
        NewTxt(nextBox, "次のラウンド", Anc(0.5f,0.5f,0.5f,0.5f), Vector2.zero,
               new Vector2(176,36), 13, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    // ── Phase / Coins / Result callbacks ─────────────────────
    void OnPhaseChanged(GameManager.Phase phase)
    {
        _betSection.SetActive(phase == GameManager.Phase.Betting);
        _placeSection.SetActive(phase == GameManager.Phase.Placing);
        _resultSection.SetActive(phase == GameManager.Phase.Result);

        _phaseText.text  = phase switch
        {
            GameManager.Phase.Betting  => "PHASE: BET",
            GameManager.Phase.Placing  => "PHASE: PLACE",
            GameManager.Phase.Launching => "PHASE: LAUNCH",
            GameManager.Phase.Result   => "PHASE: RESULT",
            _                          => ""
        };
        _phaseText.color = phase == GameManager.Phase.Betting ? C_GREY : C_RED;

        switch (phase)
        {
            case GameManager.Phase.Betting:
                RefreshPocketButtons();
                bool canBet = _gm.SelectedPocket >= 0;
                _confirmBtn.interactable = canBet;
                // Update confirm btn image color manually (interactable fade is slow)
                var img = _confirmBtn.GetComponent<Image>();
                if (img) img.color = canBet ? C_RED : new Color(C_RED.r,C_RED.g,C_RED.b,0.35f);

                // Clear result highlights
                if (_resultPocketIdx >= 0)
                {
                    foreach (var p in _pockets) p.SetResult(false);
                    _resultPocketIdx = -1;
                }
                SetHint(_gm.SelectedPocket >= 0
                    ? $"ポケット {_gm.SelectedPocket+1}（×{_pockets[_gm.SelectedPocket].Multiplier}）に賭ける。確定ボタンを押そう！"
                    : "ポケットを選んでベットしよう！");
                break;

            case GameManager.Phase.Placing:
                RefreshWallCount();
                SetHint("ドラッグして壁を配置しよう（最大3本）。準備できたら発射！");
                break;

            case GameManager.Phase.Launching:
                SetHint("物理演算で落下中...");
                break;
        }
    }

    void OnCoinsChanged(int coins) => _coinsText.text = coins.ToString();

    void OnResult(bool won, int pocketIdx, int gain)
    {
        _resultPocketIdx = pocketIdx;

        foreach (var p in _pockets) p.SetResult(false);
        _pockets[pocketIdx].SetResult(true);

        if (won)
        {
            _resultText.text  = $"🎉 当たり！\nポケット {pocketIdx+1}（×{_pockets[pocketIdx].Multiplier}）\n+{gain} コイン！";
            _resultText.color = C_GREEN;
            SetHint($"🎊 {gain}コイン獲得！残高: {_gm.Coins}");
        }
        else
        {
            _resultText.text = $"😢 ハズレ...\nボール: ポケット {pocketIdx+1}\nベット: ポケット {_gm.SelectedPocket+1}\n−{_gm.BetAmount} コイン";
            _resultText.color = C_WARN;
            SetHint(_gm.Coins <= 0 ? "ゲームオーバー！リスタートしよう。" : $"残高: {_gm.Coins} コイン");
        }

        var nextText = _nextBtn.GetComponentInChildren<Text>();
        if (nextText) nextText.text = _gm.Coins <= 0 ? "リスタート" : "次のラウンド";
    }

    // ── Pocket button clicks ──────────────────────────────────
    void OnPocketBtnClick(int idx) => _gm.SelectPocket(idx);

    void RefreshPocketButtons()
    {
        for (int i = 0; i < _pocketBtns.Length; i++)
        {
            bool sel = i == _gm.SelectedPocket;
            Color pc = _pocketColors[i];
            var img  = _pocketBtns[i].GetComponent<Image>();
            if (img) img.color = new Color(pc.r, pc.g, pc.b, sel ? 0.35f : 0.12f);
            // Border outline color
            var outline = _pocketBtns[i].GetComponent<Outline>();
            if (outline) outline.effectColor = new Color(pc.r, pc.g, pc.b, sel ? 1f : 0.45f);
        }
    }

    // ── Bet amount +/− ────────────────────────────────────────
    void OnBetMinus()
    {
        _betAmount = Mathf.Max(5, _betAmount - 5);
        _gm.SetBetAmount(_betAmount);
        _betAmtText.text = _betAmount.ToString();
    }

    void OnBetPlus()
    {
        _betAmount = Mathf.Min(50, _betAmount + 5);
        _gm.SetBetAmount(_betAmount);
        _betAmtText.text = _betAmount.ToString();
    }

    void RefreshWallCount()
    {
        int placed = GameManager.MaxPlacements - _gm.PlacementsRemaining;
        if (_wallCountText)
            _wallCountText.text =
                $"配置: {placed} / {GameManager.MaxPlacements} 本\n" +
                "ドラッグ: 壁を引く\n右クリック: 最後の壁を削除";
    }

    void SetHint(string msg) { if (_hintText) _hintText.text = msg; }

    // ── UI builder helpers ────────────────────────────────────

    // Anchor shorthand: returns (anchorMin, anchorMax) with matching pivot
    static (Vector2 min, Vector2 max) Anc(float minX, float minY, float maxX, float maxY) =>
        (new Vector2(minX, minY), new Vector2(maxX, maxY));

    static GameObject NewGO(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static Image NewImg(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    // Pivot defaults to the anchor point (same as min==max)
    static RectTransform SetRT(GameObject go, Vector2 ancMin, Vector2 ancMax,
        Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = ancMin;
        rt.anchorMax        = ancMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return rt;
    }

    /// <summary>Box at anchor point anchor, pivot=(0.5,0.5), with Image.</summary>
    GameObject NewBox(GameObject parent, (Vector2 min, Vector2 max) anc,
        Vector2 pos, Vector2 size, Color color)
    {
        // For top-anchor (anc.min.y == 1), use pivot (0.5,1) so pos.y = top edge offset.
        float pivY = anc.min.y >= 1f ? 1f : (anc.min.y <= 0f ? 0f : 0.5f);
        var go = NewGO("Box", parent);
        SetRT(go, anc.min, anc.max, new Vector2(0.5f, pivY), pos, size);
        NewImg(go, color);
        return go;
    }

    Text NewTxt(GameObject parent, string content, (Vector2 min, Vector2 max) anc,
        Vector2 pos, Vector2 size, int fs, Color color,
        FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft)
    {
        float pivY = anc.min.y >= 1f ? 1f : (anc.min.y <= 0f ? 0f : 0.5f);
        float pivX = anc.min.x >= 1f ? 1f : (anc.min.x <= 0f ? 0f : 0.5f);
        var go = NewGO("Txt", parent);
        SetRT(go, anc.min, anc.max, new Vector2(pivX, pivY), pos, size);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = fs;
        t.color     = color;
        t.font      = _font;
        t.fontStyle = style;
        t.alignment = align;
        return t;
    }

    static void AddOutline(GameObject go, Color color)
    {
        var ol = go.AddComponent<Outline>();
        ol.effectColor    = color;
        ol.effectDistance = new Vector2(1f, -1f);
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}
