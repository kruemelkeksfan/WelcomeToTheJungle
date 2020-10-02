using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRigidbody : NetworkObject
{
	[SerializeField] private new Rigidbody rigidbody = null;
	[SerializeField] private float positionTolerance = 0.4f;
	[SerializeField] private float rotationTolerance = 10.0f;
	private Vector3 lastPosition = Vector3.zero;
	private Vector3 lastRotation = Vector3.zero;
	private Vector3 lastVelocity = Vector3.zero;
	private Vector3 lastRotationVelocity = Vector3.zero;
	private float lastUpdate = 0.0f;

	protected override void Start()
	{
		base.Start();

		// Use Square to avoid Squareroots in FixedUpdate()
		positionTolerance *= positionTolerance;
	}

	private void FixedUpdate()
	{
		if(network != null && network.IsHost)
		{
			float dTime = Time.time - lastUpdate;
			Vector3 predictedPosition = lastPosition + lastVelocity * dTime;
			Vector3 predictedRotation = lastRotation + lastRotationVelocity * dTime;
			if((predictedPosition - transform.position).sqrMagnitude > positionTolerance
				|| Quaternion.Angle(Quaternion.Euler(predictedRotation), transform.rotation) > rotationTolerance)
			{
				network.SendPositionUpdate(ID, transform.position, transform.rotation.eulerAngles, rigidbody.velocity, rigidbody.angularVelocity);

				lastPosition = rigidbody.transform.position;
				lastRotation = rigidbody.transform.rotation.eulerAngles;
				lastVelocity = rigidbody.velocity;
				lastRotationVelocity = rigidbody.angularVelocity;
				lastUpdate = Time.time;
			}
		}
	}

	public void UpdatePosition(float positionX, float positionY, float positionZ, float rotationX, float rotationY, float rotationZ,
		float velocityX, float velocityY, float velocityZ, float rotationVelocityX, float rotationVelocityY, float rotationVelocityZ)
	{
		transform.position = new Vector3(positionX, positionY, positionZ);
		transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
		rigidbody.velocity = new Vector3(velocityX, velocityY, velocityZ);
		rigidbody.angularVelocity = new Vector3(rotationVelocityX, rotationVelocityY, rotationVelocityZ);
	}
}
