using UnityEngine;

/// <summary>
/// HTML5 方式の壁配置：マウスドラッグで線分を描く。
/// 右クリックで最後の壁を取り消す。
/// </summary>
public class PlacementSystem : MonoBehaviour
{
    float _halfW, _pocketY, _arenaTop;
    PhysicsMaterial2D _wallMat;

    bool    _dragging;
    Vector2 _dragStart, _dragEnd;
    LineRenderer _preview;

    const float MIN_LEN = 0.3f; // HTML5: dx*dx+dy*dy > 400 (px) → ≈ 0.27 u

    public void Setup(float halfW, float pocketY, float arenaTop, PhysicsMaterial2D mat)
    {
        _halfW    = halfW;
        _pocketY  = pocketY;
        _arenaTop = arenaTop;
        _wallMat  = mat;
        CreatePreview();
    }

    void CreatePreview()
    {
        var go = new GameObject("WallPreview");
        _preview = go.AddComponent<LineRenderer>();
        _preview.positionCount  = 2;
        _preview.startWidth     = 0.14f;
        _preview.endWidth       = 0.14f;
        _preview.material       = new Material(Shader.Find("Sprites/Default"));
        _preview.startColor     = new Color(0.94f, 0.75f, 0.25f, 0.45f);
        _preview.endColor       = new Color(0.94f, 0.75f, 0.25f, 0.45f);
        _preview.sortingOrder   = 10;
        _preview.enabled        = false;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm.CurrentPhase != GameManager.Phase.Placing)
        {
            _dragging        = false;
            _preview.enabled = false;
            return;
        }

        Vector2 mw = GetMouseWorld();

        // ── Start drag ───────────────────────────────────────
        if (Input.GetMouseButtonDown(0)
            && gm.PlacementsRemaining > 0
            && IsInArena(mw))
        {
            _dragging  = true;
            _dragStart = mw;
            _dragEnd   = mw;
        }

        // ── Update preview ───────────────────────────────────
        if (_dragging)
        {
            _dragEnd         = ClampToArena(mw);
            _preview.enabled = true;
            _preview.SetPosition(0, new Vector3(_dragStart.x, _dragStart.y, -0.1f));
            _preview.SetPosition(1, new Vector3(_dragEnd.x,   _dragEnd.y,   -0.1f));
        }

        // ── End drag ─────────────────────────────────────────
        if (_dragging && Input.GetMouseButtonUp(0))
        {
            _dragging        = false;
            _preview.enabled = false;
            if (Vector2.Distance(_dragStart, _dragEnd) >= MIN_LEN
                && gm.TryUsePlacement())
            {
                PlaceableWall.Create(_dragStart, _dragEnd, _wallMat);
            }
        }

        // ── Right-click: undo last wall ───────────────────────
        if (Input.GetMouseButtonDown(1))
            gm.RemoveLastPlacedObject();
    }

    Vector2 GetMouseWorld()
    {
        var v = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2(v.x, v.y);
    }

    bool IsInArena(Vector2 p) =>
        p.x > -_halfW && p.x < _halfW &&
        p.y > _pocketY && p.y < _arenaTop;

    Vector2 ClampToArena(Vector2 p) => new(
        Mathf.Clamp(p.x, -_halfW + 0.1f, _halfW - 0.1f),
        Mathf.Clamp(p.y, _pocketY + 0.1f, _arenaTop - 0.1f));
}
