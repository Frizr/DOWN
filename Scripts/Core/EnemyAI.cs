using Godot;
using Godot.Collections;

/// <summary>
/// EnemyAI — Agent 3: Enemy AI
/// Full state machine: Idle → Patrol → Aggro → Attack → Dead.
/// Uses NavigationAgent2D for pathfinding through the tilemap navmesh.
/// Attach as a child Node of EnemyBase.
///
/// Node type: Node
/// Siblings : DetectRange (Area2D), AttackRange (Area2D)
/// Parent   : EnemyBase (CharacterBody2D)
/// </summary>
public partial class EnemyAI : Node
{
    // ─── State Enum ──────────────────────────────────────────────────────────

    public enum AIState
    {
        Idle,       // Standing still, scanning
        Patrol,     // Walking between waypoints
        Aggro,      // Chasing detected player
        Attack,     // In melee/ranged range — executing attack
        Dead        // No processing
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [ExportGroup("Detection")]
    [Export] public float DetectRadius  = 180f;   // Pixels — outer awareness ring
    [Export] public float LoseRadius    = 280f;   // Pixels — player escapes beyond this
    [Export] public float AttackRadius  = 40f;    // Pixels — enter attack state

    [ExportGroup("Patrol")]
    [Export] public Array<NodePath> WaypointPaths = new();  // Assign Node2D waypoints in editor
    [Export] public float PatrolWaitTime = 1.5f;   // Seconds to idle at each waypoint
    [Export] public float PatrolSpeed    = 0.6f;   // Fraction of MoveSpeed

    [ExportGroup("Aggro")]
    [Export] public float AggroSpeed     = 1.0f;   // Fraction of MoveSpeed
    [Export] public float PathUpdateRate = 0.25f;  // Seconds between nav recalculations

    [ExportGroup("Attack")]
    [Export] public float AttackCooldown = 1.2f;   // Seconds between attacks
    [Export] public int   AttackDamage   = 12;

    // ─── Signals ──────────────────────────────────────────────────────────────

    [Signal] public delegate void StateChangedEventHandler(int newState);  // int = AIState cast

    // ─── References ───────────────────────────────────────────────────────────

    private EnemyBase        _enemy;
    private NavigationAgent2D _nav;
    private Node2D           _player;

    // ─── Internal State ───────────────────────────────────────────────────────

    public AIState State { get; private set; } = AIState.Idle;

    private int   _waypointIndex = 0;
    private float _patrolWaitTimer  = 0f;
    private float _attackCoolTimer  = 0f;
    private float _pathUpdateTimer  = 0f;
    private bool  _waitingAtWaypoint = false;

    private Array<Node2D> _waypoints = new();

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _enemy = GetParent<EnemyBase>();
        _nav   = _enemy.GetNode<NavigationAgent2D>("NavigationAgent2D");

        // Resolve waypoint node paths
        foreach (var path in WaypointPaths)
        {
            var wp = GetNode<Node2D>(path);
            if (wp != null) _waypoints.Add(wp);
        }

        // Locate player — they are in the "player" group
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0)
            _player = players[0] as Node2D;

        // Nav agent configuration
        _nav.PathDesiredDistance    = 6f;
        _nav.TargetDesiredDistance  = 16f;

        TransitionTo(AIState.Idle);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (State == AIState.Dead || !_enemy.IsAlive)
        {
            TransitionTo(AIState.Dead);
            return;
        }

        float dt = (float)delta;
        _attackCoolTimer -= dt;
        _pathUpdateTimer -= dt;

