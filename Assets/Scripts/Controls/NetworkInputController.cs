using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
				weapon.pullTrigger();
			}
			if(input == "FireUp")
			{
				weapon.releaseTrigger();
			}
			if(input == "AimDown")
			{
				weapon.aim();
			}
			if(input == "AimUp")
			{
				weapon.unaim();
			}
			if(input == "Reload")
			{
				weapon.reload();
			}
			if(input == "Firemode")
			{
				weapon.switchFireMode();
			}
		}
	}
}
