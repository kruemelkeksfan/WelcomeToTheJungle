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

	private void Start()
	{
		magazineIndicator = GameObject.Find("MagazineIndicator")?.GetComponentInChildren<Text>();
		firemodeIndicator = GameObject.Find("FiremodeIndicator")?.GetComponentInChildren<Text>();
	}

	private void Update()
	{
		if(movement != null)
		{
			movement.RotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
			movement.MovementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			movement.SprintInput = Input.GetButton("Sprint");
			movement.JumpInput = Input.GetButton("Jump");
		}

		if(weapon != null)
		{
			if(Input.GetButtonDown("Fire"))
			{
				weapon.pullTrigger();
			}
			if(Input.GetButtonUp("Fire"))
			{
				weapon.releaseTrigger();
			}
			if(Input.GetButtonDown("Aim"))
			{
				weapon.aim();
			}
			if(Input.GetButtonUp("Aim"))
			{
				weapon.unaim();
			}
			if(Input.GetButtonDown("Reload"))
			{
				weapon.reload();
			}
			if(Input.GetButtonDown("Firemode"))
			{
				weapon.switchFireMode();
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
