using UnityEngine;

public class SectorGenerator : MonoBehaviour
{
	[SerializeField] private int width = 100;
	[SerializeField] private int height = 100;
	[SerializeField] private float chunkSize = 1.0f;
	[Tooltip("Determines the Size of Terrain Features, low Values break Unitys PerlinNoise")]
	[SerializeField] private float terrainSmoothness = 10.0f;
	[Tooltip("Determines the Height Difference between Hills and Valleys")]
	[SerializeField] private float terrainSteepness = 10.0f;
	[Tooltip("The Building Blocks of which the Terrain will consist, should be a rectangular GameObject with exactly 1 Collider which must be on the Root GameObject")]
	[SerializeField] private GameObject chunkPrefab = null;
	private NetworkController network = null;

	private void Start()
	{
		network = NetworkController.instance;

		// TODO: Randomness by offsetting x and y in a random Direction (see PerlinNoise Doc)
		if(network == null || network.IsHost)
		{
			for(int y = 0; y < height; ++y)
			{
				for(int x = 0; x < width; ++x)
				{
					GameObject.Instantiate(chunkPrefab, new Vector3(x * chunkSize, Mathf.PerlinNoise((x * chunkSize) / terrainSmoothness, (y * chunkSize) / terrainSmoothness) * terrainSteepness, y * chunkSize), Quaternion.identity);
				}
			}
		}
	}
}
