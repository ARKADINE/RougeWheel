using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    GameManager _gm;
    Pocket[] _pockets;

    Text _coinsText;
    Text _phaseText;
    Text _infoText;

    GameObject _betPanel;
    GameObject _placePanel;
    GameObject _resultPanel;

    Text _placementsText;
    Text _resultText;

    Font _font;

    public void Setup(GameManager gm, Pocket[] pockets, PlacementSystem ps)
    {
        _gm = gm;
        _pockets = pockets;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildCanvas();

        gm.OnPhaseChanged += OnPhaseChanged;
        gm.OnCoinsChanged += OnCoinsChanged;
        gm.OnResult += OnResult;
    }

    void BuildCanvas()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Top bar background
        var topBar = MakePanel(canvasObj, new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(0, -60), new Vector2(0, 0), new Color(0, 0, 0, 0.6f));
        topBar.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        topBar.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        topBar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
        topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
        topBar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        _coinsText = MakeText(topBar, "Coins: 100", new Vector2(-300, 0), new Vector2(200, 40), 22, TextAnchor.MiddleLeft);
        _phaseText = MakeText(topBar, "BETTING", new Vector2(0, 0), new Vector2(300, 40), 24, TextAnchor.MiddleCenter);
        _infoText = MakeText(topBar, "", new Vector2(200, 0), new Vector2(350, 40), 18, TextAnchor.MiddleRight);

        // Betting panel (bottom)
        _betPanel = MakePanel(canvasObj, new Vector2(0, 0), new Vector2(0, 0),
            Vector2.zero, new Vector2(500, 80), new Color(0, 0, 0, 0.7f));
        var bpRt = _betPanel.GetComponent<RectTransform>();
        bpRt.anchorMin = new Vector2(0.5f, 0);
        bpRt.anchorMax = new Vector2(0.5f, 0);
        bpRt.pivot = new Vector2(0.5f, 0);
        bpRt.anchoredPosition = new Vector2(0, 10);
        bpRt.sizeDelta = new Vector2(500, 70);

        MakeText(_betPanel, "← Click a pocket to select. Scroll = rotate wall preview.", new Vector2(-60, 15), new Vector2(400, 30), 16, TextAnchor.MiddleLeft);
        MakeButton(_betPanel, "Bet 10 Coins", new Vector2(200, 0), new Vector2(130, 50), () => _gm.ConfirmBet());

        // Placing panel (bottom)
        _placePanel = MakePanel(canvasObj, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.7f));
        var ppRt = _placePanel.GetComponent<RectTransform>();
        ppRt.anchorMin = new Vector2(0.5f, 0);
        ppRt.anchorMax = new Vector2(0.5f, 0);
        ppRt.pivot = new Vector2(0.5f, 0);
        ppRt.anchoredPosition = new Vector2(0, 10);
        ppRt.sizeDelta = new Vector2(560, 70);

        _placementsText = MakeText(_placePanel, "Placements: 3", new Vector2(-140, 15), new Vector2(280, 30), 18, TextAnchor.MiddleLeft);
        MakeText(_placePanel, "Click arena to place wall  |  Scroll/R to rotate", new Vector2(-80, -12), new Vector2(340, 25), 14, TextAnchor.MiddleLeft);
        MakeButton(_placePanel, "Launch!", new Vector2(220, 0), new Vector2(110, 50), () => _gm.LaunchBall());

        // Result panel (center)
        _resultPanel = MakePanel(canvasObj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 200), new Color(0.05f, 0.05f, 0.05f, 0.9f));
        _resultText = MakeText(_resultPanel, "", Vector2.zero, new Vector2(360, 100), 30, TextAnchor.MiddleCenter);
        _resultText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
        MakeButton(_resultPanel, "Next Round", new Vector2(0, -55), new Vector2(160, 50), () => _gm.NextRound());

        _betPanel.SetActive(false);
        _placePanel.SetActive(false);
        _resultPanel.SetActive(false);
    }

    void OnPhaseChanged(GameManager.Phase phase)
    {
        _betPanel.SetActive(phase == GameManager.Phase.Betting);
        _placePanel.SetActive(phase == GameManager.Phase.Placing);
        _resultPanel.SetActive(phase == GameManager.Phase.Result);

        _phaseText.text = phase.ToString().ToUpper();

        if (phase == GameManager.Phase.Betting)
        {
            foreach (var p in _pockets)
                p.SetSelected(false);
            _infoText.text = "";
        }
        else if (phase == GameManager.Phase.Betting || _gm.SelectedPocket >= 0)
        {
            // Update selection highlight
            for (int i = 0; i < _pockets.Length; i++)
                _pockets[i].SetSelected(i == _gm.SelectedPocket);

            if (_gm.SelectedPocket >= 0)
            {
                var sp = _pockets[_gm.SelectedPocket];
                _infoText.text = $"Pocket {sp.Index}  ×{sp.Multiplier}";
            }
        }
        else if (phase == GameManager.Phase.Placing)
        {
            _placementsText.text = $"Placements: {_gm.PlacementsRemaining}";
        }

        if (phase == GameManager.Phase.Placing)
            _placementsText.text = $"Placements: {_gm.PlacementsRemaining}";
    }

    void OnCoinsChanged(int coins)
    {
        _coinsText.text = $"Coins: {coins}";
    }

    void OnResult(bool won, int pocket, int gain)
    {
        if (won)
        {
            _resultText.text = $"WIN!\n+{gain} coins";
            _resultText.color = new Color(1f, 0.9f, 0.2f);
        }
        else
        {
            _resultText.text = $"LOSE\nPocket {pocket}";
            _resultText.color = new Color(1f, 0.3f, 0.3f);
        }
    }

    // ── Helpers ────────────────────────────────────────────

    GameObject MakePanel(GameObject parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color bg)
    {
        var obj = new GameObject("Panel");
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = bg;
        return obj;
    }

    Text MakeText(GameObject parent, string content, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.font = _font;
        return t;
    }

    void MakeButton(GameObject parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(parent.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.5f, 0.15f);
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.7f, 0.25f);
        colors.pressedColor = new Color(0.1f, 0.35f, 0.1f);
        btn.colors = colors;

        MakeText(obj, label, Vector2.zero, size, 16, TextAnchor.MiddleCenter);
    }
}
