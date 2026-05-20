using Godot;

/// <summary>
/// EnemyGrunt — Agent 3: Enemy AI
/// Concrete enemy type: fast, low-HP, kamikaze charger.
/// Extends EnemyBase — only overrides stats and special behaviour.
///
/// To create a new enemy type, extend EnemyBase the same way.
/// </summary>
public partial class EnemyGrunt : EnemyBase
{
    [ExportGroup("Grunt Specific")]
    [Export] public float ChargeSpeedMult = 2.2f;   // Extra burst speed on first aggro contact
    [Export] public float ChargeDuration  = 0.4f;   // How long the burst lasts

    private bool  _hasCharged  = false;
    private float _chargeTimer = 0f;

    protected override void OnReady()
    {
        // Override base stats for the Grunt archetype
        MoveSpeed     = 110f;
        KillScore     = 80;
        ContactDamage = 6;
        KnockbackForce = 180f;

        HP.MaxHealth = 30;
        HP.FullRestore();      // Re-sync Current to the new MaxHealth value.
                               // Health._Ready() already set Current = 100 (old max),
                               // so we must refresh it here.
    }

    public override void _PhysicsProcess(double delta)
    {
        // Charge burst logic — fires once when Grunt first enters aggro range
        if (_chargeTimer > 0f)
        {
            _chargeTimer -= (float)delta;
            Velocity     *= ChargeSpeedMult;    // Amplify velocity set by EnemyAI
        }
        base._PhysicsProcess(delta);
    }

    protected override void OnDamageTaken(int amount, int currentHp)
    {
        base.OnDamageTaken(amount, currentHp);

        // Grunt enters a charge on first hit (enrage)
        if (!_hasCharged)
        {
            _hasCharged  = true;
            _chargeTimer = ChargeDuration;
            AI.ForceAggro();
        }
    }
}
