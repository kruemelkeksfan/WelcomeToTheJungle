using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hittable : MonoBehaviour
{
	[SerializeField] private List<Collider> colliders = null;
	[SerializeField] private float hardness = 1.0f;
	[SerializeField] private AudioClip[] hitSounds = null;
	[SerializeField] private ParticleSystem[] hitParticles = null;
	private AudioSource audioSource = null;

	public float Hardness
	{
		get
		{
			return hardness;
		}
	}

	protected virtual void Start()
	{
		audioSource = gameObject.GetComponent<AudioSource>();
	}

	public virtual void Hit(Collider hitCollider, Vector3 impactPoint, Vector3 surfaceNormal, float damage)
	{
		if(colliders.Contains(hitCollider))
		{
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
