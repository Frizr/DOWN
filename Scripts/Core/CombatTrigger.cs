using Godot;

[GlobalClass]
public partial class CombatTrigger : Node2D
{
	private const float TriggerRange = 50f;

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton)
			return;

		if (mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed)
			return;

		Vector2 mouseWorldPosition = GetGlobalMousePosition();
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is not Node2D enemy)
				continue;

			if (enemy.GlobalPosition.DistanceTo(mouseWorldPosition) >= TriggerRange)
				continue;

			if (GameManager.Instance != null)
				GameManager.Instance.SetState(GameManager.GameState.Playing);
			return;
		}
	}
}
