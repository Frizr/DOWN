using Godot;
using System.Collections.Generic;

/// <summary>
/// AttackSystem — Agent 2: Player Controller
/// Handles melee combat: 3-hit combo chain, damage dealing, hitstop, and
/// Area2D hitbox activation. Attach as a child of Player.
///
/// Node type: Node
/// Requires: an Area2D child named "HitBox" with a CollisionShape2D.
/// </summary>
public partial class AttackSystem : Node
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [ExportGroup("Damage")]
    [Export] public int   LightDamage  = 15;
    [Export] public int   HeavyDamage  = 40;   // 3rd hit in combo chain
    [Export] public float HitStopDuration = 0.06f;  // seconds engine pauses on connect

    [ExportGroup("Timing (seconds)")]
    [Export] public float LightCooldown  = 0.35f;
    [Export] public float HeavyCooldown  = 0.6f;
    [Export] public float ComboWindow    = 0.55f;  // time after hit to chain next attack
    [Export] public float HitBoxDuration = 0.12f;  // how long hitbox stays active

    [ExportGroup("References")]
    [Export] public NodePath HitBoxPath = "HitBox";  // Area2D child node

    // ─── Signals ──────────────────────────────────────────────────────────────

    [Signal] public delegate void AttackStartedEventHandler(int comboStep);
    [Signal] public delegate void HitConnectedEventHandler(Node target, int damage);
    [Signal] public delegate void ComboFinishedEventHandler();

    // ─── State ────────────────────────────────────────────────────────────────

    public  bool  IsAttacking { get; private set; } = false;
    private int   _comboStep  = 0;   // 0→1→2→reset (steps 1-2 light, step 3 heavy)
    private float _cooldownTimer = 0f;
    private float _comboTimer    = 0f;
    private float _attackTimer   = 0f;
    private int   _activeDamage  = 0;
    private Area2D _hitBox;
    private CollisionShape2D _hitBoxShape;
    private readonly HashSet<ulong> _hitTargets = new();
    private Node _owner;
    private Vector2 _facing = Vector2.Right;
    private bool _disabled = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _owner = GetParent();

        var hitBox = GetNodeOrNull<Area2D>(HitBoxPath)
            ?? GetNodeOrNull<Area2D>("HitBox")
            ?? GetParent()?.GetNodeOrNull<Area2D>("HitBox");
        if (hitBox == null)
        {
            GD.PushWarning("[AttackSystem] Could not find HitBox Area2D on AttackSystem or parent.");
            return;
        }

        _hitBox = hitBox;
        _hitBox.Monitoring = false;   // starts inactive
        _hitBoxShape = _hitBox.GetNodeOrNull<CollisionShape2D>("HitBoxShape")
            ?? _hitBox.FindChild("HitBoxShape", true, false) as CollisionShape2D;

        // Wire up hit detection
        _hitBox.BodyEntered += OnBodyEntered;
        _hitBox.AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        if (_disabled)
            return;

        float dt = (float)delta;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;

        if (IsAttacking)
        {
            _attackTimer -= dt;
            if (_attackTimer <= 0f)
                OnAnimationFinished();
        }

        // Auto-reset combo chain if the window lapses
        if (_comboStep > 0 && !IsAttacking)
        {
            _comboTimer -= dt;
            if (_comboTimer <= 0f)
            {
                _comboStep = 0;
                EmitSignal(SignalName.ComboFinished);
            }
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to perform the next attack in the combo chain.
    /// Call this from PlayerController when the attack input fires.
    /// Returns false if still in cooldown.
    /// </summary>
    public bool TryAttack()
    {
        if (_disabled || IsOwnerDead() || _cooldownTimer > 0f)
            return false;

        bool isHeavy = (_comboStep == 2);   // Third hit is the finisher

        _comboStep = (_comboStep + 1) % 3;
        IsAttacking = true;
        _comboTimer = ComboWindow;

        float cooldown = isHeavy ? HeavyCooldown : LightCooldown;
        _cooldownTimer = cooldown;
        _attackTimer = cooldown;

        EmitSignal(SignalName.AttackStarted, _comboStep);

        // Activate hitbox for a brief window then deactivate
        ActivateHitBox(isHeavy ? HeavyDamage : LightDamage);

        return true;
    }

    public void SetFacing(Vector2 facing)
    {
        if (facing == Vector2.Zero)
            return;

        _facing = facing.Normalized();
        UpdateHitBoxDirection();
    }

    /// <summary>Called externally when the attack animation finishes.</summary>
    public void OnAnimationFinished()
    {
        IsAttacking = false;
        _attackTimer = 0f;
        DisableHitBox();
    }

    public void DisableAttack()
    {
        _disabled = true;
        IsAttacking = false;
        _comboStep = 0;
        _cooldownTimer = 0f;
        _comboTimer = 0f;
        _attackTimer = 0f;
        _activeDamage = 0;
        _hitTargets.Clear();
        DisableHitBox();
        SetProcess(false);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private async void ActivateHitBox(int damage)
    {
        if (_disabled || IsOwnerDead())
        {
            DisableHitBox();
            return;
        }

        if (_hitBox == null)
        {
            GD.PushWarning("[AttackSystem] Cannot activate attack hitbox because HitBox is missing.");
            return;
        }

        _activeDamage = damage;
        _hitTargets.Clear();
        UpdateHitBoxDirection();
        _hitBox.Monitoring = true;

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        if (_disabled || IsOwnerDead())
        {
            DisableHitBox();
            return;
        }

        foreach (Area2D area in _hitBox.GetOverlappingAreas())
            TryDamage(area);
        foreach (Node2D body in _hitBox.GetOverlappingBodies())
            TryDamage(body);

        await ToSignal(
            GetTree().CreateTimer(HitBoxDuration, false),
            SceneTreeTimer.SignalName.Timeout
        );

        DisableHitBox();
    }

    private void OnAreaEntered(Area2D area)
    {
        TryDamage(area);
    }

    private void OnBodyEntered(Node2D body)
    {
        TryDamage(body);
    }

    private void TryDamage(Node node)
    {
        if (_disabled || IsOwnerDead() || _hitBox == null || !_hitBox.Monitoring)
            return;

        if (!TryResolveDamageTarget(node, out Node target, out Health hp))
            return;

        if (hp.IsDead || !_hitTargets.Add(target.GetInstanceId()))
            return;

        int dealt = hp.TakeDamage(_activeDamage);
        if (dealt <= 0)
            return;

        if (target is EnemyBase enemy && enemy.IsAlive && _owner is Node2D attacker)
        {
            enemy.ApplyKnockback(attacker.GlobalPosition);
            enemy.AI?.ForceAggro();
        }

        EmitSignal(SignalName.HitConnected, target, dealt);
        DoHitStop();
    }

    private bool TryResolveDamageTarget(Node node, out Node target, out Health hp)
    {
        target = null;
        hp = null;

        for (Node current = node; current != null; current = current.GetParent())
        {
            if (current == _owner || (_owner != null && _owner.IsAncestorOf(current)))
                return false;

            hp = current.GetNodeOrNull<Health>("Health");
            if (hp != null)
            {
                target = current;
                return true;
            }
        }

        return false;
    }

    private void DisableHitBox()
    {
        if (_hitBox != null)
            _hitBox.Monitoring = false;
    }

    private bool IsOwnerDead()
    {
        if (_owner is PlayerController player)
            return player.IsDead;

        var ownerHealth = _owner?.GetNodeOrNull<Health>("Health");
        return ownerHealth?.IsDead == true;
    }

    private void UpdateHitBoxDirection()
    {
        if (_hitBoxShape == null)
            return;

        bool horizontal = Mathf.Abs(_facing.X) >= Mathf.Abs(_facing.Y);
        if (horizontal)
        {
            float x = _facing.X >= 0f ? 28f : -28f;
            _hitBoxShape.Position = new Vector2(x, 0f);
            if (_hitBoxShape.Shape is RectangleShape2D rect)
                rect.Size = new Vector2(40f, 20f);
        }
        else
        {
            float y = _facing.Y >= 0f ? 28f : -28f;
            _hitBoxShape.Position = new Vector2(0f, y);
            if (_hitBoxShape.Shape is RectangleShape2D rect)
                rect.Size = new Vector2(20f, 40f);
        }
    }

    /// <summary>
    /// Brief engine slow-motion on hit — classic action game feel.
    /// Scales Engine.TimeScale down then restores it.
    /// </summary>
    private async void DoHitStop()
    {
        Engine.TimeScale = 0.05f;
        await ToSignal(
            GetTree().CreateTimer(HitStopDuration, true),  // true = use real time
            SceneTreeTimer.SignalName.Timeout
        );
        Engine.TimeScale = 1.0f;
    }
}
