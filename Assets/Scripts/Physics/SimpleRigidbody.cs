using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SimpleRigidbody : PoolObject
{
	[SerializeField] private float mass = 1.0f;
	[SerializeField] private float gravity = 9.81f;
	[SerializeField] private float drag = 0.2f;
	[Tooltip("How much Velocity is retained when this Object hits another Collider")]
	[SerializeField] private float bounciness = 0.5f;
	[Tooltip("Time until from Spawn until automatic Destruction of this Object, 0 means unlimited")]
	[SerializeField] private float lifetime = 10.0f;

	private float spawnTime = 0.0f;

	public float Mass
	{
		get
		{
			return mass;
		}
		set
		{
			mass = value;
		}
	}
	public Vector3 Velocity { get; set; } = Vector3.zero;

	private void Awake()
	{
		init();
	}

	public override void init()
	{
		spawnTime = Time.time;
		Velocity = Vector3.zero;

		gameObject.SetActive(true);
	}

	private void FixedUpdate()
	{
		if(lifetime <= 0 || (Time.time - spawnTime < lifetime))
		{
			// Update Position
			transform.position += Velocity * Time.deltaTime;

			// Apply Drag
			Velocity *= 1.0f - Mathf.Min((Velocity.sqrMagnitude * drag * Time.deltaTime), 1.0f);

			// Apply Gravity
			Velocity += new Vector3(0.0f, -gravity * Time.deltaTime, 0.0f);
		}
		else
		{
			// Destroy Fragment
			if(PoolManager != null)
			{
				gameObject.SetActive(false);
				PoolManager.returnPoolObject(this);
			}
			else
			{
				GameObject.Destroy(gameObject, 0.02f);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		float sqrSpeed = Velocity.sqrMagnitude;
		if(sqrSpeed >= 2.0f && (Time.time - spawnTime) > 0.2f)
		{
			Velocity = -Velocity * bounciness;
		}
		else if(sqrSpeed < 2.0f && (lifetime - (Time.time - spawnTime)) > 1.0f)
		{
			// If the SimpleRigidbody has more than 1 Second Lifetime left, accelerate its Demise
			lifetime = (Time.time - spawnTime) + 1.0f;
		}
	}

	public void applyImpulse(Vector3 impulse)
	{
		if(mass > 0.0f)
		{
			Velocity += impulse / mass;
		}
		else
		{
			Debug.LogError("Trying to apply an Impulse on a SimpleRigidbody without Mass!");
		}
	}
}
