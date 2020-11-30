using UnityEngine;

public class Bullet : PoolObject
{
	[Tooltip("List of Layers this Bullet can collide with")]
	[SerializeField] private LayerMask targetMask = new LayerMask();
	[Tooltip("The Damage Multiplier of this Bullet, actual Damage is calculated by multiplying this with the current Impulse of the Bullet at the Time of Impact")]
	[SerializeField] private int damage = 10;
	[Tooltip("The Muzzle Energy of this Bullet, which is used to calculate the Muzzle Velocity by multiplying it by the Muzzle Energy Modifier of the Gun and dividing it by the Bullet Mass")]
	[SerializeField] private float muzzleEnergy = 1.0f;
	[SerializeField] private LineRenderer tracer = null;
	[SerializeField] private float tracerLength = 20.0f;
	private Vector3 spawnPosition = Vector3.zero;
	private Vector3 lastPosition = Vector3.zero;
	private new SimpleRigidbody rigidbody = null;
	private bool drawTracer = false;
	private bool destroyed = false;

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
		Init();
	}

	public override void Init()
	{
		rigidbody.Init();

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
				// Apply Damage
				Hittable target = hit.collider.GetComponentInParent<Hittable>();
				if(target != null)
				{
					target.Hit(hit.collider, hit.point, hit.normal, (rigidbody.Mass * rigidbody.Velocity.magnitude * damage * DamageMod));
				}

				// Change Bullet Position to Impact Point
				transform.position = hit.point;

				// Destroy Bullet
				if(PoolManager != null)
				{
					gameObject.SetActive(false);
					PoolManager.ReturnPoolObject(this);
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

	public Vector3 FireBullet(Vector3 gunVelocity, float spread, float muzzleEnergyModifier)
	{
		// Calculate and apply Bullet Impulse
		Vector3 muzzleImpulse = (transform.forward + ((Random.insideUnitSphere * spread) / 10000.0f)).normalized * (muzzleEnergy * muzzleEnergyModifier);
		rigidbody.ApplyImpulse(muzzleImpulse);
		rigidbody.Velocity += gunVelocity;

		// Return Recoil
		return -muzzleImpulse;
	}
}
