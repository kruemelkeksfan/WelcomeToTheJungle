using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Animator : MonoBehaviour
{
	[SerializeField] private Animatable[] animatables = null;
	[SerializeField] private new string[] animation = null;
	[SerializeField] private bool loop = false;
	private int lineIndex = 0;
	private float time = 0.0f;
	private float passedTime = 0.0f;
	private bool running = false;

	// Animation Format:
	// [0] start <time>
	// [1] <part> <position>
	// [2] <part> <position>
	// [3] start <time>
	// [4] <part> <position>

	private void Start()
	{
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

		if(animation.Length <= 0)
		{
			Debug.LogWarning("Empty Animation " + name + "!");
		}
	}

	private void Update()
	{
		if(running)
		{
			passedTime += Time.deltaTime;
			if(passedTime >= time)
			{
				if(lineIndex >= animation.Length - 1)
				{
					if(loop)
					{
						lineIndex = 0;
					}
					else
					{
						StopAnimation();
						return;
					}
				}

				int separator = animation[lineIndex].IndexOf(" ");
				string keyword = animation[lineIndex].Substring(0, separator);
				string argument = animation[lineIndex].Substring(separator + 1);

				if(keyword == "start")
				{
					if(!float.TryParse(argument, out time)) // TODO: stop Localization
					{
						Debug.LogError("Invalid Time " + argument + " in Animation " + name + "!");
					}
				}
				else
				{
					Debug.LogError("Animation Phase in " + name + " begins with " + keyword + " instead of 'start <time>'!");
				}

				while(lineIndex < animation.Length - 1)
				{
					++lineIndex;
					separator = animation[lineIndex].IndexOf(" ");
					keyword = animation[lineIndex].Substring(0, separator);
					argument = animation[lineIndex].Substring(separator + 1);

					if(keyword == "start")
					{
						break;
					}

					int partid = 0;
					if(int.TryParse(keyword, out partid) && partid >= 0 && partid < animatables.Length)
					{
						int position = 0;
						if(int.TryParse(argument, out position))
						{
							animatables[partid].move(position, time);
						}
						else
						{
							Debug.LogError("Invalid Position " + argument + " in Animation " + name + "!");
						}
					}
					else
					{
						Debug.LogWarning("Unknown animatable Part " + keyword + " in " + name + "!");
					}
				}

				passedTime = 0.0f;
			}
		}
	}

	public void StartAnimation()
	{
		if(!running)
		{
			lineIndex = 0;
			time = 0.0f;
			passedTime = 0.0f;
			running = true;
		}
	}

	public void StopAnimation()
	{
		if(running)
		{
			running = false;

			foreach(Animatable animatable in animatables)
			{
				animatable.stopMovement();
			}
		}	
	}
}
