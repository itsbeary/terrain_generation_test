using Sandbox.Utility;
using System;
public sealed class NoiseMap
{

	private int mapWidth;
	private int mapHeight;

	private int seed;

	public NoiseMap( int seed, int mapWidth, int mapHeight )
	{
		this.seed = seed;
		this.mapWidth = mapWidth;
		this.mapHeight = mapHeight;
	}

	public void GeneratePerlin( Terrain terrain )
	{
		int start = DateTime.Now.Millisecond;
		for ( int y = 0; y < mapHeight; y++ )
			for ( int x = 0; x < mapWidth; x++ )
			{
				float originalPerlin = Noise.Perlin( x + seed, y + seed);

				ushort perlinValue = Convert.ToUInt16( originalPerlin * 100 );
				terrain.TerrainData.SetHeight( x, y, perlinValue );
			}
		Log.Warning( "GeneratePerlin Method took " + (DateTime.Now.Millisecond - start) + "ms to finish" );

	}


	public void GenerateFalloff( Terrain terrain )
	{
		int start = DateTime.Now.Millisecond;
		float halfMapWidthInv = 1.0f / (mapWidth / 2);
		float halfMapHeightInv = 1.0f / (mapHeight / 2);

		for ( int x = 0; x < mapWidth; x++ )
		{
			for ( int y = 0; y < mapHeight; y++ )
			{
				float sampleX = (x - mapWidth / 2) * halfMapWidthInv;
				float sampleY = (y - mapHeight / 2) * halfMapHeightInv;

				float value = CalculateFalloff( sampleX, sampleY );
				float newHeight = terrain.TerrainData.GetHeight( x, y ) * value;
				terrain.TerrainData.SetHeight( x, y, Convert.ToUInt16( newHeight ) );
			}
		}

		Log.Warning( "GenerateFallOff Method took " + (DateTime.Now.Millisecond - start) + "ms to finish" );
	}

	public void ApplySmoothing( Terrain terrain )
	{
		int start = DateTime.Now.Millisecond;
		for ( int x = 1; x < mapWidth - 1; x++ )
			for ( int y = 1; y < mapHeight - 1; y++ )
			{
				// Calculate the average of neighboring height values
				float averageHeight = (
					terrain.TerrainData.GetHeight( x - 1, y - 1 ) +
					terrain.TerrainData.GetHeight( x - 1, y ) +
					terrain.TerrainData.GetHeight( x - 1, y + 1 ) +
					terrain.TerrainData.GetHeight( x, y - 1 ) +
					terrain.TerrainData.GetHeight( x, y ) +
					terrain.TerrainData.GetHeight( x, y + 1 ) +
					terrain.TerrainData.GetHeight( x + 1, y - 1 ) +
					terrain.TerrainData.GetHeight( x + 1, y ) +
					terrain.TerrainData.GetHeight( x + 1, y + 1 )
				) / 9f;

				// Assign the average height to the corresponding position in the temporary array
				terrain.TerrainData.SetHeight( x, y, Convert.ToUInt16( averageHeight ));
			}
		Log.Warning( "ApplySmoothing Method took " + (DateTime.Now.Millisecond - start) + "ms to finish" );
	}

	private float CalculateFalloff( float x, float y )
	{
		float value = Math.Max( Math.Abs( x ), Math.Abs( y ) );
		return 1 - SmoothStep( 0.5f, 1f, value );
	}
	private float SmoothStep( float edge0, float edge1, float x )
	{
		x = MathX.Clamp( (x - edge0) / (edge1 - edge0), 0.0f, 1.0f );
		return x * x * (3 - 2 * x);
	}
}
