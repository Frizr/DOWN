using Godot;

/// <summary>
/// EnemyBrute — Agent 3: Enemy AI
/// Concrete enemy type: slow, high-HP tank.
/// Extends EnemyBase.
/// </summary>
public partial class EnemyBrute : EnemyBase
{
    protected override void OnReady()
    {
        // Elite/Tank stats
        MoveSpeed     = 45f;
        KillScore     = 300;
        ContactDamage = 15;
        KnockbackForce = 50f; // Harder to push back

        HP.MaxHealth = 250;
        HP.FullRestore();
    }

    /// <summary>
    /// Brute takes less knockback and doesn't get stunned easily.
    /// </summary>
    protected override void OnDamageTaken(int amount, int currentHp)
    {
        base.OnDamageTaken(amount, currentHp);
        
        // Optional: Force aggro immediately if hit from afar
        AI.ForceAggro();
    }
}
