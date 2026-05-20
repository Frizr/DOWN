using Godot;

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
    private Area2D _hitBox;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        var hitBox = GetNodeOrNull<Area2D>("HitBox")
            ?? GetParent()?.GetNodeOrNull<Area2D>("HitBox");
        if (hitBox == null)
        {
            GD.PushWarning("[AttackSystem] Could not find HitBox Area2D on AttackSystem or parent.");
            return;
        }

        _hitBox = hitBox;
        _hitBox.Monitoring = false;   // starts inactive

        // Wire up hit detection
        _hitBox.BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= dt;

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
        if (_cooldownTimer > 0f)
            return false;

        bool isHeavy = (_comboStep == 2);   // Third hit is the finisher

        _comboStep = (_comboStep + 1) % 3;
        IsAttacking = true;
        _comboTimer = ComboWindow;

        float cooldown = isHeavy ? HeavyCooldown : LightCooldown;
        _cooldownTimer = cooldown;

        EmitSignal(SignalName.AttackStarted, _comboStep);

        // Activate hitbox for a brief window then deactivate
        ActivateHitBox(isHeavy ? HeavyDamage : LightDamage);

        return true;
    }

    /// <summary>Called externally when the attack animation finishes.</summary>
    public void OnAnimationFinished()
    {
        IsAttacking = false;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private async void ActivateHitBox(int damage)
    {
        if (_hitBox == null)
        {
            GD.PushWarning("[AttackSystem] Cannot activate attack hitbox because HitBox is missing.");
            return;
        }

        _hitBox.SetMeta("damage", damage);   // Store damage value for the hit callback
        _hitBox.Monitoring = true;

        await ToSignal(
            GetTree().CreateTimer(HitBoxDuration, false),
            SceneTreeTimer.SignalName.Timeout
        );

        _hitBox.Monitoring = false;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_hitBox.Monitoring)
            return;

        int damage = (int)_hitBox.GetMeta("damage", LightDamage);

        // Resolve Health component on target
        var hp = body.GetNodeOrNull<Health>("Health");
        if (hp != null && !hp.IsDead)
        {
            int dealt = hp.TakeDamage(damage);
            EmitSignal(SignalName.HitConnected, body, dealt);
            DoHitStop();
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
