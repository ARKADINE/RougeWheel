using UnityEngine;

public class Pocket : MonoBehaviour
{
    public int   Index      { get; private set; }
    public int   Multiplier { get; private set; }
    public Color PocketColor { get; private set; }

    SpriteRenderer _sr;

    public void Initialize(int index, int multiplier, Color color, SpriteRenderer sr)
    {
        Index       = index;
        Multiplier  = multiplier;
        PocketColor = color;
        _sr         = sr;
        if (_sr != null) _sr.color = color * 0.25f; // dim default
    }

    /// <summary>選択中（ベット確定前）の表示</summary>
    public void SetSelected(bool selected)
    {
        if (_sr == null) return;
        _sr.color = selected ? PocketColor * 0.6f : PocketColor * 0.25f;
    }

    /// <summary>ボールが落ちたポケットを明るく表示</summary>
    public void SetResult(bool isResult)
    {
        if (_sr == null) return;
        _sr.color = isResult ? PocketColor : PocketColor * 0.25f;
    }

    void OnMouseDown()
    {
        if (GameManager.Instance.CurrentPhase == GameManager.Phase.Betting)
            GameManager.Instance.SelectPocket(Index);
    }
}
