using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
	[SerializeField] private Weapon weapon = null;

	private Text magazineIndicator = null;
	private Text firemodeIndicator = null;

	private void Start()
	{
		magazineIndicator = GameObject.Find("MagazineIndicator").GetComponentInChildren<Text>();
		firemodeIndicator = GameObject.Find("FiremodeIndicator").GetComponentInChildren<Text>();
	}

	private void Update()
	{
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
