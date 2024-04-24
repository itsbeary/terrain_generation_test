
using Sandbox.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Schema;

public sealed class TerrainManager : Component
{
	
	[Property]
	public Terrain terrain { get; set; }

	private int seed;
	private NoiseMap noiseMap;

	[Property]
	public GameObject grass { get; set; }
	[Property]
	public GameObject tree { get; set; }
	[Property]
	public float grassDensity = 10000f;
	[Property]
	public float treeDensity = 100f;

	private GameObject grassParent;

	[Property]
	public GameObject playerTransform;

	[Property]
	public float grassDistance = 2000f;

	private Dictionary<Vector3, GrassObject> grassCache = new Dictionary<Vector3, GrassObject>();	



	protected override void OnAwake()
	{
		GameObject grassParent = new GameObject( true );
		this.grassParent = grassParent;
		GenerateMap();
	}

	protected override void OnFixedUpdate()
	{
		foreach ( var item in grassCache )
		{
			var distance = item.Key.Distance( playerTransform.Transform.Position );
			var obj = item.Value;

			// Within player distance
			if ( distance < grassDistance )
			{
				if ( obj.gameObject == null )
				{
					obj.gameObject = grass.Clone( item.Key );
					obj.gameObject.SetParent( grassParent );
				}

				if ( !obj.gameObject.Enabled )
					obj.gameObject.Enabled = true;

			}
			// Not within player distance
			else
			{

				if ( obj.exists() )
				{
					if ( obj.gameObject.Enabled )
						obj.gameObject.Enabled = false;
				}
			}
			//Log.Info( "log: " + item.Transform.Position.Distance( playerTransform.Transform.Position ) + item.Enabled);
		}
		//Log.Info( grassCache.Count );
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

		grassParent.Clear();
		GenerateGrass();
		GenerateTrees();
	}

	private void GenerateGrass()
	{
		int start = DateTime.Now.Millisecond;
		int grassCount = 0;

		float terrainSize = terrain.TerrainData.TerrainSize;
		float heightMapSize = terrain.TerrainData.HeightMapSize;

		for ( int g = 0; g < grassDensity; g ++ )
		{
			int x = Game.Random.Int( 0, (int) terrainSize);
			int y = Game.Random.Int( 0, (int) terrainSize );


			SceneTraceResult tr = Scene.Trace.Ray( new Vector3(x, y, 800), new Vector3(x, y, 0 )).WithoutTags("grass").Run();

			if ( tr.Hit )
			{
				var pos = tr.HitPosition;
				if ( !grassCache.ContainsKey( pos ) )
				{

					grassCache.Add( pos, new GrassObject() );
					grassCount++;
				}
			}
		}
		Log.Warning( "GenerateGrass Method took " + (DateTime.Now.Millisecond - start) + "ms to finish with " + grassCount + " grass placed" );
	}

	private void GenerateTrees()
	{
		int start = DateTime.Now.Millisecond;
		int grassCount = 0;

		float terrainSize = terrain.TerrainData.TerrainSize;
		float heightMapSize = terrain.TerrainData.HeightMapSize;

		for ( int g = 0; g < treeDensity; g++ )
		{
			int x = Game.Random.Int( 0, (int)terrainSize );
			int y = Game.Random.Int( 0, (int)terrainSize );


			SceneTraceResult tr = Scene.Trace.Ray( new Vector3( x, y, 800 ), new Vector3( x, y, 0 ) ).WithoutTags( "grass" ).Run();

			if ( tr.Hit )
			{
				var pos = tr.HitPosition;
				tree.Clone( pos ).SetParent( grassParent );
			}
		}
		Log.Warning( "GenerateTrees Method took " + (DateTime.Now.Millisecond - start) + "ms to finish with " + grassCount + " grass placed" );
	}



}
