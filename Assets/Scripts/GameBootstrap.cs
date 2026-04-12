using UnityEngine;

/// <summary>
/// HTML5 prototype に忠実な構成でシーンを構築する。
/// 6ポケット・ペグ配置・ドラッグ壁・同配色。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBootstrap()
    {
        if (FindFirstObjectByType<GameBootstrap>() != null) return;
        new GameObject("[GameBootstrap]").AddComponent<GameBootstrap>();
    }

    // ── Arena (HTML5: 300×430 px → Unity: 6×8.6 u) ──────────
    const float HALF_W    = 3f;
    const float ARENA_TOP = 4.3f;
    const float POCKET_Y  = -4.3f;   // pocket centre
    const float POCKET_H  = 0.64f;   // 32/430 * 8.6
    const float ARENA_BOT = -5.3f;
    const int   NPOCKETS  = 6;

    // HTML5 pocket defs
    static readonly int[] PocketMults = { 4, 2, 6, 6, 2, 4 };
    static readonly Color[] PocketColors =
    {
        new(0.94f, 0.27f, 0.27f), // #ef4444 red
        new(0.98f, 0.60f, 0.09f), // #f97316 orange
        new(0.92f, 0.70f, 0.03f), // #eab308 yellow
        new(0.13f, 0.77f, 0.33f), // #22c55e green
        new(0.23f, 0.51f, 1.00f), // #3b82f6 blue
        new(0.66f, 0.33f, 0.97f), // #a855f7 purple
    };

    PhysicsMaterial2D _bounceMat;
    static Sprite _pixel;

    void Start()
    {
        _bounceMat = new PhysicsMaterial2D { bounciness = 0.62f, friction = 0.05f };

        ConfigureCamera();
        BuildArena();
        BuildPegs();
        var pockets = BuildPockets();

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        var ps = new GameObject("PlacementSystem").AddComponent<PlacementSystem>();
        ps.Setup(HALF_W, POCKET_Y + POCKET_H * 0.5f, ARENA_TOP, _bounceMat);

        var ui = new GameObject("UIController").AddComponent<UIController>();
        ui.Setup(gm, pockets, PocketColors, ps);

        gm.OnPhaseChanged += phase =>
        {
            if (phase == GameManager.Phase.Launching)
                SpawnBall();
        };

        gm.Initialize(pockets);
    }

    // ── Camera ───────────────────────────────────────────────
    void ConfigureCamera()
    {
        var cam = Camera.main;
        cam.orthographic    = true;
        cam.orthographicSize = 5.2f;
        cam.backgroundColor = new Color(0.051f, 0.067f, 0.090f); // #0d1117
        // Shift left so right side of screen is free for the UI panel
        cam.transform.position = new Vector3(-1.5f, 0f, -10f);
    }

    // ── Arena ────────────────────────────────────────────────
    void BuildArena()
    {
        float totalH = ARENA_TOP - ARENA_BOT;

        // Background
        MakeQuad("BG", new Vector3(0, (ARENA_TOP + ARENA_BOT) * 0.5f, 0),
            new Vector2(HALF_W * 2, totalH), new Color(0.086f, 0.106f, 0.133f), -10);

        // Boundary walls
        MakeWall("LeftWall",  new Vector3(-HALF_W - 0.1f, 0, 0), new Vector2(0.2f, totalH + 1f));
        MakeWall("RightWall", new Vector3( HALF_W + 0.1f, 0, 0), new Vector2(0.2f, totalH + 1f));
        MakeWall("TopWall",   new Vector3(0, ARENA_TOP + 0.1f, 0), new Vector2(HALF_W * 2 + 0.4f, 0.2f));
        MakeWall("Floor",     new Vector3(0, ARENA_BOT, 0), new Vector2(HALF_W * 2 + 0.4f, 0.2f));

        // Pocket separators
        float pw = HALF_W * 2f / NPOCKETS;
        for (int i = 0; i <= NPOCKETS; i++)
        {
            float x = -HALF_W + pw * i;
            var sep = new GameObject($"Sep_{i}");
            sep.transform.position = new Vector3(x, POCKET_Y, 0);
            var bc = sep.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(0.06f, POCKET_H + 0.2f);
            bc.sharedMaterial = _bounceMat;
            MakeQuad($"Sep_{i}_V", sep.transform.position,
                new Vector2(0.06f, POCKET_H + 0.2f), new Color(0.19f, 0.21f, 0.24f), 2);
        }

        // Launch guide (dashed look — single semi-transparent quad)
        MakeQuad("LaunchGuide",
            new Vector3(0, ARENA_TOP - 1.4f, 0),
            new Vector2(0.04f, 2.4f),
            new Color(1f, 1f, 1f, 0.12f), 2);
    }

    // ── Pegs (staggered Plinko, HTML5 proportions) ────────────
    void BuildPegs()
    {
        float arenaH  = ARENA_TOP - (POCKET_Y - POCKET_H * 0.5f); // ≈ 8.94
        float firstY  = ARENA_TOP - 0.244f * arenaH;
        float rowStep = 0.135f * arenaH;
        float pw      = HALF_W * 2f / NPOCKETS; // = 1 unit
        float pegR    = 0.15f;

        var pegMat = new PhysicsMaterial2D { bounciness = 0.62f, friction = 0f };

        for (int row = 0; row < 5; row++)
        {
            bool even = (row % 2 == 0);
            int  cols = even ? 5 : 4;
            float y   = firstY - row * rowStep;

            for (int col = 0; col < cols; col++)
            {
                float x = even
                    ? -HALF_W + pw * 0.5f + col * pw   // 5 pegs: half-pocket offset
                    : -HALF_W + pw         + col * pw;  // 4 pegs: one-pocket offset

                var peg = new GameObject($"Peg_{row}_{col}");
                peg.transform.position = new Vector3(x, y, 0);

                var cc = peg.AddComponent<CircleCollider2D>();
                cc.radius = pegR;
                cc.sharedMaterial = pegMat;

                // Visual (circle sprite)
                var vis = new GameObject("V");
                vis.transform.SetParent(peg.transform, false);
                vis.transform.localScale = Vector3.one * (pegR * 2f);
                var sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = MakeCircleSprite(32);
                sr.color  = new Color(0.122f, 0.435f, 0.922f); // #1f6feb
                sr.sortingOrder = 2;
            }
        }
    }

    // ── Pockets ──────────────────────────────────────────────
    Pocket[] BuildPockets()
    {
        float pw   = HALF_W * 2f / NPOCKETS;
        var   list = new Pocket[NPOCKETS];

        for (int i = 0; i < NPOCKETS; i++)
        {
            float cx = -HALF_W + pw * 0.5f + pw * i;

            var pObj = new GameObject($"Pocket_{i}");
            pObj.transform.position = new Vector3(cx, POCKET_Y, 0);

            // Single trigger: ball detection & OnMouseDown both work on triggers
            var trig = pObj.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            trig.size = new Vector2(pw - 0.08f, POCKET_H);

            // Background visual (dim by default)
            var vis = MakeQuad($"PocketVis_{i}",
                new Vector3(cx, POCKET_Y, 0),
                new Vector2(pw - 0.1f, POCKET_H - 0.02f),
                PocketColors[i] * 0.25f, 1);

            var pocket = pObj.AddComponent<Pocket>();
            pocket.Initialize(i, PocketMults[i], PocketColors[i],
                vis.GetComponent<SpriteRenderer>());
            list[i] = pocket;
        }
        return list;
    }

    // ── Ball ─────────────────────────────────────────────────
    void SpawnBall()
    {
        // Slight random x offset, same as HTML5
        float rx = (Random.value - 0.5f) * 0.67f;
        var ball = new GameObject("Ball");
        ball.transform.position = new Vector3(rx, ARENA_TOP - 0.5f, 0);
        ball.AddComponent<Rigidbody2D>();
        ball.AddComponent<CircleCollider2D>();
        ball.AddComponent<Ball>();
    }

    // ── Helpers ──────────────────────────────────────────────
    void MakeWall(string name, Vector3 pos, Vector2 size, Color? col = null)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        var bc = obj.AddComponent<BoxCollider2D>();
        bc.size = size;
        bc.sharedMaterial = _bounceMat;
        MakeQuad(name + "_V", pos, size, col ?? new Color(0.19f, 0.21f, 0.24f), 1);
    }

    static GameObject MakeQuad(string name, Vector3 pos, Vector2 size, Color color, int order)
    {
        var obj = new GameObject(name);
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetPixelSprite();
        sr.color        = color;
        sr.sortingOrder = order;
        return obj;
    }

    static Sprite GetPixelSprite()
    {
        if (_pixel != null) return _pixel;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        return _pixel;
    }

    static Sprite MakeCircleSprite(int res)
    {
        var   tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
        float r   = res * 0.5f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float a  = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);
    }
}
