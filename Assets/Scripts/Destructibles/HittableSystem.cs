using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HittableSystem : MonoBehaviour
{
	[Tooltip("Maximum Amount of Base Bleeding in ml/min, actual Bleeding Rate will be between 0 and bleedingModifier * Hit Damage")]
	[SerializeField] private float bleedingModifier = 0.0f;
	[Tooltip("Maximum Amount of expandable Blood in the System")]
	[SerializeField] private float maxBloodAmount = 0.0f;
	// TODO: Body should contain Bones, Veins, Organs, arms and legs
	private Dictionary<Hittable, SystemPart[]> hittableParts = null;
	private List<Hittable>[] partHittables = null;
	private float bleedingRate = 0.0f;
	private float bloodAmount = 0.0f;
	private MovementController movementController = null;
	private DestructionController destructionController = null;

	public enum SystemPart { Vital, AirSupply, MovementPrimary, MovementSecondary, Aim };

	protected virtual void Start()
	{
		hittableParts = new Dictionary<Hittable, SystemPart[]>();
		partHittables = new List<Hittable>[System.Enum.GetValues(typeof(SystemPart)).Length];
		bloodAmount = maxBloodAmount;
		movementController = gameObject.GetComponent<MovementController>();
		destructionController = gameObject.GetComponent<DestructionController>();

		if(bleedingModifier > 0.0f && bloodAmount > 0.0f)
		{
			StartCoroutine(Bleed());
		}
	}

	public void AddHittable(Hittable hittable, SystemPart[] types)
	{
		hittableParts.Add(hittable, types);
		foreach(SystemPart type in types)
		{
			partHittables[(int) type].Add(hittable);
		}
	}

	public void RegisterHit(Hittable hittable, float damage)
	{
		// TODO: Bandaging/First Aid Time depends on BleedingRate, not on the Amount of separate Wounds (since those are not saved)
		if(damage >= 1.0f)
		{
			bleedingRate += Random.Range(0.0f, damage * bleedingModifier);
		}

		foreach(SystemPart part in hittableParts[hittable])
		{
			if(part == SystemPart.Vital && destructionController != null && hittable.HitPoints <= 0)
			{
				destructionController.Destroy();
			}
			else if(part == SystemPart.AirSupply || part == SystemPart.MovementPrimary || part == SystemPart.MovementPrimary)
			{
				float airSupply = CalculatePartEfficiency(SystemPart.AirSupply);
				float movementPrimary = CalculatePartEfficiency(SystemPart.MovementPrimary);
				float movementSecondary = CalculatePartEfficiency(SystemPart.MovementSecondary);

				movementController.HealthMovementModifier = movementPrimary * airSupply;
				movementController.HealthCrawlModifier = (movementPrimary + movementSecondary) * airSupply;
			}
			else if(part == SystemPart.Aim)
			{
				// TODO: Aiming Penalty (same Style as Movement Penalty, but controlled by Weapon
			}
		}
	}

	private float CalculatePartEfficiency(SystemPart type)
	{
		float current = 0.0f;
		float total = 0.0f;

		foreach(Hittable hittable in partHittables[(int) type])
		{
			current += hittable.HitPoints;
			total += hittable.MaxHitPoints;
		}

		return Mathf.Max(current / total, 0.0f);
	}

	private IEnumerator Bleed()
	{
		while(true)
		{
			yield return new WaitForSeconds(1);

			bloodAmount -= bleedingRate / 60.0f;
			bleedingRate -= 1.0f / 60.0f;
			if(bleedingRate < 0.0f)
			{
				bleedingRate = 0.0f;
			}
		}
	}
}
