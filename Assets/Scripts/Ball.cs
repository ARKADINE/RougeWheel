using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    Rigidbody2D _rb;
    bool _landed;

    void Awake()
    {
        gameObject.tag = "Ball";

        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 1.5f;
        _rb.linearDamping = 0.05f;
        _rb.angularDamping = 0.5f;

        var col = GetComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.sharedMaterial = new PhysicsMaterial2D { bounciness = 0.55f, friction = 0.15f };

        // Visual
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite(64);
        sr.color = Color.white;
        sr.sortingOrder = 5;
        transform.localScale = Vector3.one * 0.5f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_landed) return;
        var pocket = other.GetComponent<Pocket>();
        if (pocket == null) return;
        _landed = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = 0f;
        GameManager.Instance.OnBallLanded(pocket.Index);
    }

    static Sprite MakeCircleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);
    }
}
