using Godot;

/// <summary>
/// Handles procedural animation (wobble/squash/stretch) for single-sprite characters.
/// Perfect for "Brotato / Vampire Survivors" style rendering without needing 8-directional sprite sheets.
/// </summary>
public partial class TweenAnimator : Node
{
    [Export] public Sprite2D TargetSprite;
    [Export] public CharacterBody2D Body;

    [Export] public float WobbleSpeed = 15f;
    [Export] public float WobbleIntensity = 0.08f;
    [Export] public float TiltIntensity = 0.15f;
    [Export] public float LerpSpeed = 12f;

    private float _time = 0f;
    private Vector2 _baseScale;

    public override void _Ready()
    {
        if (TargetSprite != null)
            _baseScale = TargetSprite.Scale;
    }

    public override void _Process(double delta)
    {
        if (TargetSprite == null || Body == null) return;

        float dt = (float)delta;
        bool isMoving = Body.Velocity.LengthSquared() > 10f;

        if (isMoving)
        {
            _time += dt * WobbleSpeed;
            float wobbleX = Mathf.Sin(_time) * WobbleIntensity;
            float wobbleY = Mathf.Cos(_time * 2f) * WobbleIntensity; // Figure 8 bounce

            // Flip sprite based on movement direction
            float facingDir = Mathf.Sign(Body.Velocity.X);
            if (Mathf.Abs(Body.Velocity.X) < 1f) facingDir = Mathf.Sign(_baseScale.X); // keep current if moving vertically

            TargetSprite.Scale = new Vector2(Mathf.Abs(_baseScale.X) * facingDir, _baseScale.Y) 
                                 + new Vector2(wobbleX, wobbleY);

            // Tilt in the direction of horizontal movement
            float targetTilt = Mathf.Sign(Body.Velocity.X) * TiltIntensity;
            if (Mathf.Abs(Body.Velocity.X) < 1f) targetTilt = 0f; // Don't tilt if moving purely vertical

            TargetSprite.Rotation = Mathf.Lerp(TargetSprite.Rotation, targetTilt, dt * LerpSpeed);
        }
        else
        {
            _time = 0f;
            TargetSprite.Scale = TargetSprite.Scale.Lerp(_baseScale, dt * LerpSpeed);
            TargetSprite.Rotation = Mathf.Lerp(TargetSprite.Rotation, 0f, dt * LerpSpeed);
        }
    }
}
