using UnityEngine;

public class PlaceableWall : MonoBehaviour
{
    public static readonly Vector2 WallSize = new(1.5f, 0.12f);
    static readonly Color WallColor = new(0.85f, 0.65f, 0.2f);
    static Sprite _sprite;

    void Awake()
    {
        var col = gameObject.AddComponent<BoxCollider2D>();
        col.size = WallSize;
        col.sharedMaterial = new PhysicsMaterial2D { bounciness = 0.65f, friction = 0.05f };

        var vis = new GameObject("Visual");
        vis.transform.SetParent(transform, false);
        vis.transform.localScale = new Vector3(WallSize.x, WallSize.y, 1f);

        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = WallColor;
        sr.sortingOrder = 3;
    }

    static Sprite GetSprite()
    {
        if (_sprite != null) return _sprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        return _sprite;
    }
}
