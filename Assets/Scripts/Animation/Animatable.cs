﻿using System.Collections;
using System.Collections.Generic;
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
			transform.position = Vector3.Lerp(startposition, targetposition, progress);
			transform.rotation = Quaternion.Slerp(startrotation, targetrotation, progress);
		}
	}

	public void Move(int target, float time)
	{
		if(target >= 0 && target < positions.Length && target < rotations.Length)
		{
			startposition = transform.position;
			startrotation = transform.rotation;
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
}
