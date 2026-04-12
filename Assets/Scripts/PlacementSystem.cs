using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    float _arenaHalfW;
    float _pocketY;
    float _arenaTop;
    float _wallAngle = 45f;

    GameObject _preview;
    SpriteRenderer _previewSr;

    public void Setup(float halfW, float pocketY, float top)
    {
        _arenaHalfW = halfW;
        _pocketY = pocketY;
        _arenaTop = top;
        CreatePreview();
    }

    void CreatePreview()
    {
        _preview = new GameObject("WallPreview");
        _preview.transform.localScale = new Vector3(PlaceableWall.WallSize.x, PlaceableWall.WallSize.y, 1f);
        _previewSr = _preview.AddComponent<SpriteRenderer>();
        _previewSr.color = new Color(1f, 1f, 0.4f, 0.5f);
        _previewSr.sortingOrder = 10;
        _previewSr.sprite = MakePixelSprite();
        _preview.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool placing = gm.CurrentPhase == GameManager.Phase.Placing;
        _preview.SetActive(placing && gm.PlacementsRemaining > 0);

        if (!placing) return;

        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mw.z = 0f;

        _preview.transform.position = mw;
        _preview.transform.rotation = Quaternion.Euler(0, 0, _wallAngle);

        // Rotate with scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            _wallAngle += scroll > 0 ? 15f : -15f;

        // Snap rotation with R key
        if (Input.GetKeyDown(KeyCode.R))
            _wallAngle = Mathf.Round(_wallAngle / 45f) * 45f + 45f;

        if (Input.GetMouseButtonDown(0) && IsPlaceable(mw) && gm.TryUsePlacement())
        {
            var wallObj = new GameObject("PlacedWall");
            wallObj.transform.position = mw;
            wallObj.transform.rotation = Quaternion.Euler(0, 0, _wallAngle);
            wallObj.AddComponent<PlaceableWall>();
            gm.RegisterPlacedObject(wallObj);
        }
    }

    bool IsPlaceable(Vector3 p) =>
        p.x > -_arenaHalfW + 0.5f && p.x < _arenaHalfW - 0.5f &&
        p.y > _pocketY + 1.2f && p.y < _arenaTop - 0.5f;

    static Sprite MakePixelSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }
}
