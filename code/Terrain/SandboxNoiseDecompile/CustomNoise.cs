public static class Noise
{
	private static FastNoise perlin;

	static Noise()
	{
		perlin = new FastNoise( 5633 );
		perlin.SetNoiseType( FastNoise.NoiseType.Perlin );
		perlin.SetFrequency( 0.03f );
	}

	private static float ConvertRange( float f )
	{
		return (1f + f) / 2f;
	}
	public static float Perlin( float x, float y = 0f )
	{
		return ConvertRange( perlin.GetNoise( x, y ) );
	}
	public static void PerlinSetSeed(int seed)
	{
		perlin.SetSeed( seed );
	}
	public static int PerlinGetSeed()
	{
		return perlin.GetSeed();
	}
	public static float Perlin( float x, float y, float z )
	{
		return ConvertRange( perlin.GetNoise( x, y, z ) );
	}


}
