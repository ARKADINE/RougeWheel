using UnityEngine;

/// <summary>
/// Programmatically builds the entire game scene at runtime.
/// Uses [RuntimeInitializeOnLoadMethod] so it works even if the scene
/// reference is broken — no manual "Attach script" needed.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    // ── Self-bootstrap (runs regardless of scene GUID) ───────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBootstrap()
    {
        // If the scene already has a working GameBootstrap, Start() will
        // handle it. Otherwise, create one now.
        if (FindFirstObjectByType<GameBootstrap>() != null) return;
        new GameObject("[GameBootstrap]").AddComponent<GameBootstrap>();
    }

    // ── Arena constants ──────────────────────────────────────
    const float HALF_W    = 4f;     // half arena width  (total 8 units)
    const float ARENA_TOP = 6.5f;
    const float ARENA_BOT = -7f;
    const float POCKET_Y  = -6.3f;
    const int   POCKETS   = 8;

    static readonly int[]   Multipliers = { 2, 5, 1, 3, 3, 1, 5, 2 };

    PhysicsMaterial2D _wallMat;
    static Sprite _pixel;

    // ── Entry point ──────────────────────────────────────────
    void Start()
    {
        _wallMat = new PhysicsMaterial2D { bounciness = 0.45f, friction = 0.1f };

        ConfigureCamera();
        BuildArena();
        var pockets = BuildPockets();

        // Managers
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        var ps = new GameObject("PlacementSystem").AddComponent<PlacementSystem>();
        ps.Setup(HALF_W, POCKET_Y, ARENA_TOP);
        var ui = new GameObject("UIController").AddComponent<UIController>();
        ui.Setup(gm, pockets, ps);

        // Wire launch → ball spawn
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
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.backgroundColor = new Color(0.02f, 0.06f, 0.02f);
        cam.transform.position = new Vector3(0, -0.5f, -10);
    }

    // ── Arena ────────────────────────────────────────────────
    void BuildArena()
    {
        // Dark green background
        MakeQuad("Background", Vector3.zero, new Vector2(HALF_W * 2 - 0.2f, 14.5f),
            new Color(0.04f, 0.12f, 0.04f), -10);

        // Boundary walls (physics + visual)
        MakeWall("LeftWall",  new Vector3(-HALF_W, -0.5f, 0), new Vector2(0.2f, 14.5f), _wallMat);
        MakeWall("RightWall", new Vector3( HALF_W, -0.5f, 0), new Vector2(0.2f, 14.5f), _wallMat);
        MakeWall("TopWall",   new Vector3(0, ARENA_TOP,   0), new Vector2(HALF_W * 2, 0.2f), _wallMat);

        // Pocket separators
        float pw = HALF_W * 2f / POCKETS;
        for (int i = 0; i <= POCKETS; i++)
        {
            float x = -HALF_W + pw * i;
            MakeWall($"Sep_{i}", new Vector3(x, POCKET_Y - 0.2f, 0), new Vector2(0.1f, 0.6f), _wallMat,
                new Color(0.55f, 0.55f, 0.55f));
        }

        // Invisible floor (catches stray balls)
        MakeWall("Floor", new Vector3(0, ARENA_BOT, 0), new Vector2(HALF_W * 2, 0.2f), _wallMat,
            new Color(0.1f, 0.1f, 0.1f));

        // Launch guide lines
        MakeQuad("Guide_L", new Vector3(-0.5f, (ARENA_TOP + POCKET_Y) * 0.5f, 0),
            new Vector2(0.03f, ARENA_TOP - POCKET_Y), new Color(0.3f, 0.5f, 0.3f, 0.4f), 2);
        MakeQuad("Guide_R", new Vector3( 0.5f, (ARENA_TOP + POCKET_Y) * 0.5f, 0),
            new Vector2(0.03f, ARENA_TOP - POCKET_Y), new Color(0.3f, 0.5f, 0.3f, 0.4f), 2);
    }

    // ── Pockets ──────────────────────────────────────────────
    Pocket[] BuildPockets()
    {
        float pw = HALF_W * 2f / POCKETS;
        var list = new Pocket[POCKETS];

        for (int i = 0; i < POCKETS; i++)
        {
            float x = -HALF_W + pw * 0.5f + pw * i;
            bool isRed = (i % 2 == 0);
            Color col = isRed ? new Color(0.75f, 0.1f, 0.1f) : new Color(0.15f, 0.15f, 0.15f);

            var pObj = new GameObject($"Pocket_{i}");
            pObj.transform.position = new Vector3(x, POCKET_Y, 0);

            // Trigger collider (ball detection)
            var trigger = pObj.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(pw - 0.15f, 0.65f);

            // Click collider (mouse input) — same size, not trigger
            var click = pObj.AddComponent<BoxCollider2D>();
            click.size = new Vector2(pw - 0.15f, 0.65f);

            // Visual child
            var vis = MakeQuad($"Vis_{i}", new Vector3(x, POCKET_Y, 0),
                new Vector2(pw - 0.17f, 0.62f), col, 1);

            var pocket = pObj.AddComponent<Pocket>();
            pocket.Initialize(i, Multipliers[i], isRed);
            list[i] = pocket;
        }
        return list;
    }

    // ── Ball ─────────────────────────────────────────────────
    void SpawnBall()
    {
        var ball = new GameObject("Ball");
        ball.transform.position = new Vector3(0, ARENA_TOP - 0.8f, 0);
        ball.AddComponent<Rigidbody2D>();
        ball.AddComponent<CircleCollider2D>();
        ball.AddComponent<Ball>();
    }

    // ── Helpers ──────────────────────────────────────────────
    void MakeWall(string name, Vector3 pos, Vector2 size, PhysicsMaterial2D mat,
        Color? color = null)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        var col = obj.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = mat;
        MakeQuad(name + "_V", pos, size, color ?? new Color(0.5f, 0.35f, 0.1f), 1);
    }

    static GameObject MakeQuad(string name, Vector3 pos, Vector2 size, Color color, int order)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetPixelSprite();
        sr.color = color;
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
}
