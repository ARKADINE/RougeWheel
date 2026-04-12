using UnityEngine;

public class Pocket : MonoBehaviour
{
    public int Index { get; private set; }
    public int Multiplier { get; private set; }

    SpriteRenderer _sr;
    Color _baseColor;
    static readonly Color SelectedColor = new(1f, 0.85f, 0f);

    public void Initialize(int index, int multiplier, bool isRed)
    {
        Index = index;
        Multiplier = multiplier;
        _baseColor = isRed ? new Color(0.75f, 0.1f, 0.1f) : new Color(0.15f, 0.15f, 0.15f);
        _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr != null) _sr.color = _baseColor;
    }

    public void SetSelected(bool selected)
    {
        if (_sr != null)
            _sr.color = selected ? SelectedColor : _baseColor;
    }

    void OnMouseDown()
    {
        if (GameManager.Instance.CurrentPhase == GameManager.Phase.Betting)
            GameManager.Instance.SelectPocket(Index);
    }
}
