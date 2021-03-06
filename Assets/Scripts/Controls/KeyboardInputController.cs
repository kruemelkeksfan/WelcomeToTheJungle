﻿using UnityEngine;
using UnityEngine.UI;

public class KeyboardInputController : MonoBehaviour
{
	[SerializeField] private MovementController movement = null;
	[SerializeField] private Weapon weapon = null;
	private Text magazineIndicator = null;
	private Text firemodeIndicator = null;
	private uint id = 0;
	private NetworkController network = null;

	private void Start()
	{
		magazineIndicator = GameObject.Find("MagazineIndicator")?.GetComponentInChildren<Text>();
		firemodeIndicator = GameObject.Find("FiremodeIndicator")?.GetComponentInChildren<Text>();
		network = NetworkController.instance;
		if(network != null)
		{
			id = gameObject.GetComponentInChildren<NetworkObject>().ID;
		}
	}

	private void Update()
	{
		if(movement != null)
		{
			Vector2 newRotation = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
			Vector2 newMovement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			if(newMovement != movement.RotationInput || newRotation != movement.MovementInput)
			{
				network?.SendMovementUpdate(id, newRotation, newMovement);
			}
			movement.RotationInput = newRotation;
			movement.MovementInput = newMovement;

			if(Input.GetButtonDown("Sprint"))
			{
				movement.SprintInput = true;
				network?.SendInput(id, "SprintDown");
			}
			if(Input.GetButtonUp("Sprint"))
			{
				movement.SprintInput = false;
				network?.SendInput(id, "SprintUp");
			}
			if(Input.GetButtonDown("Jump"))
			{
				movement.JumpInput = true;
				network?.SendInput(id, "JumpDown");
			}
			if(Input.GetButtonUp("Jump"))
			{
				movement.JumpInput = false;
				network?.SendInput(id, "JumpUp");
			}
			if(Input.GetButtonDown("Aim"))
			{
				movement.Aim();
				network?.SendInput(id, "AimDown");
			}
			if(Input.GetButtonUp("Aim"))
			{
				movement.Unaim();
				network?.SendInput(id, "AimUp");
			}
			if(Input.GetButtonUp("Crouch"))
			{
				movement.Stance = 1;
				network?.SendInput(id, "Crouch");
			}
			if(Input.GetButtonUp("Crawl"))
			{
				movement.Stance = 2;
				network?.SendInput(id, "Crawl");
			}
		}

		if(weapon != null)
		{
			if(Input.GetButtonDown("Fire"))
			{
				weapon.PullTrigger();
				network?.SendInput(id, "FireDown");
			}
			if(Input.GetButtonUp("Fire"))
			{
				weapon.ReleaseTrigger();
				network?.SendInput(id, "FireUp");
			}
			if(Input.GetButtonDown("Reload"))
			{
				weapon.Reload();
				network?.SendInput(id, "Reload");
			}
			if(Input.GetButtonDown("Firemode"))
			{
				weapon.SwitchFireMode();
				network?.SendInput(id, "Firemode");
			}
		}

		if(magazineIndicator != null)
		{
			weapon.UpdateMagazineReadout(magazineIndicator);
		}
		if(firemodeIndicator != null)
		{
			weapon.UpdateFiremodeReadout(firemodeIndicator);
		}
	}
}
