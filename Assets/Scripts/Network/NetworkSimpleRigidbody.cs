using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSimpleRigidbody : NetworkRigidbody
{
    [SerializeField] protected new SimpleRigidbody rigidbody = null;

	private void FixedUpdate()
	{
		float dTime = Time.time - lastUpdate;
        Vector3 predictedPosition = lastPosition + lastVelocity * dTime;
        Vector3 predictedRotation = lastRotation + lastRotationVelocity * dTime;
        if((predictedPosition - rigidbody.transform.position).sqrMagnitude > positionTolerance
            || (predictedRotation - rigidbody.transform.rotation.eulerAngles).sqrMagnitude > rotationTolerance)
		{
            network.SendToHost(); // TODO: New Position and Rotation and Velocities

            lastPosition = rigidbody.transform.position;
            lastRotation = rigidbody.transform.rotation.eulerAngles;
            lastVelocity = rigidbody.Velocity;
            lastRotationVelocity = Vector3.zero;
            lastUpdate = Time.time;
		}
	}
}
