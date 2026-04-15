using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum Phase { Betting, Placing, Launching, Result }

    public Phase CurrentPhase    { get; private set; }
    public int   Coins           { get; private set; } = 100;
    public int   BetAmount       { get; private set; } = 10;
    public int   SelectedPocket  { get; private set; } = -1;
    public int   PlacementsRemaining { get; private set; }
    public const int MaxPlacements = 3;

    public event Action<Phase>       OnPhaseChanged;
    public event Action<int>         OnCoinsChanged;
    public event Action<bool, int, int> OnResult; // (won, pocketIndex, gain)

    Pocket[]             _pockets;
    readonly List<GameObject> _placed = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(Pocket[] pockets)
    {
        _pockets = pockets;
        TransitionTo(Phase.Betting);
    }

    // ── Betting phase ────────────────────────────────────────
    public void SelectPocket(int index)
    {
        if (CurrentPhase != Phase.Betting) return;
        SelectedPocket = index;
        OnPhaseChanged?.Invoke(CurrentPhase); // refresh UI
    }

    public void SetBetAmount(int amount)
    {
        BetAmount = amount;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    /// <summary>
    /// HTML5 に合わせてベット時にコインは減らさない。結果時に増減する。
    /// </summary>
    public void ConfirmBet()
    {
        if (CurrentPhase != Phase.Betting || SelectedPocket < 0) return;
        PlacementsRemaining = MaxPlacements;
        TransitionTo(Phase.Placing);
    }

    // ── Placing phase ────────────────────────────────────────
    public bool TryUsePlacement()
    {
        if (CurrentPhase != Phase.Placing || PlacementsRemaining <= 0) return false;
        PlacementsRemaining--;
        OnPhaseChanged?.Invoke(CurrentPhase);
        return true;
    }

    public void RegisterPlacedObject(GameObject obj) => _placed.Add(obj);

    /// <summary>右クリックで最後の壁を取り消す（HTML5 の右クリック削除）</summary>
    public void RemoveLastPlacedObject()
    {
        if (_placed.Count == 0) return;
        int last = _placed.Count - 1;
        if (_placed[last] != null) Destroy(_placed[last]);
        _placed.RemoveAt(last);
        PlacementsRemaining = Mathf.Min(PlacementsRemaining + 1, MaxPlacements);
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    public void ClearAllPlaced()
    {
        foreach (var obj in _placed)
            if (obj != null) Destroy(obj);
        _placed.Clear();
        PlacementsRemaining = MaxPlacements;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    public void LaunchBall()
    {
        if (CurrentPhase != Phase.Placing) return;
        TransitionTo(Phase.Launching);
    }

    // ── Launching phase ──────────────────────────────────────
    public void OnBallLanded(int pocketIndex)
    {
        if (CurrentPhase != Phase.Launching) return;

        bool won  = (pocketIndex == SelectedPocket);
        int  gain = 0;

        if (won)
        {
            // Win: bet × multiplier （HTML5: G.coins += gain）
            gain   = BetAmount * _pockets[pocketIndex].Multiplier;
            Coins += gain;
        }
        else
        {
            // Lose: lose bet amount （HTML5: G.coins -= G.bet）
            Coins = Mathf.Max(0, Coins - BetAmount);
        }

        OnCoinsChanged?.Invoke(Coins);
        OnResult?.Invoke(won, pocketIndex, gain);
        TransitionTo(Phase.Result);
    }

    // ── Result phase ─────────────────────────────────────────
    public void NextRound()
    {
        if (CurrentPhase != Phase.Result) return;

        foreach (var obj in _placed)
            if (obj != null) Destroy(obj);
        _placed.Clear();

        var ball = FindFirstObjectByType<Ball>();
        if (ball != null) Destroy(ball.gameObject);

        // Restart with 100 if broke (HTML5 behaviour)
        if (Coins <= 0)
        {
            Coins = 100;
            OnCoinsChanged?.Invoke(Coins);
        }

        SelectedPocket = -1;
        TransitionTo(Phase.Betting);
    }

    void TransitionTo(Phase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}
