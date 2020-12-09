using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hittable : MonoBehaviour
{
	[SerializeField] private List<Collider> colliders = null;
	[SerializeField] private float hardness = 1.0f;
	[SerializeField] private int maxHitPoints = 100;
	[SerializeField] private AudioClip[] hitSounds = null;
	[SerializeField] private ParticleSystem[] hitParticles = null;
	[Tooltip("Base Chance that this Part will fail instantly when getting hit, this will be multiplied with the Damage of the Hit")]
	[SerializeField] private float critChance = 0.0f;
	private int hitPoints = 0;
	private HittableSystem hittableSystem = null;
	private AudioSource audioSource = null;

	public float Hardness
	{
		get
		{
			return hardness;
		}
	}
	// Annotation: If MaxHitPoints gets a Setter remember to also increment HitPoints, to avoid average Performance of a Part decreasing on MaxHitPoints Increase
	public float MaxHitPoints
	{
		get
		{
			return maxHitPoints;
		}
	}
	public float HitPoints
	{
		get
		{
			return hitPoints;
		}
	}

	protected virtual void Start()
	{
		hitPoints = maxHitPoints;

		hittableSystem = gameObject.GetComponentInParent<HittableSystem>();
		audioSource = gameObject.GetComponent<AudioSource>();
	}

	public virtual void Hit(Collider hitCollider, Vector3 impactPoint, Vector3 surfaceNormal, float damage)
	{
		if(colliders.Contains(hitCollider))
		{
			if(damage >= 1.0f && Random.value < critChance * damage)
			{
				hitPoints = 0;
			}
			else
			{
				hitPoints -= Mathf.RoundToInt(damage);
			}
			if(hittableSystem != null)
			{
				hittableSystem.RegisterHit(this, damage);
			}

			if(audioSource != null && hitSounds != null && hitSounds.Length > 0)
			{
				audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length)]);
			}

			if(hitParticles != null)
			{
				foreach(ParticleSystem hitParticleSystem in hitParticles)
				{
					hitParticleSystem.transform.position = impactPoint;
					hitParticleSystem.transform.rotation = Quaternion.LookRotation(surfaceNormal, Vector3.up);
					hitParticleSystem.Play();
				}
			}
		}
	}
}
