using UnityEngine;

public class Animatable : MonoBehaviour
{
	[SerializeField] private Vector3[] positions = null;
	[SerializeField] private Vector3[] rotations = null;
	private Vector3 startposition = Vector3.zero;
	private Quaternion startrotation = Quaternion.identity;
	private Vector3 targetposition = Vector3.zero;
	private Quaternion targetrotation = Quaternion.identity;
	private float passedtime = 0.0f;
	private float time = 0.0f;

	private void Update()
	{
		if(passedtime < time)
		{
			passedtime += Time.deltaTime;

			float progress = passedtime / time;
			transform.localPosition = Vector3.Lerp(startposition, targetposition, progress);
			transform.localRotation = Quaternion.Slerp(startrotation, targetrotation, progress);
		}
	}

	public void Move(int target, float time)
	{
		if(target >= 0 && target < positions.Length && target < rotations.Length)
		{
			startposition = transform.localPosition;
			startrotation = transform.localRotation;
			targetposition = positions[target];
			targetrotation = Quaternion.Euler(rotations[target]);

			passedtime = 0.0f;
			this.time = time;
		}
		else
		{
			Debug.LogError("Invalid Target Position " + target + " for " + name + "!");
		}
	}

	public void StopMovement()
	{
		this.time = 0.0f;
	}
}
