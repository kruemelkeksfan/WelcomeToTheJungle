using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ErrorController : MonoBehaviour
{
	[SerializeField] private float errorDuration = 4.0f;
	private Text errorPanel = null;
	private IEnumerator resetCoroutine = null;

	private void Start()
	{
		errorPanel = GetComponentInChildren<Text>();
	}

	public void AddError(string error)
	{
		if(errorPanel != null)
		{
			if(resetCoroutine != null)
			{
				StopCoroutine(resetCoroutine);
				resetCoroutine = null;
			}
			errorPanel.text = error;
			resetCoroutine = ResetErrorText();
			StartCoroutine(resetCoroutine);
		}
	}

	private IEnumerator ResetErrorText()
	{
		yield return new WaitForSeconds(errorDuration);
		errorPanel.text = "";
	}
}
