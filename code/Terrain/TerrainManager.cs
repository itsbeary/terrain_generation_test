
using System;

public sealed class TerrainManager : Component
{
	
	[Property]
	public Terrain terrain { get; set; }

	private int seed;
	private NoiseMap noiseMap;

	protected override void OnAwake()
	{
		GenerateMap();
	}

	public void GenerateMap()
	{
		terrain.Reset();
		int heightSizeHalf = (int)terrain.TerrainData.HeightMapSize;
		this.seed = (int)new Random().Int( 0, int.MaxValue - 1 ) / 100;
		Log.Info( seed );
		terrain.TerrainSize = 22500;
		terrain.TerrainHeight = 315999;
		noiseMap = new NoiseMap( seed, heightSizeHalf, heightSizeHalf );
		noiseMap.GeneratePerlin( terrain );
		noiseMap.GenerateFalloff( terrain );
		for(int i = 0; i < 5; i++ )
			noiseMap.ApplySmoothing( terrain );
		terrain.SyncHeightMap();

	}

}
