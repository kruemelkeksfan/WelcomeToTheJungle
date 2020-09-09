using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
	[SerializeField] private int minHeight = 12;
	[SerializeField] private int maxHeight = 23;
	[SerializeField] private float topBranchChance = 0.5f;
	[SerializeField] private int branchMinLength = 1;
	[SerializeField] private int branchMaxLength = 3;
	[SerializeField] private GameObject trunkWidePrefab = null;
	[SerializeField] private GameObject trunkConeWidePrefab = null;
	[SerializeField] private GameObject trunkSlimPrefab = null;
	[SerializeField] private GameObject branchPrefab = null;
	[SerializeField] private GameObject leavesPrefab = null;

	public void Start()
	{
		
	}

	public GameObject GenerateTree(Vector3 position)
	{
		GameObject tree = new GameObject("Tree");
		tree.transform.position = position;

		float trunkHeight = 2.0f;//trunk.GetComponent<Collider>().bounds.size.y; TODO: ...
		float branchWidth = 2.0f;

		int height = Random.Range(minHeight, maxHeight);
		GameObject trunk = tree;
		for(int i = 0; i < height; ++i)
		{
			if(height < (maxHeight * 0.6f) || i > Mathf.CeilToInt(height / 2.0f))
			{
				trunk = GameObject.Instantiate(trunkSlimPrefab, trunk.transform);
			}
			else if(i == Mathf.CeilToInt(height / 2.0f))
			{
				trunk = GameObject.Instantiate(trunkConeWidePrefab, trunk.transform);
			}
			else
			{
				trunk = GameObject.Instantiate(trunkWidePrefab, trunk.transform);
			}
			trunk.transform.Rotate(new Vector3(0.0f, Random.Range(0, 3) * 90.0f, 0.0f));
			if(i != 0)
			{
				trunk.transform.Translate(Vector3.up * trunkHeight);
			}

			float branchChance = (i / (height - 1.0f)) * topBranchChance;
			for(int j = 0; j < 4; ++j)
			{
				if(Random.value < branchChance)
				{
					GameObject branch = trunk;
					int branchLength = Random.Range(branchMinLength, branchMaxLength);
					for(int k = 0; k < branchLength; ++k)
					{
						branch = GameObject.Instantiate(branchPrefab, branch.transform);
						if(k == 0)
						{
							branch.transform.Rotate(new Vector3(0.0f, j * 90.0f, 0.0f));
						}
						else
						{
							branch.transform.Translate(Vector3.left * branchWidth);	
						}
					}
					GameObject leaves = GameObject.Instantiate(leavesPrefab, branch.transform);
					leaves.transform.Translate(Vector3.left * branchWidth);
				}
			}
		}

		return tree;
	}
}
