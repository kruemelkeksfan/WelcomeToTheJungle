using UnityEngine;

public class NetworkSimpleRigidbody : NetworkObject
{
	[SerializeField] private new SimpleRigidbody rigidbody = null;
	[SerializeField] private float positionTolerance = 0.4f;
	[SerializeField] private float networkUpdateInterval = 0.2f;
	private Vector3 lastPosition = Vector3.zero;
	private Vector3 lastVelocity = Vector3.zero;
	private float lastUpdate = 0.0f;

	private void FixedUpdate()
	{
		float dTime = Time.time - lastUpdate;
		Vector3 predictedPosition = lastPosition + lastVelocity * dTime;
		if((predictedPosition - rigidbody.transform.position).sqrMagnitude > positionTolerance)
		{
			//network.SendToHost(); // TODO: New Position and Rotation and Velocities

			lastPosition = rigidbody.transform.position;
			lastVelocity = rigidbody.Velocity;
			lastUpdate = Time.time;
		}
	}
}
