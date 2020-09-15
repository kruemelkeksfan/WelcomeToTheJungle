using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRigidbody : NetworkObject
{
    [SerializeField] protected new Rigidbody rigidbody = null;
    [SerializeField] protected float positionTolerance = 0.4f;
    [SerializeField] protected float rotationTolerance = 10.0f;
    [SerializeField] protected float networkUpdateInterval = 0.2f;
    protected Vector3 lastPosition = Vector3.zero;
    protected Vector3 lastRotation = Vector3.zero;
    protected Vector3 lastVelocity = Vector3.zero;
    protected Vector3 lastRotationVelocity = Vector3.zero;
    protected float lastUpdate = 0.0f;

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
            lastVelocity = rigidbody.velocity;
            lastRotationVelocity = rigidbody.angularVelocity;
            lastUpdate = Time.time;
		}
	}
}
