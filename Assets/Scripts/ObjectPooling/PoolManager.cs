using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
	private Stack<PoolObject> pool = null;

	public PoolManager()
	{
		pool = new Stack<PoolObject>();
	}

	public PoolObject getPoolObject(GameObject prefab, Vector3 position, Quaternion rotation, Type expectedType)
	{
		PoolObject poolObject = null;

		if(pool.Count > 0)
		{
			poolObject = pool.Pop();

			foreach(PoolObject newPoolObject in poolObject.GetComponents<PoolObject>())
			{
				if(newPoolObject.GetType() == expectedType)
				{
					poolObject = newPoolObject;
					break;
				}
			}

			poolObject.transform.position = position;
			poolObject.transform.rotation = rotation;
			poolObject.init();
		}
		else
		{
			GameObject newObject = GameObject.Instantiate(prefab, position, rotation);
			PoolObject[] newPoolObjects = newObject.GetComponents<PoolObject>();
			foreach(PoolObject newPoolObject in newPoolObjects)
			{
				newPoolObject.PoolManager = this;
				if(newPoolObject.GetType() == expectedType)
				{
					poolObject = newPoolObject;
				}
			}
		}

		return poolObject;
	}

	public void returnPoolObject(PoolObject poolObject)
	{
		pool.Push(poolObject);
	}
}
