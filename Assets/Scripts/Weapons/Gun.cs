using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Gun : Weapon
{
	[Tooltip("Base Damage of this Weapon")]
	[SerializeField] private float damage = 1.0f;
	[Tooltip("Maximum Deviation from Point of Aim in cm at a Target Distance of 100m")]
	[SerializeField] private float spread = 50.0f;
	[Tooltip("Angle by which the Gun is rotated at most per Shot in Degrees upwards")]
	[SerializeField] private float verticalRecoil = 0.4f;
	[Tooltip("Angle by which the Gun is rotated at most per Shot in Degrees to a random Side")]
	[SerializeField] private float horizontalRecoil = 0.2f;
	[Tooltip("The Fraction of the current accumulated Recoil which is recovered per Second")]
	[SerializeField] private float recoilResetFactor = 1.0f;
	[SerializeField] private int roundsPerMinute = 600;
	[SerializeField] private int magazineCapacity = 1;
	[SerializeField] private float reloadTime = 2.0f;
	[Tooltip("A Modifier which is applied at the Muzzle Energy of each Bullet and should mainly depend on Barrel Length and Action Type")]
	[SerializeField] private float muzzleEnergyModifier = 1.0f;
	[Tooltip("The initial Velocity of ejected Casings")]
	[SerializeField] private Vector3 casingVelocity = Vector3.right;
	[Tooltip("Determines how erratic Casings are ejected, a Value of 0.0 means, that every casing is ejected with the same Velocity")]
	[SerializeField] private float casingDeviation = 2.0f;
	[SerializeField] private Vector3 aimPosition = Vector3.zero;
	[Tooltip("A GameObject which has its Center at the Muzzle Point of the Weapon to determine where Bullets will be spawned")]
	[SerializeField] private Transform muzzle = null;
	[Tooltip("A GameObject which has its Center at the Ejection Port of the Weapon to determine where ejected Casings will be spawned")]
	[SerializeField] private Transform ejectionPort = null;
	[Tooltip("Available Burst Counts, the first Element is the default Firemode, 0 means full-auto")]
	[SerializeField] private int[] fireModes = { 0, 3, 1 };
	[SerializeField] private GameObject bulletPrefab = null;
	[SerializeField] private GameObject casingPrefab = null;
	[SerializeField] private AudioClip fireSound = null;
	private Vector3 hipPosition = Vector3.zero;
	private Quaternion originalRotation = Quaternion.identity;
	private float roundsPerMinuteMod = 1.0f;
	private float timePerRound = 1.0f;
	private float lastShot = 0.0f;
	private int shotCount = 0;
	private float verticalAccumulatedRecoil = 0.0f;
	private float horizontalAccumulatedRecoil = 0.0f;
	private float reloadStarted = 0.0f;
	private AudioSource audioSource = null;
	private new Rigidbody rigidbody = null;
	private bool fire = false;
	private int fireMode = 0;
	private int shotsFired = 0;
	private bool safety = false;
	private bool disengageSafety = false;
	private PoolManager bulletPoolManager = null;
	private PoolManager casingPoolManager = null;

	public float Damage
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
	public float Spread
	{
		get
		{
			return spread;
		}
		private set
		{
			spread = value;
		}
	}
	public float VerticalRecoil
	{
		get
		{
			return verticalRecoil;
		}
		private set
		{
			verticalRecoil = value;
		}
	}
	public float HorizontalRecoil
	{
		get
		{
			return horizontalRecoil;
		}
		private set
		{
			horizontalRecoil = value;
		}
	}
	public int RoundsPerMinute
	{
		get
		{
			return roundsPerMinute;
		}
		private set
		{
			roundsPerMinute = value;
			timePerRound = 1.0f / ((RoundsPerMinute * RoundsPerMinuteMod) / 60.0f);
		}
	}
	public int MagazineCapacity
	{
		get
		{
			return magazineCapacity;
		}
		private set
		{
			magazineCapacity = value;
		}
	}
	public float ReloadTime
	{
		get
		{
			return reloadTime;
		}
		private set
		{
			reloadTime = value;
		}
	}
	public float MuzzleEnergyModifier
	{
		get
		{
			return muzzleEnergyModifier;
		}
		private set
		{
			muzzleEnergyModifier = value;
		}
	}
	public float DamageMod { get; set; } = 1.0f;
	public float SpreadMod { get; set; } = 1.0f;
	public float RecoilMod { get; set; } = 1.0f;
	public float RoundsPerMinuteMod
	{
		get
		{
			return roundsPerMinuteMod;
		}
		set
		{
			roundsPerMinuteMod = value;
			timePerRound = 1.0f / ((RoundsPerMinute * RoundsPerMinuteMod) / 60.0f);
		}
	}
	public float MagazineCapacityMod { get; set; } = 1.0f;
	public float ReloadTimeMod { get; set; } = 1.0f;
	public float MuzzleEnergyModifierMod { get; set; } = 1.0f;
	public bool ReadyToFire { get; private set; } = false;
	public override bool Safety
	{
		get
		{
			return safety;
		}

		set
		{
			if(value == true)
			{
				safety = true;
			}
			else
			{
				disengageSafety = true;
			}
		}
	}

	private void Start()
	{
		originalRotation = transform.localRotation;
		timePerRound = 1.0f / ((RoundsPerMinute * RoundsPerMinuteMod) / 60.0f);
		hipPosition = transform.localPosition;
		audioSource = gameObject.GetComponent<AudioSource>();
		rigidbody = transform.root.gameObject.GetComponentInChildren<Rigidbody>();
		bulletPoolManager = new PoolManager();
		casingPoolManager = new PoolManager();
	}

	private void FixedUpdate()
	{
		// Auto Reload
		if(shotCount <= 0 && reloadStarted < 0)
		{
			reloadStarted = Time.time;
		}

		// Finish Reload after reloadTime
		if(reloadStarted >= 0 && Time.time - reloadStarted >= (ReloadTime * ReloadTimeMod))
		{
			reloadStarted = -1;
			shotCount = Mathf.RoundToInt(MagazineCapacity * MagazineCapacityMod);
		}

		if(!safety && reloadStarted < 0 && (Time.time - lastShot) >= timePerRound && shotCount > 0)
		{
			ReadyToFire = true;
		}
		else
		{
			ReadyToFire = false;
		}

		// Fire Weapon
		if(fire && ReadyToFire)
		{
			// Update Counters
			lastShot = Time.time;
			--shotCount;
			++shotsFired;

			// Fire Bullet
			Bullet bullet = (Bullet) bulletPoolManager.getPoolObject(bulletPrefab, muzzle.position, muzzle.rotation, typeof(Bullet));
			Vector3 recoilImpulse = bullet.fireBullet(rigidbody != null ? rigidbody.velocity : Vector3.zero, Spread * SpreadMod, MuzzleEnergyModifier * MuzzleEnergyModifierMod);
			bullet.DamageMod = Damage * DamageMod;

			// Recoil
			if(rigidbody != null)
			{
				rigidbody.AddForce(recoilImpulse, ForceMode.Impulse);
			}
			float recoilStrength = recoilImpulse.magnitude;
			verticalAccumulatedRecoil += VerticalRecoil * RecoilMod * recoilStrength * Random.Range(0.0f, 1.0f);
			horizontalAccumulatedRecoil += HorizontalRecoil * RecoilMod * recoilStrength * Random.Range(-1.0f, 1.0f);
			transform.localRotation *= Quaternion.AngleAxis(verticalAccumulatedRecoil, Vector3.left);
			transform.localRotation *= Quaternion.AngleAxis(horizontalAccumulatedRecoil, Vector3.up);

			// Eject Casing
			if(ejectionPort != null && casingPrefab != null)
			{
				SimpleRigidbody casing = (SimpleRigidbody) casingPoolManager.getPoolObject(casingPrefab, ejectionPort.position, ejectionPort.rotation, typeof(SimpleRigidbody));
				casing.Velocity = ejectionPort.rotation * (casingVelocity + (Random.insideUnitSphere * casingDeviation));
				if(rigidbody != null)
				{
					casing.Velocity += rigidbody.velocity;
				}
			}

			// Firemode Limitations
			if(fireModes[fireMode] != 0 && shotsFired >= fireModes[fireMode])
			{
				fire = false;
			}

			// Fire Sound
			if(fireSound != null)
			{
				audioSource.clip = fireSound;
				audioSource.Play();
			}
		}

		// Recenter Weapon
		verticalAccumulatedRecoil -= verticalAccumulatedRecoil * (recoilResetFactor * Time.deltaTime);
		horizontalAccumulatedRecoil -= horizontalAccumulatedRecoil * (recoilResetFactor * Time.deltaTime);
		float recoilAngle = Quaternion.Angle(transform.localRotation, originalRotation);
		transform.localRotation = Quaternion.RotateTowards(transform.localRotation, originalRotation, recoilAngle * recoilResetFactor * Time.deltaTime);

		// Toggle Safety
		// Complex Checking with multiple Variables to avoid firing Shots in the Frame in which the Safety is toggled
		if(disengageSafety)
		{
			safety = false;
			disengageSafety = false;
		}
	}

	// TODO: Remove and replace by actual Animation
	public override void aim()
	{
		transform.localPosition = aimPosition;
	}
	public override void unaim()
	{
		transform.localPosition = hipPosition;
	}

	public override void pullTrigger()
	{
		fire = true;
		shotsFired = 0;
	}

	public override void releaseTrigger()
	{
		fire = false;
	}

	public override void reload()
	{
		if(reloadStarted < 0)
		{
			reloadStarted = Time.time;
			shotCount = 0;
		}
	}

	public override void switchFireMode(int fireMode = -1)
	{
		if(fireMode >= 0)
		{
			this.fireMode = fireMode;
		}
		else
		{
			this.fireMode = (this.fireMode + 1) % fireModes.Length;
		}
	}

	public override void updateMagazineReadout(Text magazineIndicator)
	{
		if(magazineIndicator != null)
		{
			if(reloadStarted < 0)
			{
				magazineIndicator.text = shotCount + "/" + (MagazineCapacity * MagazineCapacityMod);
				magazineIndicator.alignment = TextAnchor.LowerRight;
			}
			else
			{
				string text = "Reload";
				int lettercount = (int)(((Time.time - reloadStarted) / (ReloadTime * ReloadTimeMod)) * (text.Length + 1));  // Add a virtual Letter to let the full Word appear for longer than 1 Frame
				text = text.Substring(0, Mathf.Clamp(lettercount, 0, text.Length));                                         // Subtract virtual Letter

				magazineIndicator.text = text;
				magazineIndicator.alignment = TextAnchor.LowerLeft;
			}
		}
	}

	public override void updateFiremodeReadout(Text firemodeIndicator)
	{
		if(firemodeIndicator != null)
		{
			if(fireModes[this.fireMode] == 0)
			{
				firemodeIndicator.text = "Auto";
			}
			else if(fireModes[this.fireMode] == 1)
			{
				firemodeIndicator.text = "Semi";
			}
			else
			{
				firemodeIndicator.text = fireModes[this.fireMode] + "-Burst";
			}
		}
	}
}
