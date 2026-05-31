using Godot;

/// <summary>
/// Health — Agent 2: Player Controller
/// Reusable health component. Attach as a child node on any entity
/// (Player, Enemy, Destructible). Emits signals so other systems
/// (UI, GameManager, Camera) can react without tight coupling.
///
/// Node type: Node (no physics, pure data + signals)
/// </summary>
public partial class Health : Node
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [ExportGroup("Stats")]
    [Export] public int MaxHealth   = 100;
    [Export] public bool Invincible = false;   // Set true during dodge-roll i-frames

    [ExportGroup("Regen")]
    [Export] public bool  RegenEnabled = false;
    [Export] public float RegenPerSec  = 2f;
    [Export] public float RegenDelay   = 4f;   // Seconds after last hit before regen starts

    // ─── Signals ──────────────────────────────────────────────────────────────

    [Signal] public delegate void DamageTakenEventHandler(int amount, int currentHp);
    [Signal] public delegate void HealedEventHandler(int amount, int currentHp);
    [Signal] public delegate void HealthChangedEventHandler(int current, int max);
    [Signal] public delegate void DiedEventHandler();

    // ─── State ────────────────────────────────────────────────────────────────

    public int  Current    { get; private set; }
    public bool IsDead     => Current <= 0;
    public float HpPercent => MaxHealth > 0 ? (float)Current / MaxHealth : 0f;

    private float _regenTimer = 0f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Current = MaxHealth;
        EmitSignal(SignalName.HealthChanged, Current, MaxHealth);
    }

    public override void _Process(double delta)
    {
        if (!RegenEnabled || IsDead || Current >= MaxHealth)
            return;

        _regenTimer -= (float)delta;
        if (_regenTimer <= 0f)
            Heal(Mathf.RoundToInt(RegenPerSec * (float)delta));
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Apply damage. Respects Invincible flag.
    /// Returns actual damage dealt (0 if invincible or already dead).
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (Invincible || IsDead || amount <= 0)
            return 0;

        int dealt = Mathf.Min(amount, Current);
        Current  -= dealt;
        _regenTimer = RegenDelay;

        EmitSignal(SignalName.DamageTaken, dealt, Current);
        EmitSignal(SignalName.HealthChanged, Current, MaxHealth);

        if (IsDead)
            EmitSignal(SignalName.Died);

        return dealt;
    }

    /// <summary>Restore health, clamped to MaxHealth.</summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int before  = Current;
        Current     = Mathf.Min(Current + amount, MaxHealth);
        int healed  = Current - before;

        if (Current != before)
            EmitSignal(SignalName.HealthChanged, Current, MaxHealth);

        if (healed > 0)
            EmitSignal(SignalName.Healed, healed, Current);
    }

    /// <summary>Instantly fill HP (e.g. full restore pickup).</summary>
    public void FullRestore() => Heal(MaxHealth);

    /// <summary>
    /// Grant temporary invincibility for a given duration in seconds.
    /// Used during dodge rolls and hit-stun.
    /// </summary>
    public async void SetInvincible(float duration)
    {
        Invincible = true;
        await ToSignal(GetTree().CreateTimer(duration, false), SceneTreeTimer.SignalName.Timeout);
        Invincible = false;
    }
}
