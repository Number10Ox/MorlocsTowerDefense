using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateMachine
{
    readonly struct StateAndTrigger : IEquatable<StateAndTrigger>
    {
        public readonly GameState State;
        public readonly GameTrigger Trigger;

        public StateAndTrigger(GameState state, GameTrigger trigger)
        {
            State = state;
            Trigger = trigger;
        }

        public bool Equals(StateAndTrigger other)
        {
            return State == other.State && Trigger == other.Trigger;
        }

        public override bool Equals(object obj)
        {
            return obj is StateAndTrigger other && Equals(other);
        }

        public override int GetHashCode()
        {
            const int HASH_PRIME = 397;
            unchecked
            {
                return ((int)State * HASH_PRIME) ^ (int)Trigger;
            }
        }
    }

    private readonly Dictionary<GameState, IGameState> states = new Dictionary<GameState, IGameState>();
    private readonly Dictionary<StateAndTrigger, GameState> transitions = new Dictionary<StateAndTrigger, GameState>();
    private IGameState currentState;
    private GameState currentStateId;
    private GameTrigger? pendingTrigger;
    private bool started;

    public GameState CurrentStateId => currentStateId;

    public event Action<GameState, GameState> OnStateChanged;

    public void AddState(GameState id, IGameState state)
    {
        if (states.ContainsKey(id))
        {
            Debug.LogWarning($"Duplicate state {id} registered. Overwriting.");
        }
        states[id] = state;
    }

    public void AddTransition(GameState from, GameTrigger trigger, GameState to)
    {
        var key = new StateAndTrigger(from, trigger);
        if (transitions.ContainsKey(key))
        {
            Debug.LogWarning($"Duplicate transition: ({from}, {trigger}) already registered. Overwriting.");
        }
        transitions[key] = to;
    }

    public void Start(GameState initialState)
    {
        if (started)
        {
            Debug.LogWarning("GameStateMachine.Start() called more than once. Ignoring.");
            return;
        }

        if (!states.TryGetValue(initialState, out var initialStateInstance))
        {
            Debug.LogError($"GameStateMachine.Start(): No state registered for {initialState}.");
            return;
        }

        started = true;
        currentStateId = initialState;
        currentState = initialStateInstance;
        currentState.Enter();
    }

    // Queues a trigger for resolution on the next Tick. Only one pending trigger
    // is held at a time; if Fire is called again before Tick resolves, the previous
    // trigger is overwritten. Triggers fired during a state's Tick are deferred to
    // the following frame.
    public void Fire(GameTrigger trigger)
    {
        if (!started)
        {
            Debug.LogWarning($"GameStateMachine.Fire() called before Start(). Ignoring trigger {trigger}.");
            return;
        }

        if (pendingTrigger.HasValue)
        {
            Debug.LogWarning($"Trigger {pendingTrigger.Value} overwritten by {trigger} before resolution.");
        }
        pendingTrigger = trigger;
    }

    public void Tick(float deltaTime)
    {
        if (!started)
        {
            Debug.LogWarning("GameStateMachine.Tick() called before Start(). Ignoring.");
            return;
        }

        if (pendingTrigger.HasValue)
        {
            ResolveTrigger(pendingTrigger.Value);
            pendingTrigger = null;
        }

        currentState.Tick(deltaTime);
    }

    private void ResolveTrigger(GameTrigger trigger)
    {
        var key = new StateAndTrigger(currentStateId, trigger);
        if (!transitions.TryGetValue(key, out GameState destination))
        {
            Debug.LogWarning($"No transition for ({currentStateId}, {trigger}). Ignoring.");
            return;
        }

        if (!states.TryGetValue(destination, out var destinationState))
        {
            Debug.LogError($"GameStateMachine: Transition ({currentStateId}, {trigger}) leads to {destination}, but no state is registered for it.");
            return;
        }

        GameState previousStateId = currentStateId;
        currentState.Exit();

        currentStateId = destination;
        currentState = destinationState;
        currentState.Enter();

        OnStateChanged?.Invoke(previousStateId, destination);
    }
}
