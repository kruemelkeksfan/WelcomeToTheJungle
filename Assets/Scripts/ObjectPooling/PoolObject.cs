using UnityEngine;

public class PoolObject : MonoBehaviour
{
	public PoolManager PoolManager
	{
		get; set;
	}

	public virtual void Init()
	{

	}
}