        switch (State)
        {
            case AIState.Idle:    TickIdle(dt);    break;
            case AIState.Patrol:  TickPatrol(dt);  break;
            case AIState.Aggro:   TickAggro(dt);   break;
            case AIState.Attack:  TickAttack(dt);  break;
        }
    }

    // ─── State Ticks ─────────────────────────────────────────────────────────

    private void TickIdle(float dt)
    {
        if (CanDetectPlayer())
        {
            TransitionTo(AIState.Aggro);
            return;
        }

        // After a short pause, begin patrol if waypoints exist
        _patrolWaitTimer -= dt;
        if (_patrolWaitTimer <= 0f && _waypoints.Count > 0)
            TransitionTo(AIState.Patrol);
    }

    private void TickPatrol(float dt)
    {
        // If player enters range, switch immediately
        if (CanDetectPlayer())
        {
            TransitionTo(AIState.Aggro);
            return;
        }

        if (_waitingAtWaypoint)
        {
            _patrolWaitTimer -= dt;
            _enemy.Velocity   = Vector2.Zero;
            _enemy.PlayAnim("idle");
            if (_patrolWaitTimer <= 0f)
            {
                _waitingAtWaypoint = false;
                _waypointIndex = (_waypointIndex + 1) % _waypoints.Count;
                SetNavTarget(_waypoints[_waypointIndex].GlobalPosition);
            }
            return;
        }

        MoveAlongPath(_enemy.MoveSpeed * PatrolSpeed);

        // Arrived at waypoint?
        if (_nav.IsNavigationFinished())
        {
            _waitingAtWaypoint = true;
            _patrolWaitTimer   = PatrolWaitTime;
            _enemy.PlayAnim("idle");
        }
    }

    private void TickAggro(float dt)
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            TransitionTo(AIState.Patrol);
            return;
        }

        float dist = _enemy.GlobalPosition.DistanceTo(_player.GlobalPosition);

        // Player escaped the lose radius
        if (dist > LoseRadius)
        {
            TransitionTo(AIState.Patrol);
            return;
        }

        // Close enough to attack
        if (dist <= AttackRadius)
        {
            TransitionTo(AIState.Attack);
            return;
        }

        // Recalculate path periodically
        if (_pathUpdateTimer <= 0f)
        {
            SetNavTarget(_player.GlobalPosition);
            _pathUpdateTimer = PathUpdateRate;
        }

        MoveAlongPath(_enemy.MoveSpeed * AggroSpeed);
        _enemy.PlayAnim("walk");
    }

    private void TickAttack(float dt)
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            TransitionTo(AIState.Patrol);
            return;
        }

        float dist = _enemy.GlobalPosition.DistanceTo(_player.GlobalPosition);

        // Player moved out of attack range — resume chase
        if (dist > AttackRadius * 1.3f)
        {
            TransitionTo(AIState.Aggro);
            return;
        }

        // Stand still and swing
        _enemy.Velocity = Vector2.Zero;

        if (_attackCoolTimer <= 0f)
        {
            ExecuteAttack();
            _attackCoolTimer = AttackCooldown;
        }
        else
        {
            _enemy.PlayAnim("idle");
        }
    }

    // ─── Transitions ─────────────────────────────────────────────────────────

    private void TransitionTo(AIState next)
    {
        if (State == next)
            return;

        AIState prev = State;
        State = next;

        switch (next)
        {
            case AIState.Idle:
                _patrolWaitTimer = PatrolWaitTime;
                _enemy.Velocity  = Vector2.Zero;
                _enemy.PlayAnim("idle");
                break;

            case AIState.Patrol:
                if (_waypoints.Count > 0)
                    SetNavTarget(_waypoints[_waypointIndex].GlobalPosition);
                _enemy.PlayAnim("walk");
                break;

            case AIState.Aggro:
                _enemy.EmitSignal(EnemyBase.SignalName.PlayerDetected, _player);
                break;

            case AIState.Attack:
                _enemy.PlayAnim("attack");
                break;

            case AIState.Dead:
                _enemy.Velocity = Vector2.Zero;
                SetPhysicsProcess(false);
                break;
        }

        EmitSignal(SignalName.StateChanged, (int)next);  // listeners cast back to AIState
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Is the player within DetectRadius and line-of-sight?</summary>
    private bool CanDetectPlayer()
    {
        if (_player == null || !IsInstanceValid(_player))
            return false;

        float dist = _enemy.GlobalPosition.DistanceTo(_player.GlobalPosition);
        if (dist > DetectRadius)
            return false;

        // Optional: add a raycast here for LOS check
        return true;
    }

    private void SetNavTarget(Vector2 globalPos)
    {
        _nav.TargetPosition = globalPos;
    }

    /// <summary>Move the enemy body along the current nav path.</summary>
    private void MoveAlongPath(float speed)
    {
        if (_nav.IsNavigationFinished())
        {
            _enemy.Velocity = Vector2.Zero;
            return;
        }

        Vector2 nextPoint = _nav.GetNextPathPosition();
        Vector2 dir       = (_enemy.GlobalPosition.DirectionTo(nextPoint));
        _enemy.Velocity   = dir * speed;
    }

    /// <summary>Deliver melee damage to the player.</summary>
    private void ExecuteAttack()
    {
        _enemy.PlayAnim("attack");

        // Reach the player's Health component
        var hp = _player?.GetNodeOrNull<Health>("Health");
        if (hp != null && !hp.IsDead)
        {
            hp.TakeDamage(AttackDamage);
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Force this enemy into aggro immediately (e.g. hit from stealth).</summary>
    public void ForceAggro()
    {
        if (_player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) _player = players[0] as Node2D;
        }
        TransitionTo(AIState.Aggro);
    }
}
