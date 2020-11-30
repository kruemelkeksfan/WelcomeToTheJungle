using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
	[Tooltip("Maximum look down Angle, Front is 0 Degrees, straight down 90 Degrees")]
	[SerializeField] private float maxLookDown = 90.0f;
	[Tooltip("Maximum look up Angle, Front is 0 Degrees, straight up 90 Degrees")]
	[SerializeField] private float maxLookUp = 90.0f;
	[SerializeField] private float rotationSpeed = 400.0f;
	[SerializeField] private float movementSpeed = 10.0f;
	[SerializeField] private float movementAcceleration = 20.0f;
	[Tooltip("Factor by which Sprinting is faster than Walking")]
	[SerializeField] private float sprintFactor = 2.0f;
	[SerializeField] private float jumpStrength = 2000.0f;
	[Tooltip("Minimum Time between 2 Jump Attempts")]
	[SerializeField] private float jumpTime = 0.2f;
	[SerializeField] private float minimumStepHeight = 0.02f;
	[SerializeField] private float maximumStepHeight = 0.4f;
	[Tooltip("Movement Speed Modifier when the Player is neither grounded nor grappled")]
	[SerializeField] private float floatingMovementFactor = 0.002f;
	[Tooltip("Movement Speed Modifier when the Player is grappled, but not grounded")]
	[SerializeField] private float grappledMovementFactor = 0.5f;
	[Tooltip("The maximum Acceleration, that can be applied by standing on another moving Rigidbody")]
	[SerializeField] private float tractionAcceleration = 20.0f;
	[SerializeField] private GameObject head = null;
	[SerializeField] private GrapplingHook grapplingHook = null;
	[Tooltip("A Collider at the Position of the Players Feet, used to check whether the Player is grounded")]
	[SerializeField] private Collider[] feet = null;
	[Tooltip("Array of the different Movement Speed Modifiers for the different available Stances")]
	[SerializeField] private float[] stanceModifiers = { 1.0f, 0.4f, 0.2f };
	[Tooltip("Array of all possible Standing Stances of this Character, should have the same Length as stanceModifiers")]
	[SerializeField] private Animator[] standAnimators = null;
	[Tooltip("Array of all possible Movement Stances of this Character, should have the same Length as stanceModifiers")]
	[SerializeField] private Animator[] moveAnimators = null;
	[Tooltip("Optional Array of Head Positions for different Stances of this Character, Array should have the same Length as stanceModifiers if used, or Length 0 if unused")]
	[SerializeField] private Animator[] headPositions = null;
	[SerializeField] private Animator aimAnimator = null;
	[SerializeField] private Animator unaimAnimator = null;
	private new Rigidbody rigidbody = null;
	private Rigidbody parentRigidbody = null;
	private List<ContactPoint> contactList = null;
	private bool grounded = false;
	private Vector3 slope = Vector3.zero;
	private float lastJump = 0.0f;
	private float jumpCharge = 0.0f;
	private byte stance = 0;
	private float feetDisplacement = 0.0f;
	private float topDisplacement = 0.0f;
	private float stepTime = 0.0f;
	private Vector3 stepForward = Vector3.forward;
	private float stepDelay = 0.2f;

	public Vector2 RotationInput { get; set; } = Vector2.zero;
	public Vector2 MovementInput { get; set; } = Vector2.zero;
	public bool SprintInput { get; set; } = false;
	public bool JumpInput { get; set; } = false;
	public byte Stance
	{
		get
		{
			return stance;
		}

		set
		{
			standAnimators[stance].StopAnimation();
			moveAnimators[stance].StopAnimation();

			if(headPositions != null && headPositions.Length > 0)
			{
				headPositions[stance].StopAnimation();
			}

			if(stance == value)
			{
				stance = 0;
			}
			else
			{
				stance = value;
			}

			if(headPositions != null && headPositions.Length > 0)
			{
				headPositions[stance].StartAnimation();
			}
		}
	}
	public bool MouseVisible { get; set; } = false;

	private void Start()
	{
		rigidbody = gameObject.GetComponent<Rigidbody>();
		contactList = new List<ContactPoint>(64);
		float miny = transform.position.y;
		foreach(Collider foot in feet)
		{
			if(foot.bounds.min.y < miny)
			{
				miny = foot.bounds.min.y;
			}
		}
		feetDisplacement = miny - transform.position.y;

		Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
		for(int i = 0; i < colliders.Length; ++i)
		{
			for(int j = i + 1; j < colliders.Length; ++j)
			{
				Physics.IgnoreCollision(colliders[i], colliders[j]);
			}

			if((colliders[i].bounds.max.y - transform.position.y) > topDisplacement)
			{
				topDisplacement = colliders[i].bounds.max.y - transform.position.y;
			}
		}
	}

	// Remember: Instantanious Input like GetKeyDown is consumed by the first Update() after Key-Press, so it could be missed in FixedUpdate().
	//		Therefore only use GetAxis() and GetButton() in FixedUpdate() and buffer everything else in an Update() Call.
	private void FixedUpdate()
	{
		// Rotation
		if(!MouseVisible)
		{
			Cursor.lockState = CursorLockMode.Locked;

			Vector2 dRotation = new Vector2(-RotationInput.y, RotationInput.x) * rotationSpeed * Time.deltaTime;
			float oldX;
			if(head != null)
			{
				oldX = head.transform.rotation.eulerAngles.x;
			}
			else
			{
				oldX = transform.rotation.eulerAngles.x;
			}
			float newX = oldX + dRotation.x;
			newX = (newX < 0) ? (360.0f - (newX % 360.0f)) : (newX % 360.0f);
			if(newX < 180 && newX > maxLookDown)
			{
				dRotation.x = maxLookDown - oldX;
			}
			else if(newX > 180 && newX < 360 - maxLookUp)
			{
				dRotation.x = (360.0f - maxLookUp) - oldX;
			}

			if(head != null)
			{
				head.transform.rotation = Quaternion.Euler(head.transform.rotation.eulerAngles + new Vector3(dRotation.x, 0.0f, 0.0f));
				transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0.0f, dRotation.y, 0.0f));
			}
			else
			{
				transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(dRotation.x, dRotation.y, 0.0f));
			}
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
		}

		// Calculate Movement
		Vector3 direction = (transform.right * MovementInput.x + slope * MovementInput.y);
		Vector3 tractionVelocityChange = Vector3.zero;
		Vector3 movementVelocityChange = Vector3.zero;
		if(direction != Vector3.zero || (grounded && parentRigidbody == null))
		{
			// Cap Direction Length to 1
			if(direction.sqrMagnitude > 1.0f)
			{
				direction.Normalize();
			}

			// Calculate Speed
			float speed = movementSpeed * stanceModifiers[stance];
			// Sprint Bonus
			if(SprintInput && Vector3.Angle(transform.forward, direction) <= 45.0f)
			{
				speed *= sprintFactor;
			}
			// Slope Penalty
			speed *= 1.0f - slope.y * 0.6f;
			// Avoid unwanted Deceleration
			// TODO: Could get problematic if slow Effects should trigger in Motion
			float sqrRigidbodySpeed = rigidbody.velocity.sqrMagnitude;
			if(sqrRigidbodySpeed > (speed * speed) && Vector3.Angle(rigidbody.velocity, direction) <= 45.0f)
			{
				speed = Mathf.Sqrt(sqrRigidbodySpeed);
			}

			// Calculate Acceleration
			float acceleration = movementAcceleration;
			// Ground and Grappling Bonus
			if(!grounded)
			{
				if(grapplingHook != null && grapplingHook.Hooked)
				{
					acceleration *= grappledMovementFactor;
				}
				else
				{
					acceleration *= floatingMovementFactor;
				}
			}

			// Calculate Acceleration
			movementVelocityChange = CalculateAcceleratedVelocityChange(((direction * speed) - rigidbody.velocity), acceleration);
		}
		// Calculate Traction
		if(parentRigidbody != null)
		{
			tractionVelocityChange = CalculateAcceleratedVelocityChange(parentRigidbody.velocity - rigidbody.velocity, tractionAcceleration);
		}
		//Apply Movement
		rigidbody.AddForce(movementVelocityChange + tractionVelocityChange, ForceMode.VelocityChange);

		// Walking Animation
		if(standAnimators.Length > Stance && moveAnimators.Length > Stance)
		{
			if(direction != Vector3.zero)
			{
				standAnimators[Stance].StopAnimation();
				moveAnimators[Stance].StartAnimation();
			}
			else
			{
				moveAnimators[Stance].StopAnimation();
				standAnimators[Stance].StartAnimation();
			}
		}

		// Step forward to finish automatic Step up from previous Frame
		if(stepTime > 0.0f && Time.time >= stepTime)
		{
			rigidbody.AddForce(stepForward, ForceMode.VelocityChange);
			stepTime = 0.0f;
		}
		if(stepTime <= 0 && stepTime > -stepDelay)
		{
			stepTime -= Time.deltaTime;                 // Measure Time since last Step
		}

		// Jumping
		if(JumpInput && stance == 0)
		{
			if((Time.time - lastJump) >= jumpTime && grounded)
			{
				lastJump = Time.time;
				jumpCharge = 0.0f;
				rigidbody.AddForce(Vector3.up * jumpStrength * Time.deltaTime, ForceMode.Impulse);
			}
			else
			{
				if(jumpCharge < jumpTime)
				{
					jumpCharge += Time.deltaTime;
					rigidbody.AddForce(transform.up * jumpStrength * Time.deltaTime, ForceMode.Impulse);
				}
			}
		}
		else
		{
			jumpCharge = jumpTime;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Step up if Step is low enough
		StepUp(collision);
	}

	private void OnCollisionStay(Collision collision)
	{
		// Step up if Step is low enough
		StepUp(collision);
		// TODO: Maybe just check for Y-Component of Collision Normals?
		if(!grounded)
		{
			if(stance == 0)
			{
				int contactCount = collision.GetContacts(contactList);
				float maxMass = parentRigidbody != null ? parentRigidbody.mass : 0.0f;
				for(int i = 0; i < contactCount; ++i)
				{
					// Get grounded, when you stay longer than 1 Frame on a Collider
					foreach(Collider foot in feet)
					{
						if(contactList[i].thisCollider.Equals(foot))
						{
							grounded = true;
							if(contactList[i].otherCollider.attachedRigidbody != null && contactList[i].otherCollider.attachedRigidbody.mass > maxMass)
							{
								parentRigidbody = contactList[i].otherCollider.attachedRigidbody;
								maxMass = parentRigidbody.mass;
							}

							break;
						}
					}
				}
			}
			else
			{
				grounded = true;
			}
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		grounded = false;
		if(collision.rigidbody == parentRigidbody)
		{
			parentRigidbody = null;
		}
	}

	public virtual void Aim()
	{
		if(aimAnimator != null && unaimAnimator != null)
		{
			unaimAnimator.StopAnimation();
			aimAnimator.StartAnimation();
		}
	}

	public virtual void Unaim()
	{
		if(aimAnimator != null && unaimAnimator != null)
		{
			aimAnimator.StopAnimation();
			unaimAnimator.StartAnimation();
		}
	}

	private Vector3 CalculateAcceleratedVelocityChange(Vector3 targetVelocity, float acceleration)
	{
		if(targetVelocity.sqrMagnitude > Mathf.Pow(acceleration * Time.deltaTime, 2.0f))
		{
			targetVelocity = targetVelocity.normalized * acceleration * Time.deltaTime;
		}

		return targetVelocity;
	}

	private void StepUp(Collision collision)
	{
		if(MovementInput != Vector2.zero)
		{
			slope = transform.forward;
			// TODO: Find out why the heck ContactList sometimes is not set here
			if(contactList == null)
			{
				contactList = new List<ContactPoint>(64);
				Debug.LogWarning("ContactList in MovementController was null!");
			}
			int contactCount = collision.GetContacts(contactList);
			for(int i = 0; i < contactCount; ++i)
			{
				Vector3 currentSlope = Vector3.Cross(transform.right, contactList[i].normal);
				if(currentSlope.y > slope.y)
				{
					slope = currentSlope;
				}
			}
			slope = slope.normalized;

			if(Vector3.Angle(slope, transform.forward) > 50.0f)
			{
				if(stepTime < -stepDelay)                                                                                                                                       // Previous Step must be complete
				{
					for(int i = 0; i < contactCount; ++i)
					{
						if(contactList[i].point.y > (transform.position.y + feetDisplacement + 0.02f))                                                                          // Is it actually an upward Step?
						{
							Vector3 stepStart = new Vector3(transform.position.x, (transform.position.y + feetDisplacement + maximumStepHeight), transform.position.z);
							Vector3 stepTarget = new Vector3(contactList[i].point.x, (transform.position.y + feetDisplacement + maximumStepHeight), contactList[i].point.z);
							Vector3 stepDirection = stepTarget - stepStart;
							if(rigidbody.velocity == Vector3.zero || Vector3.Angle(rigidbody.velocity, stepDirection) <= 90.0f)                                                 // Is the Step actually in the Way of the Player?
							{
								if(!Physics.Raycast(stepStart, stepDirection, stepDirection.magnitude + 0.02f))                                                                 // Is the Path for the Step clear and the Step itself not too high?
								{
									RaycastHit hit;
									if(Physics.Raycast(new Ray(stepTarget, Vector3.down), out hit, maximumStepHeight))                                                          // How high is the Step exactly?
									{
										float stepHeight = hit.point.y - (transform.position.y + feetDisplacement);                                                             // Calculate exact Step height
										if(stepHeight >= minimumStepHeight)                                                                                                     // Is the Step high enough (to avoid jittery Movement on Slopes)?
										{
											rigidbody.velocity = ((Vector3.up * (2.0f + (stepHeight * 4.0f))) + (-stepDirection.normalized));                                   // Reset Velocity and apply Height dependent upward Force
											stepTime = Time.time + stepHeight * 0.4f;                                                                                           // Height dependent Delay for stepping forward
											stepForward = stepDirection.normalized * (2.0f + (stepHeight * 2.0f));                                                              // Height dependent Step forward Direction
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}

