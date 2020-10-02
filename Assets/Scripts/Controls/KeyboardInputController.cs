using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardInputController : MonoBehaviour
{
	[SerializeField] private MovementController movement = null;
	[SerializeField] private Weapon weapon = null;
	private Text magazineIndicator = null;
	private Text firemodeIndicator = null;
	private NetworkController network = null;

	private void Start()
	{
		magazineIndicator = GameObject.Find("MagazineIndicator")?.GetComponentInChildren<Text>();
		firemodeIndicator = GameObject.Find("FiremodeIndicator")?.GetComponentInChildren<Text>();
		network = NetworkController.instance;
	}

	private void Update()
	{
		if(movement != null)
		{
			Vector2 newMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
			Vector2 newRotation = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			if(newMovement != movement.RotationInput || newRotation != movement.MovementInput)
			{
				network?.SendMovementUpdate(movement.RotationInput, movement.MovementInput);
			}
			movement.RotationInput = newMovement;
			movement.MovementInput = newRotation;

			if(Input.GetButtonDown("Sprint"))
			{
				movement.SprintInput = true;
				network?.SendInput("SprintDown");
			}
			if(Input.GetButtonUp("Sprint"))
			{
				movement.SprintInput = false;
				network?.SendInput("SprintUp");
			}
			if(Input.GetButtonDown("Jump"))
			{
				movement.JumpInput = true;
				network?.SendInput("JumpDown");
			}
			if(Input.GetButtonUp("Jump"))
			{
				movement.JumpInput = false;
				network?.SendInput("JumpUp");
			}
		}

		if(weapon != null)
		{
			if(Input.GetButtonDown("Fire"))
			{
				weapon.pullTrigger();
				network?.SendInput("FireDown");
			}
			if(Input.GetButtonUp("Fire"))
			{
				weapon.releaseTrigger();
				network?.SendInput("FireUp");
			}
			if(Input.GetButtonDown("Aim"))
			{
				weapon.aim();
				network?.SendInput("AimDown");
			}
			if(Input.GetButtonUp("Aim"))
			{
				weapon.unaim();
				network?.SendInput("AimUp");
			}
			if(Input.GetButtonDown("Reload"))
			{
				weapon.reload();
				network?.SendInput("Reload");
			}
			if(Input.GetButtonDown("Firemode"))
			{
				weapon.switchFireMode();
				network?.SendInput("Firemode");
			}
		}

		if(magazineIndicator != null)
		{
			weapon.updateMagazineReadout(magazineIndicator);
		}
		if(firemodeIndicator != null)
		{
			weapon.updateFiremodeReadout(firemodeIndicator);
		}
	}
}
