using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : PoolObject
{
	[Tooltip("List of Layers this Bullet can collide with")]
	[SerializeField] private LayerMask targetMask = new LayerMask();
	[Tooltip("The Damage Multiplier of this Bullet, actual Damage is calculated by multiplying this with the current Impulse of the Bullet at the Time of Impact")]
	[SerializeField] private int damage = 10;
	[Tooltip("The Muzzle Energy of this Bullet, which is used to calculate the Muzzle Velocity by multiplying it by the Muzzle Energy Modifier of the Gun and dividing it by the Bullet Mass")]
	[SerializeField] private float muzzleEnergy = 1.0f;
	[SerializeField] private float fragmentCountModifier = 1.0f;
	[SerializeField] private float fragmentSpeed = 4.0f;
	[SerializeField] private LineRenderer tracer = null;
	[SerializeField] private float tracerLength = 20.0f;
	[SerializeField] private GameObject fragmentPrefab = null;
	[SerializeField] private AudioClip[] hitSounds = null;
	private Vector3 spawnPosition = Vector3.zero;
	private Vector3 lastPosition = Vector3.zero;
	private new SimpleRigidbody rigidbody = null;
	private bool drawTracer = false;
	private bool destroyed = false;
	private PoolManager fragmentPoolManager = null;

	public int Damage
	{
		get
		{
			return damage;
		}

		private set
		{
			damage = value;
		}
	}
	public float DamageMod { get; set; } = 1.0f;

	private void Awake()
	{
		rigidbody = gameObject.GetComponent<SimpleRigidbody>();
		fragmentPoolManager = new PoolManager ();
		init();
	}

	public override void init()
	{
		rigidbody.init();

		spawnPosition = transform.position;
		lastPosition = transform.position;

		if(tracer != null)
		{
			drawTracer = false;
			tracer.SetPosition(0, transform.position);
			tracer.SetPosition(1, transform.position);
		}

		gameObject.SetActive(true);
	}

	private void FixedUpdate()
	{
		if(!destroyed)
		{
			Vector3 travelledSegment = transform.position - lastPosition;
			RaycastHit hit;
			// TODO: Check for Tag here, too
			if(Physics.Raycast(lastPosition, travelledSegment, out hit, travelledSegment.magnitude, targetMask))
			{
				// Calculate Damage
				int impactDamage = Mathf.CeilToInt(rigidbody.Mass * rigidbody.Velocity.magnitude * damage * DamageMod);

				// Apply Damage
				/*Hittable target = hit.collider.GetComponent<Hittable>();
				if(target != null)
				{
					target.GetDamage(impactDamage);
				}*/

				// Play Hit Sound
				AudioSource audioSource = hit.collider.GetComponent<AudioSource>();
				if(audioSource != null && hitSounds != null && hitSounds.Length > 0)
				{
					audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length - 1)]);
				}

				// Change Bullet Position to Impact Point
				transform.position = hit.point;

				// Spawn Fragments at Impact Point
				travelledSegment = -travelledSegment.normalized;
				if(impactDamage > 0.0f && fragmentPrefab != null)
				{
					int fragmentCount = Mathf.Max(Mathf.FloorToInt(impactDamage * fragmentCountModifier), 1);
					for(int i = 0; i < fragmentCount; ++i)
					{
						// Spawn Fragment and set Speed
						((SimpleRigidbody) fragmentPoolManager.getPoolObject(fragmentPrefab, transform.position, transform.rotation, typeof(SimpleRigidbody))).Velocity = (travelledSegment + Random.insideUnitSphere) * fragmentSpeed;
					}
				}

				// Destroy Bullet
				if(PoolManager != null)
				{
					gameObject.SetActive(false);
					PoolManager.returnPoolObject(this);
				}
				else
				{
					destroyed = true;
					GameObject.Destroy(gameObject, 0.02f);
				}
			}

			lastPosition = transform.position;

			// Make sure, that Tracer Ends never appear inside or behind the Weapon
			if(tracer != null && (drawTracer || (transform.position - spawnPosition).sqrMagnitude > (rigidbody.Velocity.sqrMagnitude * (tracerLength * tracerLength) * (Time.deltaTime * Time.deltaTime))))
			{
				tracer.SetPosition(0, transform.position);
				tracer.SetPosition(1, transform.position - (rigidbody.Velocity * tracerLength * Time.deltaTime));
				drawTracer = true;
			}
		}
	}

	public Vector3 fireBullet(Vector3 gunVelocity, float spread, float muzzleEnergyModifier)
	{
		// Calculate and apply Bullet Impulse
		Vector3 muzzleImpulse = (transform.forward + ((Random.insideUnitSphere * spread) / 10000.0f)).normalized * (muzzleEnergy * muzzleEnergyModifier);
		rigidbody.applyImpulse(muzzleImpulse);
		rigidbody.Velocity += gunVelocity;

		// Return Recoil
		return -muzzleImpulse;
	}
}
