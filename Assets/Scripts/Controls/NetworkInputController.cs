using UnityEngine;

public class NetworkInputController : MonoBehaviour
{
	[SerializeField] private MovementController movement = null;
	[SerializeField] private Weapon weapon = null;

	private void Start()
	{

	}

	public void UpdateMovement(float rotationX, float rotationY, float movementX, float movementY)
	{
		movement.RotationInput = new Vector2(rotationX, rotationY);
		movement.MovementInput = new Vector2(movementX, movementY);
	}

	public void ProcessInput(string input)
	{
		if(movement != null)
		{
			if(input == "SprintDown")
			{
				movement.SprintInput = true;
			}
			if(input == "SprintUp")
			{
				movement.SprintInput = false;
			}
			if(input == "JumpDown")
			{
				movement.JumpInput = true;
			}
			if(input == "JumpUp")
			{
				movement.JumpInput = false;
			}
		}

		if(weapon != null)
		{
			if(input == "FireDown")
			{
				weapon.PullTrigger();
			}
			if(input == "FireUp")
			{
				weapon.ReleaseTrigger();
			}
			if(input == "AimDown")
			{
				weapon.Aim();
			}
			if(input == "AimUp")
			{
				weapon.Unaim();
			}
			if(input == "Reload")
			{
				weapon.Reload();
			}
			if(input == "Firemode")
			{
				weapon.SwitchFireMode();
			}
		}
	}
}
