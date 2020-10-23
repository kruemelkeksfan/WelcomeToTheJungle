using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
	[Tooltip("Maximum look down Angle, Front is 0/360 Degrees, straight down 90/360 Degrees")]
	[SerializeField] private float maxLookDown = 90.0f;
	[Tooltip("Maximum look up Angle, Front is 0/360 Degrees, straight up 270/360 Degrees")]
	[SerializeField] private float maxLookUp = 270.0f;
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
	[SerializeField] private Animator standAnimator = null;
	[SerializeField] private Animator walkAnimator = null;
	[SerializeField] private Animator aimAnimator = null;
	[SerializeField] private Animator unaimAnimator = null;
	private new Rigidbody rigidbody = null;
	private Rigidbody parentRigidbody = null;
	private List<ContactPoint> contactList = null;
	private bool grounded = false;
	private Vector3 slope = Vector3.zero;
	private float lastJump = 0.0f;
	private float jumpCharge = 0.0f;
	private float feetDisplacement = 0.0f;
	private float topDisplacement = 0.0f;
	private float stepTime = 0.0f;
	private Vector3 stepForward = Vector3.forward;
	private float stepDelay = 0.2f;

	public Vector2 RotationInput { get; set; } = Vector2.zero;
	public Vector2 MovementInput { get; set; } = Vector2.zero;
	public bool SprintInput { get; set; } = false;
	public bool JumpInput { get; set; } = false;
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
		if(!MouseVisible)
		{
			Cursor.lockState = CursorLockMode.Locked;

			// Rotation
			Vector3 rotation = transform.rotation.eulerAngles;

			if(head != null)
			{
				rotation.x = head.transform.rotation.eulerAngles.x;
			}

			rotation.x += -RotationInput.y * rotationSpeed * Time.deltaTime;
			rotation.y += RotationInput.x * rotationSpeed * Time.deltaTime;

			if(rotation.x < 180 && rotation.x > maxLookDown)
			{
				rotation.x = maxLookDown;
			}
			else if(rotation.x > 180 && rotation.x < maxLookUp)
			{
				rotation.x = maxLookUp;
			}

			if(head != null)
			{
				Vector3 oldHeadRotation = head.transform.localRotation.eulerAngles;
				Vector3 oldRotation = transform.localRotation.eulerAngles;
				head.transform.localRotation = Quaternion.Euler(new Vector3(rotation.x, oldHeadRotation.y, oldHeadRotation.z));
				transform.localRotation = Quaternion.Euler(new Vector3(oldRotation.x, rotation.y, oldRotation.z));
			}
			else
			{
				transform.localRotation = Quaternion.Euler(rotation);
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
			float speed = movementSpeed;
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
			movementVelocityChange = CalculateAcceleration(((direction * speed) - rigidbody.velocity), acceleration);
		}
		// Calculate Traction
		if(parentRigidbody != null)
		{
			tractionVelocityChange = CalculateAcceleration(parentRigidbody.velocity - rigidbody.velocity, tractionAcceleration);
		}
		//Apply Movement
		rigidbody.AddForce(movementVelocityChange + tractionVelocityChange, ForceMode.VelocityChange);

		// Walking Animation
		if(standAnimator != null && walkAnimator != null)
		{
			if(direction != Vector3.zero)
			{
				standAnimator.StopAnimation();
				walkAnimator.StartAnimation();
			}
			else
			{
				walkAnimator.StopAnimation();
				standAnimator.StartAnimation();
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
		if(JumpInput)
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

	// Step up if Step is low enough
	private void OnCollisionEnter(Collision collision)
	{
		StepUp(collision);
	}

	// Only get grounded, when you stay longer than 1 Frame on a Collider
	private void OnCollisionStay(Collision collision)
	{
		StepUp(collision);

		// TODO: Maybe just check for Y-Component of Collision Normals?
		if(!grounded)
		{
			int contactCount = collision.GetContacts(contactList);
			float maxMass = parentRigidbody != null ? parentRigidbody.mass : 0.0f;
			for(int i = 0; i < contactCount; ++i)
			{
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
		if(aimAnimator != null)
		{
			aimAnimator.StartAnimation();
		}
	}

	public virtual void Unaim()
	{
		if(unaimAnimator != null)
		{
			unaimAnimator.StartAnimation();
		}
	}

	private Vector3 CalculateAcceleration(Vector3 targetVelocity, float acceleration)
	{
		float changeSqrMagnitude = targetVelocity.sqrMagnitude;
		if(changeSqrMagnitude > Mathf.Pow(acceleration * Time.deltaTime, 2.0f))
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

