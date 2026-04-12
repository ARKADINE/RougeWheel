using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum Phase { Betting, Placing, Launching, Result }

    public Phase CurrentPhase { get; private set; }
    public int Coins { get; private set; } = 100;
    public int BetAmount { get; private set; } = 10;
    public int SelectedPocket { get; private set; } = -1;
    public int MaxPlacements { get; private set; } = 3;
    public int PlacementsRemaining { get; private set; }

    public event Action<Phase> OnPhaseChanged;
    public event Action<int> OnCoinsChanged;
    public event Action<bool, int, int> OnResult; // won, pocketIndex, gain

    Pocket[] _pockets;
    readonly List<GameObject> _placedObjects = new();

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

    public void SelectPocket(int index)
    {
        if (CurrentPhase != Phase.Betting) return;
        SelectedPocket = index;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    public void ConfirmBet()
    {
        if (CurrentPhase != Phase.Betting || SelectedPocket < 0 || Coins < BetAmount) return;
        Coins -= BetAmount;
        OnCoinsChanged?.Invoke(Coins);
        PlacementsRemaining = MaxPlacements;
        TransitionTo(Phase.Placing);
    }

    public bool TryUsePlacement()
    {
        if (CurrentPhase != Phase.Placing || PlacementsRemaining <= 0) return false;
        PlacementsRemaining--;
        OnPhaseChanged?.Invoke(CurrentPhase);
        return true;
    }

    public void RegisterPlacedObject(GameObject obj) => _placedObjects.Add(obj);

    public void LaunchBall()
    {
        if (CurrentPhase != Phase.Placing) return;
        TransitionTo(Phase.Launching);
    }

    public void OnBallLanded(int pocketIndex)
    {
        if (CurrentPhase != Phase.Launching) return;
        bool won = (pocketIndex == SelectedPocket);
        int gain = won ? BetAmount * _pockets[pocketIndex].Multiplier : 0;
        Coins += gain;
        OnCoinsChanged?.Invoke(Coins);
        OnResult?.Invoke(won, pocketIndex, gain);
        TransitionTo(Phase.Result);
    }

    public void NextRound()
    {
        if (CurrentPhase != Phase.Result) return;
        foreach (var obj in _placedObjects)
            if (obj != null) Destroy(obj);
        _placedObjects.Clear();

        var ball = FindFirstObjectByType<Ball>();
        if (ball != null) Destroy(ball.gameObject);

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
