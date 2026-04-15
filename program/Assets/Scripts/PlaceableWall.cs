using UnityEngine;

/// <summary>
/// HTML5 のドラッグ壁を Unity に再現。
/// EdgeCollider2D（線分）＋視覚用の伸縮 Quad。
/// </summary>
public class PlaceableWall : MonoBehaviour
{
    static Sprite _sprite;

    /// <summary>2点間に壁を生成し GameManager に登録する。</summary>
    public static void Create(Vector2 p1, Vector2 p2, PhysicsMaterial2D mat)
    {
        var go = new GameObject("PlacedWall");
        go.AddComponent<PlaceableWall>().Init(p1, p2, mat);
        GameManager.Instance.RegisterPlacedObject(go);
    }

    void Init(Vector2 p1, Vector2 p2, PhysicsMaterial2D mat)
    {
        // Physics: EdgeCollider2D at world origin（points = world coords）
        transform.position = Vector3.zero;
        var ec = gameObject.AddComponent<EdgeCollider2D>();
        ec.points        = new[] { p1, p2 };
        ec.edgeRadius    = 0.06f;
        ec.sharedMaterial = mat;

        // Visual: rotated/scaled quad
        Vector2 mid   = (p1 + p2) * 0.5f;
        float   len   = Vector2.Distance(p1, p2);
        float   angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;

        var vis = new GameObject("Visual");
        vis.transform.SetParent(transform, false);
        vis.transform.position   = mid;
        vis.transform.rotation   = Quaternion.Euler(0, 0, angle);
        vis.transform.localScale = new Vector3(len, 0.14f, 1f);

        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite       = GetSprite();
        sr.color        = new Color(0.94f, 0.75f, 0.25f); // #f0c040 gold
        sr.sortingOrder = 3;

        // Bright inner highlight (HTML5: fde68a overlay)
        var hi = new GameObject("Highlight");
        hi.transform.SetParent(vis.transform, false);
        hi.transform.localScale = new Vector3(1f, 0.35f, 1f);
        var hiSr = hi.AddComponent<SpriteRenderer>();
        hiSr.sprite       = GetSprite();
        hiSr.color        = new Color(0.99f, 0.91f, 0.54f, 0.7f); // #fde68a
        hiSr.sortingOrder = 4;
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
