using System;
using System.Runtime.CompilerServices;

internal class FastNoise
{
	public enum NoiseType
	{
		Value,
		ValueFractal,
		Perlin,
		PerlinFractal,
		Simplex,
		SimplexFractal,
		Cellular,
		WhiteNoise,
		Cubic,
		CubicFractal
	}

	public enum Interp
	{
		Linear,
		Hermite,
		Quintic
	}

	public enum FractalType
	{
		FBM,
		Billow,
		RigidMulti
	}

	public enum CellularDistanceFunction
	{
		Euclidean,
		Manhattan,
		Natural
	}

	public enum CellularReturnType
	{
		CellValue,
		NoiseLookup,
		Distance,
		Distance2,
		Distance2Add,
		Distance2Sub,
		Distance2Mul,
		Distance2Div
	}

	private struct Float2
	{
		public readonly float x;

		public readonly float y;

		public Float2( float x, float y )
		{
			this.x = x;
			this.y = y;
		}
	}

	private struct Float3
	{
		public readonly float x;

		public readonly float y;

		public readonly float z;

		public Float3( float x, float y, float z )
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	private const short FN_INLINE = 256;

	private const int FN_CELLULAR_INDEX_MAX = 3;

	private int m_seed = 1337;

	private float m_frequency = 0.01f;

	private Interp m_interp = Interp.Quintic;

	private NoiseType m_noiseType = NoiseType.Simplex;

	private int m_octaves = 3;

	private float m_lacunarity = 2f;

	private float m_gain = 0.5f;

	private FractalType m_fractalType = FractalType.FBM;

	private float m_fractalBounding;

	private CellularDistanceFunction m_cellularDistanceFunction = CellularDistanceFunction.Euclidean;

	private CellularReturnType m_cellularReturnType = CellularReturnType.CellValue;

	private FastNoise m_cellularNoiseLookup = null;

	private int m_cellularDistanceIndex0 = 0;

	private int m_cellularDistanceIndex1 = 1;

	private float m_cellularJitter = 0.45f;

	private float m_gradientPerturbAmp = 1f;

	private static readonly Float2[] GRAD_2D = new Float2[8]
	{
		new Float2(-1f, -1f),
		new Float2(1f, -1f),
		new Float2(-1f, 1f),
		new Float2(1f, 1f),
		new Float2(0f, -1f),
		new Float2(-1f, 0f),
		new Float2(0f, 1f),
		new Float2(1f, 0f)
	};

	private static readonly Float3[] GRAD_3D = new Float3[16]
	{
		new Float3(1f, 1f, 0f),
		new Float3(-1f, 1f, 0f),
		new Float3(1f, -1f, 0f),
		new Float3(-1f, -1f, 0f),
		new Float3(1f, 0f, 1f),
		new Float3(-1f, 0f, 1f),
		new Float3(1f, 0f, -1f),
		new Float3(-1f, 0f, -1f),
		new Float3(0f, 1f, 1f),
		new Float3(0f, -1f, 1f),
		new Float3(0f, 1f, -1f),
		new Float3(0f, -1f, -1f),
		new Float3(1f, 1f, 0f),
		new Float3(0f, -1f, 1f),
		new Float3(-1f, 1f, 0f),
		new Float3(0f, -1f, -1f)
	};

	private static readonly Float2[] CELL_2D = new Float2[256]
	{
		new Float2(-0.2700222f, -0.9628541f),
		new Float2(0.386309266f, -0.9223693f),
		new Float2(0.04444859f, -0.9990117f),
		new Float2(-0.599252343f, -0.800560236f),
		new Float2(-0.781928f, 0.62336874f),
		new Float2(0.9464672f, 0.322799921f),
		new Float2(-0.6514147f, -0.7587219f),
		new Float2(0.937847257f, 0.347048372f),
		new Float2(-0.8497876f, -0.527125239f),
		new Float2(-0.879042566f, 0.476743251f),
		new Float2(-0.8923003f, -0.451442361f),
		new Float2(-0.379844427f, -0.9250504f),
		new Float2(-0.9951651f, 0.09821638f),
		new Float2(0.7724398f, -0.635088f),
		new Float2(0.757328331f, -0.6530343f),
		new Float2(-0.9928005f, -0.119780056f),
		new Float2(-0.05326657f, 0.998580337f),
		new Float2(0.975425363f, -0.220330074f),
		new Float2(-0.766501844f, 0.642242134f),
		new Float2(0.9916367f, 0.129060611f),
		new Float2(-0.994696856f, 0.102850378f),
		new Float2(-0.537920535f, -0.8429955f),
		new Float2(0.502281547f, -0.864704132f),
		new Float2(0.455982149f, -0.8899889f),
		new Float2(-0.8659131f, -0.50019443f),
		new Float2(0.08794584f, -0.9961253f),
		new Float2(-0.5051685f, 0.8630207f),
		new Float2(0.7753185f, -0.6315704f),
		new Float2(-0.692194462f, 0.72171104f),
		new Float2(-0.519165933f, -0.854673445f),
		new Float2(0.8978623f, -0.4402764f),
		new Float2(-0.170677409f, 0.985326946f),
		new Float2(-0.935343f, -0.353742063f),
		new Float2(-0.999240458f, 0.0389674678f),
		new Float2(-0.2882064f, -0.9575683f),
		new Float2(-0.966381133f, 0.2571138f),
		new Float2(-0.875971437f, -0.482363015f),
		new Float2(-0.8303123f, -0.557298362f),
		new Float2(0.0511013381f, -0.998693466f),
		new Float2(-0.855837345f, -0.517245054f),
		new Float2(0.0988702551f, 0.9951003f),
		new Float2(0.9189016f, 0.394486785f),
		new Float2(-0.243937582f, -0.969790936f),
		new Float2(-0.812140942f, -0.5834613f),
		new Float2(-0.99104315f, 0.133542135f),
		new Float2(0.8492424f, -0.528003156f),
		new Float2(-0.9717839f, -0.235872954f),
		new Float2(0.9949457f, 0.100414209f),
		new Float2(0.6241065f, -0.7813392f),
		new Float2(0.6629103f, 0.748698831f),
		new Float2(-0.7197418f, 0.6942418f),
		new Float2(-0.8143371f, -0.580392241f),
		new Float2(0.104521051f, -0.9945227f),
		new Float2(-0.10659261f, -0.99430275f),
		new Float2(0.445799679f, -0.8951328f),
		new Float2(0.105547406f, 0.99441427f),
		new Float2(-0.9927903f, 0.119864449f),
		new Float2(-0.833436668f, 0.552615047f),
		new Float2(0.9115562f, -0.4111756f),
		new Float2(0.8285545f, -0.55990845f),
		new Float2(0.7217098f, -0.6921958f),
		new Float2(0.494049281f, -0.8694339f),
		new Float2(-0.36523214f, -0.9309165f),
		new Float2(-0.9696607f, 0.244454846f),
		new Float2(0.0892550945f, -0.9960088f),
		new Float2(0.5354071f, -0.8445941f),
		new Float2(-0.105357617f, 0.9944344f),
		new Float2(-0.989028454f, 0.1477251f),
		new Float2(0.004856105f, 0.9999882f),
		new Float2(0.988559842f, 0.150829136f),
		new Float2(0.928612947f, -0.371049821f),
		new Float2(-0.5832394f, -0.8123003f),
		new Float2(0.301520765f, 0.9534596f),
		new Float2(-0.957511067f, 0.288396567f),
		new Float2(0.9715802f, -0.236710548f),
		new Float2(0.2299818f, 0.973194957f),
		new Float2(0.9557638f, -0.2941352f),
		new Float2(0.7409561f, 0.671553433f),
		new Float2(-0.9971514f, -0.07542631f),
		new Float2(0.69057107f, -0.7232645f),
		new Float2(-0.2907137f, -0.9568101f),
		new Float2(0.5912778f, -0.80646795f),
		new Float2(-0.945459247f, -0.3257405f),
		new Float2(0.666445553f, 0.7455537f),
		new Float2(0.6236135f, 0.781732857f),
		new Float2(0.9126994f, -0.408631653f),
		new Float2(-0.8191762f, 0.573541939f),
		new Float2(-0.8812746f, -0.4726046f),
		new Float2(0.995331347f, 0.09651673f),
		new Float2(0.985565066f, -0.169296965f),
		new Float2(-0.8495981f, 0.527430654f),
		new Float2(0.6174854f, -0.786582351f),
		new Float2(0.850815654f, 0.5254643f),
		new Float2(0.998503268f, -0.0546925f),
		new Float2(0.197137162f, -0.980375946f),
		new Float2(0.660785556f, -0.7505747f),
		new Float2(-0.0309749413f, 0.9995202f),
		new Float2(-0.6731661f, 0.739491343f),
		new Float2(-0.719501853f, -0.694490552f),
		new Float2(0.972751141f, 0.2318516f),
		new Float2(0.9997059f, -0.02425069f),
		new Float2(0.442178756f, -0.896926939f),
		new Float2(0.9981351f, -0.0610436723f),
		new Float2(-0.9173661f, -0.398044556f),
		new Float2(-0.81500566f, -0.579453f),
		new Float2(-0.878933132f, 0.476945f),
		new Float2(0.0158605836f, 0.999874234f),
		new Float2(-0.8095465f, 0.5870558f),
		new Float2(-0.9165899f, -0.399828672f),
		new Float2(-0.8023543f, 0.5968481f),
		new Float2(-0.5176738f, 0.855578065f),
		new Float2(-0.8154407f, -0.578840554f),
		new Float2(0.402201027f, -0.915551364f),
		new Float2(-0.9052557f, -0.4248672f),
		new Float2(0.7317446f, 0.681579f),
		new Float2(-0.564763248f, -0.825253f),
		new Float2(-0.8403276f, -0.542078853f),
		new Float2(-0.931428134f, 0.363925248f),
		new Float2(0.523819864f, 0.851829052f),
		new Float2(0.7432804f, -0.66898f),
		new Float2(-0.9853716f, -0.170419738f),
		new Float2(0.460146874f, 0.887842834f),
		new Float2(0.8258554f, 0.563881934f),
		new Float2(0.6182366f, 0.785992f),
		new Float2(0.833150268f, -0.553046644f),
		new Float2(0.150030747f, 0.9886813f),
		new Float2(-0.6623304f, -0.7492119f),
		new Float2(-0.668598652f, 0.743623435f),
		new Float2(0.7025606f, 0.7116239f),
		new Float2(-0.541938961f, -0.840417862f),
		new Float2(-0.338861644f, 0.9408362f),
		new Float2(0.833153f, 0.553042531f),
		new Float2(-0.29897207f, -0.954261839f),
		new Float2(0.2638523f, 0.9645631f),
		new Float2(0.124108739f, -0.9922686f),
		new Float2(-0.7282649f, -0.6852957f),
		new Float2(0.69625f, 0.717799366f),
		new Float2(-0.918353558f, 0.395761f),
		new Float2(-0.6326102f, -0.7744703f),
		new Float2(-0.9331892f, -0.35938552f),
		new Float2(-0.115377933f, -0.993321657f),
		new Float2(0.9514975f, -0.307656556f),
		new Float2(-0.08987977f, -0.9959526f),
		new Float2(0.6678497f, 0.7442962f),
		new Float2(0.795240045f, -0.6062947f),
		new Float2(-0.6462007f, -0.7631675f),
		new Float2(-0.273359865f, 0.961911857f),
		new Float2(0.966959f, -0.254931837f),
		new Float2(-0.9792895f, 0.202465191f),
		new Float2(-0.5369503f, -0.843613863f),
		new Float2(-0.270036459f, -0.9628501f),
		new Float2(-0.6400277f, 0.768351853f),
		new Float2(-0.785453737f, -0.6189204f),
		new Float2(0.0600590557f, -0.9981948f),
		new Float2(-0.0245577041f, 0.9996984f),
		new Float2(-0.659836233f, 0.7514095f),
		new Float2(-0.625389457f, -0.7803128f),
		new Float2(-0.6210409f, -0.7837782f),
		new Float2(0.8348889f, 0.550418556f),
		new Float2(-0.15922752f, 0.9872419f),
		new Float2(0.836762249f, 0.547566354f),
		new Float2(-0.8675754f, -0.4973057f),
		new Float2(-0.202266261f, -0.97933054f),
		new Float2(0.939919f, 0.341397554f),
		new Float2(0.987740457f, -0.1561049f),
		new Float2(-0.903445542f, 0.428702831f),
		new Float2(0.126980424f, -0.9919052f),
		new Float2(-0.3819601f, 0.924178839f),
		new Float2(0.9754626f, 0.220165253f),
		new Float2(-0.320401579f, -0.947281837f),
		new Float2(-0.9874761f, 0.157768741f),
		new Float2(0.0253534839f, -0.999678552f),
		new Float2(0.4835131f, -0.8753371f),
		new Float2(-0.28508f, -0.9585037f),
		new Float2(-0.06805516f, -0.997681558f),
		new Float2(-0.7885244f, -0.615003467f),
		new Float2(0.3185392f, -0.9479097f),
		new Float2(0.8880043f, 0.459835142f),
		new Float2(0.647692144f, -0.761902153f),
		new Float2(0.982024133f, 0.188755423f),
		new Float2(0.935727537f, -0.352723718f),
		new Float2(-0.889489532f, 0.456955522f),
		new Float2(0.7922791f, 0.6101588f),
		new Float2(0.748381853f, 0.663268149f),
		new Float2(-0.728893f, -0.684627652f),
		new Float2(0.8729033f, -0.487893283f),
		new Float2(0.8288346f, 0.5594937f),
		new Float2(0.08074567f, 0.996734738f),
		new Float2(0.979914844f, -0.1994165f),
		new Float2(-0.5807307f, -0.814095736f),
		new Float2(-0.470004976f, -0.8826638f),
		new Float2(0.2409493f, 0.9705377f),
		new Float2(0.9437817f, -0.330569416f),
		new Float2(-0.892799854f, -0.45045355f),
		new Float2(-0.806962252f, 0.590603054f),
		new Float2(0.0625897348f, 0.998039365f),
		new Float2(-0.931259751f, 0.364355981f),
		new Float2(0.577744961f, 0.816217363f),
		new Float2(-0.3360096f, -0.9418586f),
		new Float2(0.697932065f, -0.716163933f),
		new Float2(-0.00200815732f, -0.999998f),
		new Float2(-0.182729438f, -0.983163238f),
		new Float2(-0.6523912f, 0.7578824f),
		new Float2(-0.430262685f, -0.9027037f),
		new Float2(-0.9985126f, -0.0545209125f),
		new Float2(-0.0102810217f, -0.999947131f),
		new Float2(-0.494607121f, 0.869116664f),
		new Float2(-0.299935f, 0.953959644f),
		new Float2(0.8165472f, 0.5772787f),
		new Float2(0.269746035f, 0.9629315f),
		new Float2(-0.7306287f, -0.682774961f),
		new Float2(-0.7590952f, -0.650979638f),
		new Float2(-0.9070538f, 0.4210146f),
		new Float2(-0.5104861f, -0.859886f),
		new Float2(0.861335039f, 0.5080373f),
		new Float2(0.500788152f, -0.8655699f),
		new Float2(-0.6541582f, 0.7563578f),
		new Float2(-0.838275552f, -0.54524684f),
		new Float2(0.6940071f, 0.7199682f),
		new Float2(0.06950936f, 0.9975813f),
		new Float2(0.170294225f, -0.9853933f),
		new Float2(0.269597322f, 0.9629731f),
		new Float2(0.551961243f, -0.833869755f),
		new Float2(0.2256575f, -0.9742067f),
		new Float2(0.421526283f, -0.9068162f),
		new Float2(0.488187343f, -0.872738838f),
		new Float2(-0.3683855f, -0.929673135f),
		new Float2(-0.982539058f, 0.18605645f),
		new Float2(0.812564731f, 0.582871f),
		new Float2(0.3196461f, -0.947537f),
		new Float2(0.9570914f, 0.289786249f),
		new Float2(-0.6876655f, -0.7260276f),
		new Float2(-0.9988771f, -0.04737673f),
		new Float2(-0.1250179f, 0.9921545f),
		new Float2(-0.828013361f, 0.560708344f),
		new Float2(0.932486355f, -0.361205131f),
		new Float2(0.639465332f, 0.7688199f),
		new Float2(-0.0162384715f, -0.999868155f),
		new Float2(-0.995501459f, -0.0947461352f),
		new Float2(-0.8145332f, 0.580117f),
		new Float2(0.4037328f, -0.914876938f),
		new Float2(0.9944263f, 0.10543368f),
		new Float2(-0.16247116f, 0.9867133f),
		new Float2(-0.9949488f, -0.100383878f),
		new Float2(-0.699530244f, 0.714603f),
		new Float2(0.5263415f, -0.850273252f),
		new Float2(-0.5395222f, 0.8419714f),
		new Float2(0.65793705f, 0.7530729f),
		new Float2(0.014267588f, -0.9998982f),
		new Float2(-0.6734384f, 0.7392433f),
		new Float2(0.6394121f, -0.7688642f),
		new Float2(0.9211571f, 0.389190853f),
		new Float2(-0.146637216f, -0.98919034f),
		new Float2(-0.7823181f, 0.6228791f),
		new Float2(-0.5039611f, -0.8637264f),
		new Float2(-0.774312f, -0.632804f)
	};

	private static readonly Float3[] CELL_3D = new Float3[256]
	{
		new Float3(-0.7292737f, -0.661843956f, 0.17355819f),
		new Float3(0.7902921f, -0.5480887f, -0.2739291f),
		new Float3(0.7217579f, 0.622621238f, -0.3023381f),
		new Float3(0.5656831f, -0.8208298f, -0.079000026f),
		new Float3(0.760049045f, -0.555597961f, -0.337099969f),
		new Float3(0.371394575f, 0.501126468f, 0.78162545f),
		new Float3(-0.127706245f, -0.4254439f, -0.8959289f),
		new Float3(-0.2881561f, -0.5815839f, 0.7607406f),
		new Float3(0.5849561f, -0.6628202f, -0.4674352f),
		new Float3(0.330717117f, 0.0391653739f, 0.94291687f),
		new Float3(0.8712122f, -0.411337435f, -0.267938167f),
		new Float3(0.580981f, 0.7021916f, 0.411567777f),
		new Float3(0.5037569f, 0.6330057f, -0.5878204f),
		new Float3(0.449371219f, 0.6013902f, 0.6606023f),
		new Float3(-0.6878404f, 0.0901889056f, -0.7202372f),
		new Float3(-0.595895648f, -0.646935046f, 0.475797653f),
		new Float3(-0.5127052f, 0.1946922f, -0.836198747f),
		new Float3(-0.991150737f, -0.0541027635f, -0.121215314f),
		new Float3(-0.214972109f, 0.9720882f, -0.09397608f),
		new Float3(-0.7518651f, -0.542805731f, 0.374246955f),
		new Float3(0.5237069f, 0.8516377f, -0.0210781787f),
		new Float3(0.6333505f, 0.192616716f, -0.749510467f),
		new Float3(-0.06788242f, 0.39983058f, 0.9140719f),
		new Float3(-0.55386287f, -0.472989678f, -0.6852129f),
		new Float3(-0.726145566f, -0.5911991f, 0.350993335f),
		new Float3(-0.9229275f, -0.178280875f, 0.341204941f),
		new Float3(-0.6968815f, 0.651127458f, 0.300648034f),
		new Float3(0.960804462f, -0.209836319f, -0.18117249f),
		new Float3(0.0681714639f, -0.9743405f, 0.214506909f),
		new Float3(-0.3577285f, -0.6697087f, -0.650784552f),
		new Float3(-0.186862111f, 0.7648617f, -0.616497457f),
		new Float3(-0.654169738f, 0.3967915f, 0.643908739f),
		new Float3(0.699334f, -0.6164538f, 0.361823916f),
		new Float3(-0.154666573f, 0.6291284f, 0.7617583f),
		new Float3(-0.6841613f, -0.2580482f, -0.682154238f),
		new Float3(0.5383981f, 0.4258655f, 0.727163f),
		new Float3(-0.5026988f, -0.7939833f, -0.3418837f),
		new Float3(0.320297182f, 0.283441544f, 0.9039196f),
		new Float3(0.86832273f, -0.000376265642f, -0.495999515f),
		new Float3(0.791120052f, -0.0851104558f, 0.605710566f),
		new Float3(-0.04011016f, -0.439724863f, 0.8972364f),
		new Float3(0.914512f, 0.357934624f, -0.188548759f),
		new Float3(-0.961203933f, -0.275648415f, 0.0102466689f),
		new Float3(0.651036143f, -0.287779927f, -0.702377856f),
		new Float3(-0.204178631f, 0.736523747f, 0.6448596f),
		new Float3(-0.7718264f, 0.379062682f, 0.5104856f),
		new Float3(-0.306008279f, -0.7692988f, 0.56083715f),
		new Float3(0.454007328f, -0.5024843f, 0.735789955f),
		new Float3(0.481679559f, 0.6021208f, -0.636738f),
		new Float3(0.696198046f, -0.322219729f, 0.6414692f),
		new Float3(-0.653216064f, -0.6781149f, 0.336851567f),
		new Float3(0.508930147f, -0.615466237f, -0.601823449f),
		new Float3(-0.163591981f, -0.9133605f, -0.372840881f),
		new Float3(0.5240802f, -0.8437664f, 0.115750588f),
		new Float3(0.5902587f, 0.4983818f, -0.634988368f),
		new Float3(0.5863228f, 0.494764745f, 0.6414308f),
		new Float3(0.6779335f, 0.234134525f, 0.6968409f),
		new Float3(0.7177054f, -0.685897946f, 0.120178632f),
		new Float3(-0.532882f, -0.5205125f, 0.6671608f),
		new Float3(-0.8654874f, -0.07007271f, -0.4960054f),
		new Float3(-0.286181f, 0.795208931f, 0.534549534f),
		new Float3(-0.0484952964f, 0.981083632f, -0.187411562f),
		new Float3(-0.635852158f, 0.605834842f, 0.478180021f),
		new Float3(0.62547946f, -0.286161959f, 0.725869656f),
		new Float3(-0.258526f, 0.506194949f, -0.8227582f),
		new Float3(0.0213630684f, 0.506401658f, -0.862033f),
		new Float3(0.200111777f, 0.859926343f, 0.46955505f),
		new Float3(0.474356145f, 0.6014985f, -0.6427953f),
		new Float3(0.6622994f, -0.520247459f, -0.539168f),
		new Float3(0.08084973f, -0.653272033f, 0.7527941f),
		new Float3(-0.6893687f, 0.0592860356f, 0.7219805f),
		new Float3(-0.112188712f, -0.967318535f, 0.227395251f),
		new Float3(0.7344116f, 0.59796685f, -0.3210533f),
		new Float3(0.5789393f, -0.248884976f, 0.776457f),
		new Float3(0.698818266f, 0.355716974f, -0.6205791f),
		new Float3(-0.863684535f, -0.274877131f, -0.4224826f),
		new Float3(-0.4247028f, -0.464088082f, 0.777335048f),
		new Float3(0.5257723f, -0.842701733f, 0.115832992f),
		new Float3(0.934383035f, 0.316302478f, -0.163954392f),
		new Float3(-0.101683639f, -0.8057303f, -0.583488762f),
		new Float3(-0.6529239f, 0.506021261f, -0.5635893f),
		new Float3(-0.246528611f, -0.9668206f, -0.06694497f),
		new Float3(-0.9776897f, -0.209925056f, -0.00736882538f),
		new Float3(0.7736893f, 0.573424459f, 0.2694238f),
		new Float3(-0.6095088f, 0.4995679f, 0.6155737f),
		new Float3(0.5794535f, 0.7434547f, 0.333929241f),
		new Float3(-0.8226211f, 0.0814258158f, 0.562729359f),
		new Float3(-0.510385454f, 0.470366776f, 0.719904f),
		new Float3(-0.5764972f, -0.0723165646f, -0.813892663f),
		new Float3(0.7250629f, 0.39499715f, -0.56414634f),
		new Float3(-0.1525424f, 0.486084074f, -0.8604958f),
		new Float3(-0.55509764f, -0.495782077f, 0.6678823f),
		new Float3(-0.188361436f, 0.914586961f, 0.35784173f),
		new Float3(0.762555659f, -0.541440845f, -0.354048967f),
		new Float3(-0.5870232f, -0.3226498f, -0.7424964f),
		new Float3(0.305112422f, 0.2262544f, -0.9250488f),
		new Float3(0.637957633f, 0.577242434f, -0.509707034f),
		new Float3(-0.5966776f, 0.145485237f, -0.7891831f),
		new Float3(-0.65833056f, 0.655548751f, -0.369941473f),
		new Float3(0.743489265f, 0.235108465f, 0.6260573f),
		new Float3(0.5562114f, 0.826436043f, -0.08736329f),
		new Float3(-0.302894f, -0.8251527f, 0.476841927f),
		new Float3(0.112934381f, -0.9858884f, -0.123571075f),
		new Float3(0.5937653f, -0.5896814f, 0.5474657f),
		new Float3(0.6757964f, -0.583575845f, -0.450264841f),
		new Float3(0.7242303f, -0.115271978f, 0.679855049f),
		new Float3(-0.9511914f, 0.0753624f, -0.299258083f),
		new Float3(0.2539471f, -0.188633934f, 0.9486454f),
		new Float3(0.5714336f, -0.167945087f, -0.8032796f),
		new Float3(-0.06778235f, 0.39782694f, 0.9149532f),
		new Float3(0.6074973f, 0.73306f, -0.305892259f),
		new Float3(-0.543547869f, 0.167582244f, 0.8224791f),
		new Float3(-0.5876678f, -0.3380045f, -0.7351187f),
		new Float3(-0.796756268f, 0.0409782268f, -0.602909863f),
		new Float3(-0.199635088f, 0.8706295f, 0.4496111f),
		new Float3(-0.0278766025f, -0.910623252f, -0.4122962f),
		new Float3(-0.7797626f, -0.6257635f, 0.0197577551f),
		new Float3(-0.5211233f, 0.740164459f, -0.424955457f),
		new Float3(0.8575425f, 0.4053273f, -0.316750169f),
		new Float3(0.104522333f, 0.8390196f, -0.533967435f),
		new Float3(0.3501823f, 0.9242524f, -0.152085021f),
		new Float3(0.198784992f, 0.0764761344f, 0.9770547f),
		new Float3(0.784599662f, 0.6066257f, -0.128096417f),
		new Float3(0.09006737f, -0.975098968f, -0.20265691f),
		new Float3(-0.827434361f, -0.542299569f, 0.145820364f),
		new Float3(-0.348579764f, -0.41580227f, 0.8400004f),
		new Float3(-0.2471779f, -0.730482f, -0.6366311f),
		new Float3(-0.3700155f, 0.8577948f, 0.356758446f),
		new Float3(0.591339469f, -0.548311949f, -0.591330349f),
		new Float3(0.120487355f, -0.7626472f, -0.6354935f),
		new Float3(0.6169593f, 0.03079648f, 0.7863923f),
		new Float3(0.12581569f, -0.664083f, -0.73699677f),
		new Float3(-0.6477565f, -0.174014732f, -0.741707742f),
		new Float3(0.6217889f, -0.7804431f, -0.06547655f),
		new Float3(0.6589943f, -0.6096988f, 0.44044736f),
		new Float3(-0.268983752f, -0.6732403f, -0.688763559f),
		new Float3(-0.38497752f, 0.567654252f, 0.7277094f),
		new Float3(0.57544446f, 0.811047137f, -0.105196349f),
		new Float3(0.914159358f, 0.3832948f, 0.131900564f),
		new Float3(-0.107925318f, 0.9245494f, 0.365459353f),
		new Float3(0.3779771f, 0.304314882f, 0.874371648f),
		new Float3(-0.214288518f, -0.8259286f, 0.5214617f),
		new Float3(0.580254436f, 0.414809853f, -0.7008834f),
		new Float3(-0.198266089f, 0.856716156f, -0.476159662f),
		new Float3(-0.0338155366f, 0.377318084f, -0.9254661f),
		new Float3(-0.686792254f, -0.6656598f, 0.29191336f),
		new Float3(0.7731743f, -0.287579358f, -0.565243f),
		new Float3(-0.09655942f, 0.91937083f, -0.3813575f),
		new Float3(0.271570235f, -0.957791f, -0.09426606f),
		new Float3(0.245101571f, -0.6917999f, -0.6792188f),
		new Float3(0.97770077f, -0.175385535f, 0.115503654f),
		new Float3(-0.522474f, 0.8521607f, 0.0290361587f),
		new Float3(-0.773488045f, -0.526129246f, 0.353417963f),
		new Float3(-0.71344924f, -0.269547254f, 0.6467878f),
		new Float3(0.164403722f, 0.5105846f, -0.843963742f),
		new Float3(0.6494636f, 0.0558561124f, 0.7583384f),
		new Float3(-0.4711971f, 0.501728058f, -0.7254256f),
		new Float3(-0.633576453f, -0.238168627f, -0.7361091f),
		new Float3(-0.9021533f, -0.2709478f, -0.335718185f),
		new Float3(-0.3793711f, 0.8722581f, 0.3086152f),
		new Float3(-0.685559869f, -0.325014323f, 0.6514394f),
		new Float3(0.290094227f, -0.7799058f, -0.5546101f),
		new Float3(-0.209831938f, 0.8503707f, 0.482535154f),
		new Float3(-0.459260374f, 0.6598504f, -0.5947077f),
		new Float3(0.871594548f, 0.09616365f, -0.480703115f),
		new Float3(-0.6776666f, 0.711850464f, -0.1844907f),
		new Float3(0.7044378f, 0.3124276f, 0.637304f),
		new Float3(-0.7052319f, -0.240109324f, -0.6670798f),
		new Float3(0.0819210038f, -0.720733643f, -0.688354552f),
		new Float3(-0.6993681f, -0.5875763f, -0.4069869f),
		new Float3(-0.128145441f, 0.6419896f, 0.755928636f),
		new Float3(-0.6337388f, -0.678547144f, -0.3714147f),
		new Float3(0.5565052f, -0.216888756f, -0.8020357f),
		new Float3(-0.579155445f, 0.7244372f, -0.3738579f),
		new Float3(0.11757791f, -0.7096451f, 0.69467926f),
		new Float3(-0.613462f, 0.132363111f, 0.7785528f),
		new Float3(0.698463559f, -0.0298051629f, -0.7150247f),
		new Float3(0.831808269f, -0.3930172f, 0.391959757f),
		new Float3(0.146957636f, 0.055416517f, -0.98758924f),
		new Float3(0.708868563f, -0.2690504f, 0.652010143f),
		new Float3(0.27260533f, 0.67369765f, -0.686889946f),
		new Float3(-0.65912956f, 0.303545862f, -0.688046634f),
		new Float3(0.481513143f, -0.752827f, 0.4487723f),
		new Float3(0.943001f, 0.167564735f, -0.287526131f),
		new Float3(0.434802949f, 0.7695305f, -0.46772778f),
		new Float3(0.393199623f, 0.5944736f, 0.701423645f),
		new Float3(0.725433648f, -0.603925645f, 0.330181479f),
		new Float3(0.759023547f, -0.6506083f, 0.0243331324f),
		new Float3(-0.8552769f, -0.3430043f, 0.388393581f),
		new Float3(-0.6139747f, 0.6981725f, 0.368225753f),
		new Float3(-0.746590555f, -0.575201f, 0.334284931f),
		new Float3(0.5730066f, 0.8105555f, -0.121091679f),
		new Float3(-0.922587752f, -0.3475211f, -0.167514041f),
		new Float3(-0.71058166f, -0.471969217f, -0.5218417f),
		new Float3(-0.0856461f, 0.358300149f, 0.9296697f),
		new Float3(-0.8279698f, -0.2043157f, 0.5222271f),
		new Float3(0.427944034f, 0.278166f, 0.8599346f),
		new Float3(0.539908f, -0.785712063f, -0.3019204f),
		new Float3(0.5678404f, -0.5495414f, -0.612830758f),
		new Float3(-0.9896071f, 0.136563912f, -0.0450341851f),
		new Float3(-0.6154343f, -0.644087553f, 0.454303741f),
		new Float3(0.107420437f, -0.794634044f, 0.597509444f),
		new Float3(-0.359545f, -0.888553f, 0.284957826f),
		new Float3(-0.218040526f, 0.1529889f, 0.9638738f),
		new Float3(-0.7277432f, -0.61640507f, -0.300723463f),
		new Float3(0.7249729f, -0.00669719465f, 0.688744843f),
		new Float3(-0.5553659f, -0.5336586f, 0.6377908f),
		new Float3(0.5137558f, 0.797620833f, -0.316f),
		new Float3(-0.3794025f, 0.924560845f, -0.0352275148f),
		new Float3(0.822924852f, 0.27453658f, -0.497417659f),
		new Float3(-0.5404114f, 0.60911417f, 0.5804614f),
		new Float3(0.8036582f, -0.270302951f, 0.5301602f),
		new Float3(0.604431868f, 0.683296859f, 0.409594327f),
		new Float3(0.06389989f, 0.965820849f, -0.2512108f),
		new Float3(0.108711332f, 0.74024713f, -0.6634878f),
		new Float3(-0.7134277f, -0.6926784f, 0.105912849f),
		new Float3(0.645889759f, -0.57245487f, -0.50509584f),
		new Float3(-0.6553931f, 0.73814714f, 0.159995615f),
		new Float3(0.391096145f, 0.918887138f, -0.05186756f),
		new Float3(-0.487902254f, -0.5904377f, 0.642911136f),
		new Float3(0.601479f, 0.770744145f, -0.210182011f),
		new Float3(-0.5677173f, 0.7511361f, 0.336885184f),
		new Float3(0.7858574f, 0.226674661f, 0.5753667f),
		new Float3(-0.452034563f, -0.6042227f, -0.656185746f),
		new Float3(0.00227211625f, 0.4132844f, -0.9105992f),
		new Float3(-0.581575155f, -0.5162926f, 0.6286591f),
		new Float3(-0.03703705f, 0.8273786f, 0.5604221f),
		new Float3(-0.511969268f, 0.795354366f, -0.324498f),
		new Float3(-0.268241733f, -0.957229f, -0.10843876f),
		new Float3(-0.232248276f, -0.9679131f, -0.09594243f),
		new Float3(0.3554329f, -0.8881506f, 0.291300625f),
		new Float3(0.734652042f, -0.4371373f, 0.5188423f),
		new Float3(0.998512f, 0.0465901121f, -0.0283394456f),
		new Float3(-0.37276876f, -0.9082481f, 0.190075725f),
		new Float3(0.9173738f, -0.3483642f, 0.192529842f),
		new Float3(0.2714911f, 0.41475296f, -0.868488669f),
		new Float3(0.5131763f, -0.711633444f, 0.4798207f),
		new Float3(-0.873735368f, 0.188869923f, -0.448235065f),
		new Float3(0.846004367f, -0.3725218f, 0.38145f),
		new Float3(0.897872746f, -0.178020909f, -0.402657539f),
		new Float3(0.217806563f, -0.9698323f, -0.109478951f),
		new Float3(-0.151803136f, -0.7788918f, -0.6085091f),
		new Float3(-0.2600385f, -0.4755398f, -0.840382f),
		new Float3(0.5723135f, -0.7474341f, -0.337341845f),
		new Float3(-0.7174141f, 0.169901714f, -0.675611138f),
		new Float3(-0.6841808f, 0.0214570761f, -0.728996754f),
		new Float3(-0.2007448f, 0.06555606f, -0.9774477f),
		new Float3(-0.114880368f, -0.8044887f, 0.5827524f),
		new Float3(-0.787035f, 0.03447489f, 0.6159443f),
		new Float3(-0.201559648f, 0.685987234f, 0.699138939f),
		new Float3(-0.0858108252f, -0.10920836f, -0.990308046f),
		new Float3(0.5532693f, 0.732525051f, -0.396610767f),
		new Float3(-0.184248939f, -0.9777375f, -0.100407675f),
		new Float3(0.07754738f, -0.9111506f, 0.404711038f),
		new Float3(0.139983848f, 0.7601631f, -0.634473443f),
		new Float3(0.448441923f, -0.84528923f, 0.290492535f)
	};

	private const int X_PRIME = 1619;

	private const int Y_PRIME = 31337;

	private const int Z_PRIME = 6971;

	private const int W_PRIME = 1013;

	private const float F3 = 1f / 3f;

	private const float G3 = 1f / 6f;

	private const float G33 = -0.5f;

	private const float SQRT3 = 1.73205078f;

	private const float F2 = 0.3660254f;

	private const float G2 = 0.211324871f;

	private static readonly byte[] SIMPLEX_4D = new byte[256]
	{
		0, 1, 2, 3, 0, 1, 3, 2, 0, 0,
		0, 0, 0, 2, 3, 1, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 1, 2,
		3, 0, 0, 2, 1, 3, 0, 0, 0, 0,
		0, 3, 1, 2, 0, 3, 2, 1, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		1, 3, 2, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 1, 2, 0, 3,
		0, 0, 0, 0, 1, 3, 0, 2, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		2, 3, 0, 1, 2, 3, 1, 0, 1, 0,
		2, 3, 1, 0, 3, 2, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 2, 0,
		3, 1, 0, 0, 0, 0, 2, 1, 3, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 2, 0, 1, 3, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 3, 0,
		1, 2, 3, 0, 2, 1, 0, 0, 0, 0,
		3, 1, 2, 0, 2, 1, 0, 3, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		3, 1, 0, 2, 0, 0, 0, 0, 3, 2,
		0, 1, 3, 2, 1, 0
	};

	private const float F4 = 0.309017f;

	private const float G4 = 0.1381966f;

	private const float CUBIC_3D_BOUNDING = 8f / 27f;

	private const float CUBIC_2D_BOUNDING = 4f / 9f;

	public FastNoise( int seed = 1337 )
	{
		m_seed = seed;
		CalculateFractalBounding();
	}

	public static float GetDecimalType()
	{
		return 0f;
	}

	public int GetSeed()
	{
		return m_seed;
	}

	public void SetSeed( int seed )
	{
		m_seed = seed;
	}

	public void SetFrequency( float frequency )
	{
		m_frequency = frequency;
	}

	public void SetInterp( Interp interp )
	{
		m_interp = interp;
	}

	public void SetNoiseType( NoiseType noiseType )
	{
		m_noiseType = noiseType;
	}

	public void SetFractalOctaves( int octaves )
	{
		if ( m_octaves != octaves )
		{
			m_octaves = octaves;
			CalculateFractalBounding();
		}
	}

	public void SetFractalLacunarity( float lacunarity )
	{
		m_lacunarity = lacunarity;
	}

	public void SetFractalGain( float gain )
	{
		m_gain = gain;
		CalculateFractalBounding();
	}

	public void SetFractalType( FractalType fractalType )
	{
		m_fractalType = fractalType;
	}

	public void SetCellularDistanceFunction( CellularDistanceFunction cellularDistanceFunction )
	{
		m_cellularDistanceFunction = cellularDistanceFunction;
	}

	public void SetCellularReturnType( CellularReturnType cellularReturnType )
	{
		m_cellularReturnType = cellularReturnType;
	}

	public void SetCellularDistance2Indicies( int cellularDistanceIndex0, int cellularDistanceIndex1 )
	{
		m_cellularDistanceIndex0 = Math.Min( cellularDistanceIndex0, cellularDistanceIndex1 );
		m_cellularDistanceIndex1 = Math.Max( cellularDistanceIndex0, cellularDistanceIndex1 );
		m_cellularDistanceIndex0 = Math.Min( Math.Max( m_cellularDistanceIndex0, 0 ), 3 );
		m_cellularDistanceIndex1 = Math.Min( Math.Max( m_cellularDistanceIndex1, 0 ), 3 );
	}

	public void SetCellularJitter( float cellularJitter )
	{
		m_cellularJitter = cellularJitter;
	}

	public void SetCellularNoiseLookup( FastNoise noise )
	{
		m_cellularNoiseLookup = noise;
	}

	public void SetGradientPerturbAmp( float gradientPerturbAmp )
	{
		m_gradientPerturbAmp = gradientPerturbAmp;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static int FastFloor( float f )
	{
		return (f >= 0f) ? ((int)f) : ((int)f - 1);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static int FastRound( float f )
	{
		return (f >= 0f) ? ((int)(f + 0.5f)) : ((int)(f - 0.5f));
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float Lerp( float a, float b, float t )
	{
		return a + t * (b - a);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float InterpHermiteFunc( float t )
	{
		return t * t * (3f - 2f * t);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float InterpQuinticFunc( float t )
	{
		return t * t * t * (t * (t * 6f - 15f) + 10f);
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float CubicLerp( float a, float b, float c, float d, float t )
	{
		float num = d - c - (a - b);
		return t * t * t * num + t * t * (a - b - num) + t * (c - a) + b;
	}

	private void CalculateFractalBounding()
	{
		float num = m_gain;
		float num2 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			num2 += num;
			num *= m_gain;
		}

		m_fractalBounding = 1f / num2;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static int Hash2D( int seed, int x, int y )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num = num * num * num * 60493;
		return (num >> 13) ^ num;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static int Hash3D( int seed, int x, int y, int z )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num ^= 6971 * z;
		num = num * num * num * 60493;
		return (num >> 13) ^ num;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static int Hash4D( int seed, int x, int y, int z, int w )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num ^= 6971 * z;
		num ^= 1013 * w;
		num = num * num * num * 60493;
		return (num >> 13) ^ num;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float ValCoord2D( int seed, int x, int y )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		return (float)(num * num * num * 60493) / 2.14748365E+09f;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float ValCoord3D( int seed, int x, int y, int z )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num ^= 6971 * z;
		return (float)(num * num * num * 60493) / 2.14748365E+09f;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float ValCoord4D( int seed, int x, int y, int z, int w )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num ^= 6971 * z;
		num ^= 1013 * w;
		return (float)(num * num * num * 60493) / 2.14748365E+09f;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float GradCoord2D( int seed, int x, int y, float xd, float yd )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num = num * num * num * 60493;
		num = (num >> 13) ^ num;
		Float2 @float = GRAD_2D[num & 7];
		return xd * @float.x + yd * @float.y;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float GradCoord3D( int seed, int x, int y, int z, float xd, float yd, float zd )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num ^= 6971 * z;
		num = num * num * num * 60493;
		num = (num >> 13) ^ num;
		Float3 @float = GRAD_3D[num & 0xF];
		return xd * @float.x + yd * @float.y + zd * @float.z;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float GradCoord4D( int seed, int x, int y, int z, int w, float xd, float yd, float zd, float wd )
	{
		int num = seed;
		num ^= 1619 * x;
		num ^= 31337 * y;
		num ^= 6971 * z;
		num ^= 1013 * w;
		num = num * num * num * 60493;
		num = (num >> 13) ^ num;
		num &= 0x1F;
		float num2 = yd;
		float num3 = zd;
		float num4 = wd;
		switch ( num >> 3 )
		{
			case 1:
				num2 = wd;
				num3 = xd;
				num4 = yd;
				break;
			case 2:
				num2 = zd;
				num3 = wd;
				num4 = xd;
				break;
			case 3:
				num2 = yd;
				num3 = zd;
				num4 = wd;
				break;
		}

		return (((num & 4) == 0) ? (0f - num2) : num2) + (((num & 2) == 0) ? (0f - num3) : num3) + (((num & 1) == 0) ? (0f - num4) : num4);
	}

	public float GetNoise( float x, float y, float z )
	{
		x *= m_frequency;
		y *= m_frequency;
		z *= m_frequency;
		switch ( m_noiseType )
		{
			case NoiseType.Value:
				return SingleValue( m_seed, x, y, z );
			case NoiseType.ValueFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SingleValueFractalFBM( x, y, z ),
					FractalType.Billow => SingleValueFractalBillow( x, y, z ),
					FractalType.RigidMulti => SingleValueFractalRigidMulti( x, y, z ),
					_ => 0f,
				};
			case NoiseType.Perlin:
				return SinglePerlin( m_seed, x, y, z );
			case NoiseType.PerlinFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SinglePerlinFractalFBM( x, y, z ),
					FractalType.Billow => SinglePerlinFractalBillow( x, y, z ),
					FractalType.RigidMulti => SinglePerlinFractalRigidMulti( x, y, z ),
					_ => 0f,
				};
			case NoiseType.Simplex:
				return SingleSimplex( m_seed, x, y, z );
			case NoiseType.SimplexFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SingleSimplexFractalFBM( x, y, z ),
					FractalType.Billow => SingleSimplexFractalBillow( x, y, z ),
					FractalType.RigidMulti => SingleSimplexFractalRigidMulti( x, y, z ),
					_ => 0f,
				};
			case NoiseType.Cellular:
				{
					CellularReturnType cellularReturnType = m_cellularReturnType;
					CellularReturnType cellularReturnType2 = cellularReturnType;
					if ( (uint)cellularReturnType2 <= 2u )
					{
						return SingleCellular( x, y, z );
					}

					return SingleCellular2Edge( x, y, z );
				}
			case NoiseType.WhiteNoise:
				return GetWhiteNoise( x, y, z );
			case NoiseType.Cubic:
				return SingleCubic( m_seed, x, y, z );
			case NoiseType.CubicFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SingleCubicFractalFBM( x, y, z ),
					FractalType.Billow => SingleCubicFractalBillow( x, y, z ),
					FractalType.RigidMulti => SingleCubicFractalRigidMulti( x, y, z ),
					_ => 0f,
				};
			default:
				return 0f;
		}
	}

	public float GetNoise( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		switch ( m_noiseType )
		{
			case NoiseType.Value:
				return SingleValue( m_seed, x, y );
			case NoiseType.ValueFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SingleValueFractalFBM( x, y ),
					FractalType.Billow => SingleValueFractalBillow( x, y ),
					FractalType.RigidMulti => SingleValueFractalRigidMulti( x, y ),
					_ => 0f,
				};
			case NoiseType.Perlin:
				return SinglePerlin( m_seed, x, y );
			case NoiseType.PerlinFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SinglePerlinFractalFBM( x, y ),
					FractalType.Billow => SinglePerlinFractalBillow( x, y ),
					FractalType.RigidMulti => SinglePerlinFractalRigidMulti( x, y ),
					_ => 0f,
				};
			case NoiseType.Simplex:
				return SingleSimplex( m_seed, x, y );
			case NoiseType.SimplexFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SingleSimplexFractalFBM( x, y ),
					FractalType.Billow => SingleSimplexFractalBillow( x, y ),
					FractalType.RigidMulti => SingleSimplexFractalRigidMulti( x, y ),
					_ => 0f,
				};
			case NoiseType.Cellular:
				{
					CellularReturnType cellularReturnType = m_cellularReturnType;
					CellularReturnType cellularReturnType2 = cellularReturnType;
					if ( (uint)cellularReturnType2 <= 2u )
					{
						return SingleCellular( x, y );
					}

					return SingleCellular2Edge( x, y );
				}
			case NoiseType.WhiteNoise:
				return GetWhiteNoise( x, y );
			case NoiseType.Cubic:
				return SingleCubic( m_seed, x, y );
			case NoiseType.CubicFractal:
				return m_fractalType switch
				{
					FractalType.FBM => SingleCubicFractalFBM( x, y ),
					FractalType.Billow => SingleCubicFractalBillow( x, y ),
					FractalType.RigidMulti => SingleCubicFractalRigidMulti( x, y ),
					_ => 0f,
				};
			default:
				return 0f;
		}
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private int FloatCast2Int( float f )
	{
		long num = BitConverter.DoubleToInt64Bits( f );
		return (int)(num ^ (num >> 32));
	}

	public float GetWhiteNoise( float x, float y, float z, float w )
	{
		int x2 = FloatCast2Int( x );
		int y2 = FloatCast2Int( y );
		int z2 = FloatCast2Int( z );
		int w2 = FloatCast2Int( w );
		return ValCoord4D( m_seed, x2, y2, z2, w2 );
	}

	public float GetWhiteNoise( float x, float y, float z )
	{
		int x2 = FloatCast2Int( x );
		int y2 = FloatCast2Int( y );
		int z2 = FloatCast2Int( z );
		return ValCoord3D( m_seed, x2, y2, z2 );
	}

	public float GetWhiteNoise( float x, float y )
	{
		int x2 = FloatCast2Int( x );
		int y2 = FloatCast2Int( y );
		return ValCoord2D( m_seed, x2, y2 );
	}

	public float GetWhiteNoiseInt( int x, int y, int z, int w )
	{
		return ValCoord4D( m_seed, x, y, z, w );
	}

	public float GetWhiteNoiseInt( int x, int y, int z )
	{
		return ValCoord3D( m_seed, x, y, z );
	}

	public float GetWhiteNoiseInt( int x, int y )
	{
		return ValCoord2D( m_seed, x, y );
	}

	public float GetValueFractal( float x, float y, float z )
	{
		x *= m_frequency;
		y *= m_frequency;
		z *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SingleValueFractalFBM( x, y, z ),
			FractalType.Billow => SingleValueFractalBillow( x, y, z ),
			FractalType.RigidMulti => SingleValueFractalRigidMulti( x, y, z ),
			_ => 0f,
		};
	}

	private float SingleValueFractalFBM( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = SingleValue( num, x, y, z );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += SingleValue( ++num, x, y, z ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleValueFractalBillow( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = Math.Abs( SingleValue( num, x, y, z ) ) * 2f - 1f;
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SingleValue( ++num, x, y, z ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleValueFractalRigidMulti( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SingleValue( num, x, y, z ) );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SingleValue( ++num, x, y, z ) )) * num3;
		}

		return num2;
	}

	public float GetValue( float x, float y, float z )
	{
		return SingleValue( m_seed, x * m_frequency, y * m_frequency, z * m_frequency );
	}

	private float SingleValue( int seed, float x, float y, float z )
	{
		int num = FastFloor( x );
		int num2 = FastFloor( y );
		int num3 = FastFloor( z );
		int x2 = num + 1;
		int y2 = num2 + 1;
		int z2 = num3 + 1;
		float t;
		float t2;
		float t3;
		switch ( m_interp )
		{
			default:
				t = x - (float)num;
				t2 = y - (float)num2;
				t3 = z - (float)num3;
				break;
			case Interp.Hermite:
				t = InterpHermiteFunc( x - (float)num );
				t2 = InterpHermiteFunc( y - (float)num2 );
				t3 = InterpHermiteFunc( z - (float)num3 );
				break;
			case Interp.Quintic:
				t = InterpQuinticFunc( x - (float)num );
				t2 = InterpQuinticFunc( y - (float)num2 );
				t3 = InterpQuinticFunc( z - (float)num3 );
				break;
		}

		float a = Lerp( ValCoord3D( seed, num, num2, num3 ), ValCoord3D( seed, x2, num2, num3 ), t );
		float b = Lerp( ValCoord3D( seed, num, y2, num3 ), ValCoord3D( seed, x2, y2, num3 ), t );
		float a2 = Lerp( ValCoord3D( seed, num, num2, z2 ), ValCoord3D( seed, x2, num2, z2 ), t );
		float b2 = Lerp( ValCoord3D( seed, num, y2, z2 ), ValCoord3D( seed, x2, y2, z2 ), t );
		float a3 = Lerp( a, b, t2 );
		float b3 = Lerp( a2, b2, t2 );
		return Lerp( a3, b3, t3 );
	}

	public float GetValueFractal( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SingleValueFractalFBM( x, y ),
			FractalType.Billow => SingleValueFractalBillow( x, y ),
			FractalType.RigidMulti => SingleValueFractalRigidMulti( x, y ),
			_ => 0f,
		};
	}

	private float SingleValueFractalFBM( float x, float y )
	{
		int num = m_seed;
		float num2 = SingleValue( num, x, y );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += SingleValue( ++num, x, y ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleValueFractalBillow( float x, float y )
	{
		int num = m_seed;
		float num2 = Math.Abs( SingleValue( num, x, y ) ) * 2f - 1f;
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SingleValue( ++num, x, y ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleValueFractalRigidMulti( float x, float y )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SingleValue( num, x, y ) );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SingleValue( ++num, x, y ) )) * num3;
		}

		return num2;
	}

	public float GetValue( float x, float y )
	{
		return SingleValue( m_seed, x * m_frequency, y * m_frequency );
	}

	private float SingleValue( int seed, float x, float y )
	{
		int num = FastFloor( x );
		int num2 = FastFloor( y );
		int x2 = num + 1;
		int y2 = num2 + 1;
		float t;
		float t2;
		switch ( m_interp )
		{
			default:
				t = x - (float)num;
				t2 = y - (float)num2;
				break;
			case Interp.Hermite:
				t = InterpHermiteFunc( x - (float)num );
				t2 = InterpHermiteFunc( y - (float)num2 );
				break;
			case Interp.Quintic:
				t = InterpQuinticFunc( x - (float)num );
				t2 = InterpQuinticFunc( y - (float)num2 );
				break;
		}

		float a = Lerp( ValCoord2D( seed, num, num2 ), ValCoord2D( seed, x2, num2 ), t );
		float b = Lerp( ValCoord2D( seed, num, y2 ), ValCoord2D( seed, x2, y2 ), t );
		return Lerp( a, b, t2 );
	}

	public float GetPerlinFractal( float x, float y, float z )
	{
		x *= m_frequency;
		y *= m_frequency;
		z *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SinglePerlinFractalFBM( x, y, z ),
			FractalType.Billow => SinglePerlinFractalBillow( x, y, z ),
			FractalType.RigidMulti => SinglePerlinFractalRigidMulti( x, y, z ),
			_ => 0f,
		};
	}

	private float SinglePerlinFractalFBM( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = SinglePerlin( num, x, y, z );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += SinglePerlin( ++num, x, y, z ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SinglePerlinFractalBillow( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = Math.Abs( SinglePerlin( num, x, y, z ) ) * 2f - 1f;
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SinglePerlin( ++num, x, y, z ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SinglePerlinFractalRigidMulti( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SinglePerlin( num, x, y, z ) );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SinglePerlin( ++num, x, y, z ) )) * num3;
		}

		return num2;
	}

	public float GetPerlin( float x, float y, float z )
	{
		return SinglePerlin( m_seed, x * m_frequency, y * m_frequency, z * m_frequency );
	}

	private float SinglePerlin( int seed, float x, float y, float z )
	{
		int num = FastFloor( x );
		int num2 = FastFloor( y );
		int num3 = FastFloor( z );
		int x2 = num + 1;
		int y2 = num2 + 1;
		int z2 = num3 + 1;
		float t;
		float t2;
		float t3;
		switch ( m_interp )
		{
			default:
				t = x - (float)num;
				t2 = y - (float)num2;
				t3 = z - (float)num3;
				break;
			case Interp.Hermite:
				t = InterpHermiteFunc( x - (float)num );
				t2 = InterpHermiteFunc( y - (float)num2 );
				t3 = InterpHermiteFunc( z - (float)num3 );
				break;
			case Interp.Quintic:
				t = InterpQuinticFunc( x - (float)num );
				t2 = InterpQuinticFunc( y - (float)num2 );
				t3 = InterpQuinticFunc( z - (float)num3 );
				break;
		}

		float num4 = x - (float)num;
		float num5 = y - (float)num2;
		float num6 = z - (float)num3;
		float xd = num4 - 1f;
		float yd = num5 - 1f;
		float zd = num6 - 1f;
		float a = Lerp( GradCoord3D( seed, num, num2, num3, num4, num5, num6 ), GradCoord3D( seed, x2, num2, num3, xd, num5, num6 ), t );
		float b = Lerp( GradCoord3D( seed, num, y2, num3, num4, yd, num6 ), GradCoord3D( seed, x2, y2, num3, xd, yd, num6 ), t );
		float a2 = Lerp( GradCoord3D( seed, num, num2, z2, num4, num5, zd ), GradCoord3D( seed, x2, num2, z2, xd, num5, zd ), t );
		float b2 = Lerp( GradCoord3D( seed, num, y2, z2, num4, yd, zd ), GradCoord3D( seed, x2, y2, z2, xd, yd, zd ), t );
		float a3 = Lerp( a, b, t2 );
		float b3 = Lerp( a2, b2, t2 );
		return Lerp( a3, b3, t3 );
	}

	public float GetPerlinFractal( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SinglePerlinFractalFBM( x, y ),
			FractalType.Billow => SinglePerlinFractalBillow( x, y ),
			FractalType.RigidMulti => SinglePerlinFractalRigidMulti( x, y ),
			_ => 0f,
		};
	}

	private float SinglePerlinFractalFBM( float x, float y )
	{
		int num = m_seed;
		float num2 = SinglePerlin( num, x, y );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += SinglePerlin( ++num, x, y ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SinglePerlinFractalBillow( float x, float y )
	{
		int num = m_seed;
		float num2 = Math.Abs( SinglePerlin( num, x, y ) ) * 2f - 1f;
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SinglePerlin( ++num, x, y ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SinglePerlinFractalRigidMulti( float x, float y )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SinglePerlin( num, x, y ) );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SinglePerlin( ++num, x, y ) )) * num3;
		}

		return num2;
	}

	public float GetPerlin( float x, float y )
	{
		return SinglePerlin( m_seed, x * m_frequency, y * m_frequency );
	}

	private float SinglePerlin( int seed, float x, float y )
	{
		int num = FastFloor( x );
		int num2 = FastFloor( y );
		int x2 = num + 1;
		int y2 = num2 + 1;
		float t;
		float t2;
		switch ( m_interp )
		{
			default:
				t = x - (float)num;
				t2 = y - (float)num2;
				break;
			case Interp.Hermite:
				t = InterpHermiteFunc( x - (float)num );
				t2 = InterpHermiteFunc( y - (float)num2 );
				break;
			case Interp.Quintic:
				t = InterpQuinticFunc( x - (float)num );
				t2 = InterpQuinticFunc( y - (float)num2 );
				break;
		}

		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		float xd = num3 - 1f;
		float yd = num4 - 1f;
		float a = Lerp( GradCoord2D( seed, num, num2, num3, num4 ), GradCoord2D( seed, x2, num2, xd, num4 ), t );
		float b = Lerp( GradCoord2D( seed, num, y2, num3, yd ), GradCoord2D( seed, x2, y2, xd, yd ), t );
		return Lerp( a, b, t2 );
	}

	public float GetSimplexFractal( float x, float y, float z )
	{
		x *= m_frequency;
		y *= m_frequency;
		z *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SingleSimplexFractalFBM( x, y, z ),
			FractalType.Billow => SingleSimplexFractalBillow( x, y, z ),
			FractalType.RigidMulti => SingleSimplexFractalRigidMulti( x, y, z ),
			_ => 0f,
		};
	}

	private float SingleSimplexFractalFBM( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = SingleSimplex( num, x, y, z );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += SingleSimplex( ++num, x, y, z ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleSimplexFractalBillow( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = Math.Abs( SingleSimplex( num, x, y, z ) ) * 2f - 1f;
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SingleSimplex( ++num, x, y, z ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleSimplexFractalRigidMulti( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SingleSimplex( num, x, y, z ) );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SingleSimplex( ++num, x, y, z ) )) * num3;
		}

		return num2;
	}

	public float GetSimplex( float x, float y, float z )
	{
		return SingleSimplex( m_seed, x * m_frequency, y * m_frequency, z * m_frequency );
	}

	private float SingleSimplex( int seed, float x, float y, float z )
	{
		float num = (x + y + z) * (1f / 3f);
		int num2 = FastFloor( x + num );
		int num3 = FastFloor( y + num );
		int num4 = FastFloor( z + num );
		num = (float)(num2 + num3 + num4) * (1f / 6f);
		float num5 = x - ((float)num2 - num);
		float num6 = y - ((float)num3 - num);
		float num7 = z - ((float)num4 - num);
		int num8;
		int num9;
		int num10;
		int num11;
		int num12;
		int num13;
		if ( num5 >= num6 )
		{
			if ( num6 >= num7 )
			{
				num8 = 1;
				num9 = 0;
				num10 = 0;
				num11 = 1;
				num12 = 1;
				num13 = 0;
			}
			else if ( num5 >= num7 )
			{
				num8 = 1;
				num9 = 0;
				num10 = 0;
				num11 = 1;
				num12 = 0;
				num13 = 1;
			}
			else
			{
				num8 = 0;
				num9 = 0;
				num10 = 1;
				num11 = 1;
				num12 = 0;
				num13 = 1;
			}
		}
		else if ( num6 < num7 )
		{
			num8 = 0;
			num9 = 0;
			num10 = 1;
			num11 = 0;
			num12 = 1;
			num13 = 1;
		}
		else if ( num5 < num7 )
		{
			num8 = 0;
			num9 = 1;
			num10 = 0;
			num11 = 0;
			num12 = 1;
			num13 = 1;
		}
		else
		{
			num8 = 0;
			num9 = 1;
			num10 = 0;
			num11 = 1;
			num12 = 1;
			num13 = 0;
		}

		float num14 = num5 - (float)num8 + 1f / 6f;
		float num15 = num6 - (float)num9 + 1f / 6f;
		float num16 = num7 - (float)num10 + 1f / 6f;
		float num17 = num5 - (float)num11 + 1f / 3f;
		float num18 = num6 - (float)num12 + 1f / 3f;
		float num19 = num7 - (float)num13 + 1f / 3f;
		float num20 = num5 + -0.5f;
		float num21 = num6 + -0.5f;
		float num22 = num7 + -0.5f;
		num = 0.6f - num5 * num5 - num6 * num6 - num7 * num7;
		float num23;
		if ( num < 0f )
		{
			num23 = 0f;
		}
		else
		{
			num *= num;
			num23 = num * num * GradCoord3D( seed, num2, num3, num4, num5, num6, num7 );
		}

		num = 0.6f - num14 * num14 - num15 * num15 - num16 * num16;
		float num24;
		if ( num < 0f )
		{
			num24 = 0f;
		}
		else
		{
			num *= num;
			num24 = num * num * GradCoord3D( seed, num2 + num8, num3 + num9, num4 + num10, num14, num15, num16 );
		}

		num = 0.6f - num17 * num17 - num18 * num18 - num19 * num19;
		float num25;
		if ( num < 0f )
		{
			num25 = 0f;
		}
		else
		{
			num *= num;
			num25 = num * num * GradCoord3D( seed, num2 + num11, num3 + num12, num4 + num13, num17, num18, num19 );
		}

		num = 0.6f - num20 * num20 - num21 * num21 - num22 * num22;
		float num26;
		if ( num < 0f )
		{
			num26 = 0f;
		}
		else
		{
			num *= num;
			num26 = num * num * GradCoord3D( seed, num2 + 1, num3 + 1, num4 + 1, num20, num21, num22 );
		}

		return 32f * (num23 + num24 + num25 + num26);
	}

	public float GetSimplexFractal( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SingleSimplexFractalFBM( x, y ),
			FractalType.Billow => SingleSimplexFractalBillow( x, y ),
			FractalType.RigidMulti => SingleSimplexFractalRigidMulti( x, y ),
			_ => 0f,
		};
	}

	private float SingleSimplexFractalFBM( float x, float y )
	{
		int num = m_seed;
		float num2 = SingleSimplex( num, x, y );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += SingleSimplex( ++num, x, y ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleSimplexFractalBillow( float x, float y )
	{
		int num = m_seed;
		float num2 = Math.Abs( SingleSimplex( num, x, y ) ) * 2f - 1f;
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SingleSimplex( ++num, x, y ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleSimplexFractalRigidMulti( float x, float y )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SingleSimplex( num, x, y ) );
		float num3 = 1f;
		for ( int i = 1; i < m_octaves; i++ )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SingleSimplex( ++num, x, y ) )) * num3;
		}

		return num2;
	}

	public float GetSimplex( float x, float y )
	{
		return SingleSimplex( m_seed, x * m_frequency, y * m_frequency );
	}

	private float SingleSimplex( int seed, float x, float y )
	{
		float num = (x + y) * 0.3660254f;
		int num2 = FastFloor( x + num );
		int num3 = FastFloor( y + num );
		num = (float)(num2 + num3) * 0.211324871f;
		float num4 = (float)num2 - num;
		float num5 = (float)num3 - num;
		float num6 = x - num4;
		float num7 = y - num5;
		int num8;
		int num9;
		if ( num6 > num7 )
		{
			num8 = 1;
			num9 = 0;
		}
		else
		{
			num8 = 0;
			num9 = 1;
		}

		float num10 = num6 - (float)num8 + 0.211324871f;
		float num11 = num7 - (float)num9 + 0.211324871f;
		float num12 = num6 - 1f + 0.422649741f;
		float num13 = num7 - 1f + 0.422649741f;
		num = 0.5f - num6 * num6 - num7 * num7;
		float num14;
		if ( num < 0f )
		{
			num14 = 0f;
		}
		else
		{
			num *= num;
			num14 = num * num * GradCoord2D( seed, num2, num3, num6, num7 );
		}

		num = 0.5f - num10 * num10 - num11 * num11;
		float num15;
		if ( num < 0f )
		{
			num15 = 0f;
		}
		else
		{
			num *= num;
			num15 = num * num * GradCoord2D( seed, num2 + num8, num3 + num9, num10, num11 );
		}

		num = 0.5f - num12 * num12 - num13 * num13;
		float num16;
		if ( num < 0f )
		{
			num16 = 0f;
		}
		else
		{
			num *= num;
			num16 = num * num * GradCoord2D( seed, num2 + 1, num3 + 1, num12, num13 );
		}

		return 50f * (num14 + num15 + num16);
	}

	public float GetSimplex( float x, float y, float z, float w )
	{
		return SingleSimplex( m_seed, x * m_frequency, y * m_frequency, z * m_frequency, w * m_frequency );
	}

	private float SingleSimplex( int seed, float x, float y, float z, float w )
	{
		float num = (x + y + z + w) * 0.309017f;
		int num2 = FastFloor( x + num );
		int num3 = FastFloor( y + num );
		int num4 = FastFloor( z + num );
		int num5 = FastFloor( w + num );
		num = (float)(num2 + num3 + num4 + num5) * 0.1381966f;
		float num6 = (float)num2 - num;
		float num7 = (float)num3 - num;
		float num8 = (float)num4 - num;
		float num9 = (float)num5 - num;
		float num10 = x - num6;
		float num11 = y - num7;
		float num12 = z - num8;
		float num13 = w - num9;
		int num14 = ((num10 > num11) ? 32 : 0);
		num14 += ((num10 > num12) ? 16 : 0);
		num14 += ((num11 > num12) ? 8 : 0);
		num14 += ((num10 > num13) ? 4 : 0);
		num14 += ((num11 > num13) ? 2 : 0);
		num14 += ((num12 > num13) ? 1 : 0);
		num14 <<= 2;
		int num15 = ((SIMPLEX_4D[num14] >= 3) ? 1 : 0);
		int num16 = ((SIMPLEX_4D[num14] >= 2) ? 1 : 0);
		int num17 = ((SIMPLEX_4D[num14++] >= 1) ? 1 : 0);
		int num18 = ((SIMPLEX_4D[num14] >= 3) ? 1 : 0);
		int num19 = ((SIMPLEX_4D[num14] >= 2) ? 1 : 0);
		int num20 = ((SIMPLEX_4D[num14++] >= 1) ? 1 : 0);
		int num21 = ((SIMPLEX_4D[num14] >= 3) ? 1 : 0);
		int num22 = ((SIMPLEX_4D[num14] >= 2) ? 1 : 0);
		int num23 = ((SIMPLEX_4D[num14++] >= 1) ? 1 : 0);
		int num24 = ((SIMPLEX_4D[num14] >= 3) ? 1 : 0);
		int num25 = ((SIMPLEX_4D[num14] >= 2) ? 1 : 0);
		int num26 = ((SIMPLEX_4D[num14] >= 1) ? 1 : 0);
		float num27 = num10 - (float)num15 + 0.1381966f;
		float num28 = num11 - (float)num18 + 0.1381966f;
		float num29 = num12 - (float)num21 + 0.1381966f;
		float num30 = num13 - (float)num24 + 0.1381966f;
		float num31 = num10 - (float)num16 + 0.2763932f;
		float num32 = num11 - (float)num19 + 0.2763932f;
		float num33 = num12 - (float)num22 + 0.2763932f;
		float num34 = num13 - (float)num25 + 0.2763932f;
		float num35 = num10 - (float)num17 + 0.414589822f;
		float num36 = num11 - (float)num20 + 0.414589822f;
		float num37 = num12 - (float)num23 + 0.414589822f;
		float num38 = num13 - (float)num26 + 0.414589822f;
		float num39 = num10 - 1f + 0.5527864f;
		float num40 = num11 - 1f + 0.5527864f;
		float num41 = num12 - 1f + 0.5527864f;
		float num42 = num13 - 1f + 0.5527864f;
		num = 0.6f - num10 * num10 - num11 * num11 - num12 * num12 - num13 * num13;
		float num43;
		if ( num < 0f )
		{
			num43 = 0f;
		}
		else
		{
			num *= num;
			num43 = num * num * GradCoord4D( seed, num2, num3, num4, num5, num10, num11, num12, num13 );
		}

		num = 0.6f - num27 * num27 - num28 * num28 - num29 * num29 - num30 * num30;
		float num44;
		if ( num < 0f )
		{
			num44 = 0f;
		}
		else
		{
			num *= num;
			num44 = num * num * GradCoord4D( seed, num2 + num15, num3 + num18, num4 + num21, num5 + num24, num27, num28, num29, num30 );
		}

		num = 0.6f - num31 * num31 - num32 * num32 - num33 * num33 - num34 * num34;
		float num45;
		if ( num < 0f )
		{
			num45 = 0f;
		}
		else
		{
			num *= num;
			num45 = num * num * GradCoord4D( seed, num2 + num16, num3 + num19, num4 + num22, num5 + num25, num31, num32, num33, num34 );
		}

		num = 0.6f - num35 * num35 - num36 * num36 - num37 * num37 - num38 * num38;
		float num46;
		if ( num < 0f )
		{
			num46 = 0f;
		}
		else
		{
			num *= num;
			num46 = num * num * GradCoord4D( seed, num2 + num17, num3 + num20, num4 + num23, num5 + num26, num35, num36, num37, num38 );
		}

		num = 0.6f - num39 * num39 - num40 * num40 - num41 * num41 - num42 * num42;
		float num47;
		if ( num < 0f )
		{
			num47 = 0f;
		}
		else
		{
			num *= num;
			num47 = num * num * GradCoord4D( seed, num2 + 1, num3 + 1, num4 + 1, num5 + 1, num39, num40, num41, num42 );
		}

		return 27f * (num43 + num44 + num45 + num46 + num47);
	}

	public float GetCubicFractal( float x, float y, float z )
	{
		x *= m_frequency;
		y *= m_frequency;
		z *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SingleCubicFractalFBM( x, y, z ),
			FractalType.Billow => SingleCubicFractalBillow( x, y, z ),
			FractalType.RigidMulti => SingleCubicFractalRigidMulti( x, y, z ),
			_ => 0f,
		};
	}

	private float SingleCubicFractalFBM( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = SingleCubic( num, x, y, z );
		float num3 = 1f;
		int num4 = 0;
		while ( ++num4 < m_octaves )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += SingleCubic( ++num, x, y, z ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleCubicFractalBillow( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = Math.Abs( SingleCubic( num, x, y, z ) ) * 2f - 1f;
		float num3 = 1f;
		int num4 = 0;
		while ( ++num4 < m_octaves )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SingleCubic( ++num, x, y, z ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleCubicFractalRigidMulti( float x, float y, float z )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SingleCubic( num, x, y, z ) );
		float num3 = 1f;
		int num4 = 0;
		while ( ++num4 < m_octaves )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			z *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SingleCubic( ++num, x, y, z ) )) * num3;
		}

		return num2;
	}

	public float GetCubic( float x, float y, float z )
	{
		return SingleCubic( m_seed, x * m_frequency, y * m_frequency, z * m_frequency );
	}

	private float SingleCubic( int seed, float x, float y, float z )
	{
		int num = FastFloor( x );
		int num2 = FastFloor( y );
		int num3 = FastFloor( z );
		int x2 = num - 1;
		int y2 = num2 - 1;
		int z2 = num3 - 1;
		int x3 = num + 1;
		int y3 = num2 + 1;
		int z3 = num3 + 1;
		int x4 = num + 2;
		int y4 = num2 + 2;
		int z4 = num3 + 2;
		float t = x - (float)num;
		float t2 = y - (float)num2;
		float t3 = z - (float)num3;
		return CubicLerp( CubicLerp( CubicLerp( ValCoord3D( seed, x2, y2, z2 ), ValCoord3D( seed, num, y2, z2 ), ValCoord3D( seed, x3, y2, z2 ), ValCoord3D( seed, x4, y2, z2 ), t ), CubicLerp( ValCoord3D( seed, x2, num2, z2 ), ValCoord3D( seed, num, num2, z2 ), ValCoord3D( seed, x3, num2, z2 ), ValCoord3D( seed, x4, num2, z2 ), t ), CubicLerp( ValCoord3D( seed, x2, y3, z2 ), ValCoord3D( seed, num, y3, z2 ), ValCoord3D( seed, x3, y3, z2 ), ValCoord3D( seed, x4, y3, z2 ), t ), CubicLerp( ValCoord3D( seed, x2, y4, z2 ), ValCoord3D( seed, num, y4, z2 ), ValCoord3D( seed, x3, y4, z2 ), ValCoord3D( seed, x4, y4, z2 ), t ), t2 ), CubicLerp( CubicLerp( ValCoord3D( seed, x2, y2, num3 ), ValCoord3D( seed, num, y2, num3 ), ValCoord3D( seed, x3, y2, num3 ), ValCoord3D( seed, x4, y2, num3 ), t ), CubicLerp( ValCoord3D( seed, x2, num2, num3 ), ValCoord3D( seed, num, num2, num3 ), ValCoord3D( seed, x3, num2, num3 ), ValCoord3D( seed, x4, num2, num3 ), t ), CubicLerp( ValCoord3D( seed, x2, y3, num3 ), ValCoord3D( seed, num, y3, num3 ), ValCoord3D( seed, x3, y3, num3 ), ValCoord3D( seed, x4, y3, num3 ), t ), CubicLerp( ValCoord3D( seed, x2, y4, num3 ), ValCoord3D( seed, num, y4, num3 ), ValCoord3D( seed, x3, y4, num3 ), ValCoord3D( seed, x4, y4, num3 ), t ), t2 ), CubicLerp( CubicLerp( ValCoord3D( seed, x2, y2, z3 ), ValCoord3D( seed, num, y2, z3 ), ValCoord3D( seed, x3, y2, z3 ), ValCoord3D( seed, x4, y2, z3 ), t ), CubicLerp( ValCoord3D( seed, x2, num2, z3 ), ValCoord3D( seed, num, num2, z3 ), ValCoord3D( seed, x3, num2, z3 ), ValCoord3D( seed, x4, num2, z3 ), t ), CubicLerp( ValCoord3D( seed, x2, y3, z3 ), ValCoord3D( seed, num, y3, z3 ), ValCoord3D( seed, x3, y3, z3 ), ValCoord3D( seed, x4, y3, z3 ), t ), CubicLerp( ValCoord3D( seed, x2, y4, z3 ), ValCoord3D( seed, num, y4, z3 ), ValCoord3D( seed, x3, y4, z3 ), ValCoord3D( seed, x4, y4, z3 ), t ), t2 ), CubicLerp( CubicLerp( ValCoord3D( seed, x2, y2, z4 ), ValCoord3D( seed, num, y2, z4 ), ValCoord3D( seed, x3, y2, z4 ), ValCoord3D( seed, x4, y2, z4 ), t ), CubicLerp( ValCoord3D( seed, x2, num2, z4 ), ValCoord3D( seed, num, num2, z4 ), ValCoord3D( seed, x3, num2, z4 ), ValCoord3D( seed, x4, num2, z4 ), t ), CubicLerp( ValCoord3D( seed, x2, y3, z4 ), ValCoord3D( seed, num, y3, z4 ), ValCoord3D( seed, x3, y3, z4 ), ValCoord3D( seed, x4, y3, z4 ), t ), CubicLerp( ValCoord3D( seed, x2, y4, z4 ), ValCoord3D( seed, num, y4, z4 ), ValCoord3D( seed, x3, y4, z4 ), ValCoord3D( seed, x4, y4, z4 ), t ), t2 ), t3 ) * (8f / 27f);
	}

	public float GetCubicFractal( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		return m_fractalType switch
		{
			FractalType.FBM => SingleCubicFractalFBM( x, y ),
			FractalType.Billow => SingleCubicFractalBillow( x, y ),
			FractalType.RigidMulti => SingleCubicFractalRigidMulti( x, y ),
			_ => 0f,
		};
	}

	private float SingleCubicFractalFBM( float x, float y )
	{
		int num = m_seed;
		float num2 = SingleCubic( num, x, y );
		float num3 = 1f;
		int num4 = 0;
		while ( ++num4 < m_octaves )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += SingleCubic( ++num, x, y ) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleCubicFractalBillow( float x, float y )
	{
		int num = m_seed;
		float num2 = Math.Abs( SingleCubic( num, x, y ) ) * 2f - 1f;
		float num3 = 1f;
		int num4 = 0;
		while ( ++num4 < m_octaves )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 += (Math.Abs( SingleCubic( ++num, x, y ) ) * 2f - 1f) * num3;
		}

		return num2 * m_fractalBounding;
	}

	private float SingleCubicFractalRigidMulti( float x, float y )
	{
		int num = m_seed;
		float num2 = 1f - Math.Abs( SingleCubic( num, x, y ) );
		float num3 = 1f;
		int num4 = 0;
		while ( ++num4 < m_octaves )
		{
			x *= m_lacunarity;
			y *= m_lacunarity;
			num3 *= m_gain;
			num2 -= (1f - Math.Abs( SingleCubic( ++num, x, y ) )) * num3;
		}

		return num2;
	}

	public float GetCubic( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		return SingleCubic( m_seed, x, y );
	}

	private float SingleCubic( int seed, float x, float y )
	{
		int num = FastFloor( x );
		int num2 = FastFloor( y );
		int x2 = num - 1;
		int y2 = num2 - 1;
		int x3 = num + 1;
		int y3 = num2 + 1;
		int x4 = num + 2;
		int y4 = num2 + 2;
		float t = x - (float)num;
		float t2 = y - (float)num2;
		return CubicLerp( CubicLerp( ValCoord2D( seed, x2, y2 ), ValCoord2D( seed, num, y2 ), ValCoord2D( seed, x3, y2 ), ValCoord2D( seed, x4, y2 ), t ), CubicLerp( ValCoord2D( seed, x2, num2 ), ValCoord2D( seed, num, num2 ), ValCoord2D( seed, x3, num2 ), ValCoord2D( seed, x4, num2 ), t ), CubicLerp( ValCoord2D( seed, x2, y3 ), ValCoord2D( seed, num, y3 ), ValCoord2D( seed, x3, y3 ), ValCoord2D( seed, x4, y3 ), t ), CubicLerp( ValCoord2D( seed, x2, y4 ), ValCoord2D( seed, num, y4 ), ValCoord2D( seed, x3, y4 ), ValCoord2D( seed, x4, y4 ), t ), t2 ) * (4f / 9f);
	}

	public float GetCellular( float x, float y, float z )
	{
		x *= m_frequency;
		y *= m_frequency;
		z *= m_frequency;
		CellularReturnType cellularReturnType = m_cellularReturnType;
		CellularReturnType cellularReturnType2 = cellularReturnType;
		if ( (uint)cellularReturnType2 <= 2u )
		{
			return SingleCellular( x, y, z );
		}

		return SingleCellular2Edge( x, y, z );
	}

	private float SingleCellular( float x, float y, float z )
	{
		int num = FastRound( x );
		int num2 = FastRound( y );
		int num3 = FastRound( z );
		float num4 = 999999f;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		switch ( m_cellularDistanceFunction )
		{
			case CellularDistanceFunction.Euclidean:
				{
					for ( int l = num - 1; l <= num + 1; l++ )
					{
						for ( int m = num2 - 1; m <= num2 + 1; m++ )
						{
							for ( int n = num3 - 1; n <= num3 + 1; n++ )
							{
								Float3 float2 = CELL_3D[Hash3D( m_seed, l, m, n ) & 0xFF];
								float num12 = (float)l - x + float2.x * m_cellularJitter;
								float num13 = (float)m - y + float2.y * m_cellularJitter;
								float num14 = (float)n - z + float2.z * m_cellularJitter;
								float num15 = num12 * num12 + num13 * num13 + num14 * num14;
								if ( num15 < num4 )
								{
									num4 = num15;
									num5 = l;
									num6 = m;
									num7 = n;
								}
							}
						}
					}

					break;
				}
			case CellularDistanceFunction.Manhattan:
				{
					for ( int num16 = num - 1; num16 <= num + 1; num16++ )
					{
						for ( int num17 = num2 - 1; num17 <= num2 + 1; num17++ )
						{
							for ( int num18 = num3 - 1; num18 <= num3 + 1; num18++ )
							{
								Float3 float3 = CELL_3D[Hash3D( m_seed, num16, num17, num18 ) & 0xFF];
								float value = (float)num16 - x + float3.x * m_cellularJitter;
								float value2 = (float)num17 - y + float3.y * m_cellularJitter;
								float value3 = (float)num18 - z + float3.z * m_cellularJitter;
								float num19 = Math.Abs( value ) + Math.Abs( value2 ) + Math.Abs( value3 );
								if ( num19 < num4 )
								{
									num4 = num19;
									num5 = num16;
									num6 = num17;
									num7 = num18;
								}
							}
						}
					}

					break;
				}
			case CellularDistanceFunction.Natural:
				{
					for ( int i = num - 1; i <= num + 1; i++ )
					{
						for ( int j = num2 - 1; j <= num2 + 1; j++ )
						{
							for ( int k = num3 - 1; k <= num3 + 1; k++ )
							{
								Float3 @float = CELL_3D[Hash3D( m_seed, i, j, k ) & 0xFF];
								float num8 = (float)i - x + @float.x * m_cellularJitter;
								float num9 = (float)j - y + @float.y * m_cellularJitter;
								float num10 = (float)k - z + @float.z * m_cellularJitter;
								float num11 = Math.Abs( num8 ) + Math.Abs( num9 ) + Math.Abs( num10 ) + (num8 * num8 + num9 * num9 + num10 * num10);
								if ( num11 < num4 )
								{
									num4 = num11;
									num5 = i;
									num6 = j;
									num7 = k;
								}
							}
						}
					}

					break;
				}
		}

		switch ( m_cellularReturnType )
		{
			case CellularReturnType.CellValue:
				return ValCoord3D( m_seed, num5, num6, num7 );
			case CellularReturnType.NoiseLookup:
				{
					Float3 float4 = CELL_3D[Hash3D( m_seed, num5, num6, num7 ) & 0xFF];
					return m_cellularNoiseLookup.GetNoise( (float)num5 + float4.x * m_cellularJitter, (float)num6 + float4.y * m_cellularJitter, (float)num7 + float4.z * m_cellularJitter );
				}
			case CellularReturnType.Distance:
				return num4;
			default:
				return 0f;
		}
	}

	private float SingleCellular2Edge( float x, float y, float z )
	{
		int num = FastRound( x );
		int num2 = FastRound( y );
		int num3 = FastRound( z );
		float[] array = new float[4] { 999999f, 999999f, 999999f, 999999f };
		switch ( m_cellularDistanceFunction )
		{
			case CellularDistanceFunction.Euclidean:
				{
					for ( int l = num - 1; l <= num + 1; l++ )
					{
						for ( int m = num2 - 1; m <= num2 + 1; m++ )
						{
							for ( int n = num3 - 1; n <= num3 + 1; n++ )
							{
								Float3 float2 = CELL_3D[Hash3D( m_seed, l, m, n ) & 0xFF];
								float num8 = (float)l - x + float2.x * m_cellularJitter;
								float num9 = (float)m - y + float2.y * m_cellularJitter;
								float num10 = (float)n - z + float2.z * m_cellularJitter;
								float val2 = num8 * num8 + num9 * num9 + num10 * num10;
								for ( int num11 = m_cellularDistanceIndex1; num11 > 0; num11-- )
								{
									array[num11] = Math.Max( Math.Min( array[num11], val2 ), array[num11 - 1] );
								}

								array[0] = Math.Min( array[0], val2 );
							}
						}
					}

					break;
				}
			case CellularDistanceFunction.Manhattan:
				{
					for ( int num12 = num - 1; num12 <= num + 1; num12++ )
					{
						for ( int num13 = num2 - 1; num13 <= num2 + 1; num13++ )
						{
							for ( int num14 = num3 - 1; num14 <= num3 + 1; num14++ )
							{
								Float3 float3 = CELL_3D[Hash3D( m_seed, num12, num13, num14 ) & 0xFF];
								float value = (float)num12 - x + float3.x * m_cellularJitter;
								float value2 = (float)num13 - y + float3.y * m_cellularJitter;
								float value3 = (float)num14 - z + float3.z * m_cellularJitter;
								float val3 = Math.Abs( value ) + Math.Abs( value2 ) + Math.Abs( value3 );
								for ( int num15 = m_cellularDistanceIndex1; num15 > 0; num15-- )
								{
									array[num15] = Math.Max( Math.Min( array[num15], val3 ), array[num15 - 1] );
								}

								array[0] = Math.Min( array[0], val3 );
							}
						}
					}

					break;
				}
			case CellularDistanceFunction.Natural:
				{
					for ( int i = num - 1; i <= num + 1; i++ )
					{
						for ( int j = num2 - 1; j <= num2 + 1; j++ )
						{
							for ( int k = num3 - 1; k <= num3 + 1; k++ )
							{
								Float3 @float = CELL_3D[Hash3D( m_seed, i, j, k ) & 0xFF];
								float num4 = (float)i - x + @float.x * m_cellularJitter;
								float num5 = (float)j - y + @float.y * m_cellularJitter;
								float num6 = (float)k - z + @float.z * m_cellularJitter;
								float val = Math.Abs( num4 ) + Math.Abs( num5 ) + Math.Abs( num6 ) + (num4 * num4 + num5 * num5 + num6 * num6);
								for ( int num7 = m_cellularDistanceIndex1; num7 > 0; num7-- )
								{
									array[num7] = Math.Max( Math.Min( array[num7], val ), array[num7 - 1] );
								}

								array[0] = Math.Min( array[0], val );
							}
						}
					}

					break;
				}
		}

		return m_cellularReturnType switch
		{
			CellularReturnType.Distance2 => array[m_cellularDistanceIndex1],
			CellularReturnType.Distance2Add => array[m_cellularDistanceIndex1] + array[m_cellularDistanceIndex0],
			CellularReturnType.Distance2Sub => array[m_cellularDistanceIndex1] - array[m_cellularDistanceIndex0],
			CellularReturnType.Distance2Mul => array[m_cellularDistanceIndex1] * array[m_cellularDistanceIndex0],
			CellularReturnType.Distance2Div => array[m_cellularDistanceIndex0] / array[m_cellularDistanceIndex1],
			_ => 0f,
		};
	}

	public float GetCellular( float x, float y )
	{
		x *= m_frequency;
		y *= m_frequency;
		CellularReturnType cellularReturnType = m_cellularReturnType;
		CellularReturnType cellularReturnType2 = cellularReturnType;
		if ( (uint)cellularReturnType2 <= 2u )
		{
			return SingleCellular( x, y );
		}

		return SingleCellular2Edge( x, y );
	}

	private float SingleCellular( float x, float y )
	{
		int num = FastRound( x );
		int num2 = FastRound( y );
		float num3 = 999999f;
		int num4 = 0;
		int num5 = 0;
		switch ( m_cellularDistanceFunction )
		{
			default:
				{
					for ( int k = num - 1; k <= num + 1; k++ )
					{
						for ( int l = num2 - 1; l <= num2 + 1; l++ )
						{
							Float2 float2 = CELL_2D[Hash2D( m_seed, k, l ) & 0xFF];
							float num9 = (float)k - x + float2.x * m_cellularJitter;
							float num10 = (float)l - y + float2.y * m_cellularJitter;
							float num11 = num9 * num9 + num10 * num10;
							if ( num11 < num3 )
							{
								num3 = num11;
								num4 = k;
								num5 = l;
							}
						}
					}

					break;
				}
			case CellularDistanceFunction.Manhattan:
				{
					for ( int m = num - 1; m <= num + 1; m++ )
					{
						for ( int n = num2 - 1; n <= num2 + 1; n++ )
						{
							Float2 float3 = CELL_2D[Hash2D( m_seed, m, n ) & 0xFF];
							float value = (float)m - x + float3.x * m_cellularJitter;
							float value2 = (float)n - y + float3.y * m_cellularJitter;
							float num12 = Math.Abs( value ) + Math.Abs( value2 );
							if ( num12 < num3 )
							{
								num3 = num12;
								num4 = m;
								num5 = n;
							}
						}
					}

					break;
				}
			case CellularDistanceFunction.Natural:
				{
					for ( int i = num - 1; i <= num + 1; i++ )
					{
						for ( int j = num2 - 1; j <= num2 + 1; j++ )
						{
							Float2 @float = CELL_2D[Hash2D( m_seed, i, j ) & 0xFF];
							float num6 = (float)i - x + @float.x * m_cellularJitter;
							float num7 = (float)j - y + @float.y * m_cellularJitter;
							float num8 = Math.Abs( num6 ) + Math.Abs( num7 ) + (num6 * num6 + num7 * num7);
							if ( num8 < num3 )
							{
								num3 = num8;
								num4 = i;
								num5 = j;
							}
						}
					}

					break;
				}
		}

		switch ( m_cellularReturnType )
		{
			case CellularReturnType.CellValue:
				return ValCoord2D( m_seed, num4, num5 );
			case CellularReturnType.NoiseLookup:
				{
					Float2 float4 = CELL_2D[Hash2D( m_seed, num4, num5 ) & 0xFF];
					return m_cellularNoiseLookup.GetNoise( (float)num4 + float4.x * m_cellularJitter, (float)num5 + float4.y * m_cellularJitter );
				}
			case CellularReturnType.Distance:
				return num3;
			default:
				return 0f;
		}
	}

	private float SingleCellular2Edge( float x, float y )
	{
		int num = FastRound( x );
		int num2 = FastRound( y );
		float[] array = new float[4] { 999999f, 999999f, 999999f, 999999f };
		switch ( m_cellularDistanceFunction )
		{
			default:
				{
					for ( int k = num - 1; k <= num + 1; k++ )
					{
						for ( int l = num2 - 1; l <= num2 + 1; l++ )
						{
							Float2 float2 = CELL_2D[Hash2D( m_seed, k, l ) & 0xFF];
							float num6 = (float)k - x + float2.x * m_cellularJitter;
							float num7 = (float)l - y + float2.y * m_cellularJitter;
							float val2 = num6 * num6 + num7 * num7;
							for ( int num8 = m_cellularDistanceIndex1; num8 > 0; num8-- )
							{
								array[num8] = Math.Max( Math.Min( array[num8], val2 ), array[num8 - 1] );
							}

							array[0] = Math.Min( array[0], val2 );
						}
					}

					break;
				}
			case CellularDistanceFunction.Manhattan:
				{
					for ( int m = num - 1; m <= num + 1; m++ )
					{
						for ( int n = num2 - 1; n <= num2 + 1; n++ )
						{
							Float2 float3 = CELL_2D[Hash2D( m_seed, m, n ) & 0xFF];
							float value = (float)m - x + float3.x * m_cellularJitter;
							float value2 = (float)n - y + float3.y * m_cellularJitter;
							float val3 = Math.Abs( value ) + Math.Abs( value2 );
							for ( int num9 = m_cellularDistanceIndex1; num9 > 0; num9-- )
							{
								array[num9] = Math.Max( Math.Min( array[num9], val3 ), array[num9 - 1] );
							}

							array[0] = Math.Min( array[0], val3 );
						}
					}

					break;
				}
			case CellularDistanceFunction.Natural:
				{
					for ( int i = num - 1; i <= num + 1; i++ )
					{
						for ( int j = num2 - 1; j <= num2 + 1; j++ )
						{
							Float2 @float = CELL_2D[Hash2D( m_seed, i, j ) & 0xFF];
							float num3 = (float)i - x + @float.x * m_cellularJitter;
							float num4 = (float)j - y + @float.y * m_cellularJitter;
							float val = Math.Abs( num3 ) + Math.Abs( num4 ) + (num3 * num3 + num4 * num4);
							for ( int num5 = m_cellularDistanceIndex1; num5 > 0; num5-- )
							{
								array[num5] = Math.Max( Math.Min( array[num5], val ), array[num5 - 1] );
							}

							array[0] = Math.Min( array[0], val );
						}
					}

					break;
				}
		}

		return m_cellularReturnType switch
		{
			CellularReturnType.Distance2 => array[m_cellularDistanceIndex1],
			CellularReturnType.Distance2Add => array[m_cellularDistanceIndex1] + array[m_cellularDistanceIndex0],
			CellularReturnType.Distance2Sub => array[m_cellularDistanceIndex1] - array[m_cellularDistanceIndex0],
			CellularReturnType.Distance2Mul => array[m_cellularDistanceIndex1] * array[m_cellularDistanceIndex0],
			CellularReturnType.Distance2Div => array[m_cellularDistanceIndex0] / array[m_cellularDistanceIndex1],
			_ => 0f,
		};
	}

	public void GradientPerturb( ref float x, ref float y, ref float z )
	{
		SingleGradientPerturb( m_seed, m_gradientPerturbAmp, m_frequency, ref x, ref y, ref z );
	}

	public void GradientPerturbFractal( ref float x, ref float y, ref float z )
	{
		int num = m_seed;
		float num2 = m_gradientPerturbAmp * m_fractalBounding;
		float num3 = m_frequency;
		SingleGradientPerturb( num, num2, m_frequency, ref x, ref y, ref z );
		for ( int i = 1; i < m_octaves; i++ )
		{
			num3 *= m_lacunarity;
			num2 *= m_gain;
			SingleGradientPerturb( ++num, num2, num3, ref x, ref y, ref z );
		}
	}

	private void SingleGradientPerturb( int seed, float perturbAmp, float frequency, ref float x, ref float y, ref float z )
	{
		float num = x * frequency;
		float num2 = y * frequency;
		float num3 = z * frequency;
		int num4 = FastFloor( num );
		int num5 = FastFloor( num2 );
		int num6 = FastFloor( num3 );
		int x2 = num4 + 1;
		int y2 = num5 + 1;
		int z2 = num6 + 1;
		float t;
		float t2;
		float t3;
		switch ( m_interp )
		{
			default:
				t = num - (float)num4;
				t2 = num2 - (float)num5;
				t3 = num3 - (float)num6;
				break;
			case Interp.Hermite:
				t = InterpHermiteFunc( num - (float)num4 );
				t2 = InterpHermiteFunc( num2 - (float)num5 );
				t3 = InterpHermiteFunc( num3 - (float)num6 );
				break;
			case Interp.Quintic:
				t = InterpQuinticFunc( num - (float)num4 );
				t2 = InterpQuinticFunc( num2 - (float)num5 );
				t3 = InterpQuinticFunc( num3 - (float)num6 );
				break;
		}

		Float3 @float = CELL_3D[Hash3D( seed, num4, num5, num6 ) & 0xFF];
		Float3 float2 = CELL_3D[Hash3D( seed, x2, num5, num6 ) & 0xFF];
		float a = Lerp( @float.x, float2.x, t );
		float a2 = Lerp( @float.y, float2.y, t );
		float a3 = Lerp( @float.z, float2.z, t );
		@float = CELL_3D[Hash3D( seed, num4, y2, num6 ) & 0xFF];
		float2 = CELL_3D[Hash3D( seed, x2, y2, num6 ) & 0xFF];
		float b = Lerp( @float.x, float2.x, t );
		float b2 = Lerp( @float.y, float2.y, t );
		float b3 = Lerp( @float.z, float2.z, t );
		float a4 = Lerp( a, b, t2 );
		float a5 = Lerp( a2, b2, t2 );
		float a6 = Lerp( a3, b3, t2 );
		@float = CELL_3D[Hash3D( seed, num4, num5, z2 ) & 0xFF];
		float2 = CELL_3D[Hash3D( seed, x2, num5, z2 ) & 0xFF];
		a = Lerp( @float.x, float2.x, t );
		a2 = Lerp( @float.y, float2.y, t );
		a3 = Lerp( @float.z, float2.z, t );
		@float = CELL_3D[Hash3D( seed, num4, y2, z2 ) & 0xFF];
		float2 = CELL_3D[Hash3D( seed, x2, y2, z2 ) & 0xFF];
		b = Lerp( @float.x, float2.x, t );
		b2 = Lerp( @float.y, float2.y, t );
		b3 = Lerp( @float.z, float2.z, t );
		x += Lerp( a4, Lerp( a, b, t2 ), t3 ) * perturbAmp;
		y += Lerp( a5, Lerp( a2, b2, t2 ), t3 ) * perturbAmp;
		z += Lerp( a6, Lerp( a3, b3, t2 ), t3 ) * perturbAmp;
	}

	public void GradientPerturb( ref float x, ref float y )
	{
		SingleGradientPerturb( m_seed, m_gradientPerturbAmp, m_frequency, ref x, ref y );
	}

	public void GradientPerturbFractal( ref float x, ref float y )
	{
		int num = m_seed;
		float num2 = m_gradientPerturbAmp * m_fractalBounding;
		float num3 = m_frequency;
		SingleGradientPerturb( num, num2, m_frequency, ref x, ref y );
		for ( int i = 1; i < m_octaves; i++ )
		{
			num3 *= m_lacunarity;
			num2 *= m_gain;
			SingleGradientPerturb( ++num, num2, num3, ref x, ref y );
		}
	}

	private void SingleGradientPerturb( int seed, float perturbAmp, float frequency, ref float x, ref float y )
	{
		float num = x * frequency;
		float num2 = y * frequency;
		int num3 = FastFloor( num );
		int num4 = FastFloor( num2 );
		int x2 = num3 + 1;
		int y2 = num4 + 1;
		float t;
		float t2;
		switch ( m_interp )
		{
			default:
				t = num - (float)num3;
				t2 = num2 - (float)num4;
				break;
			case Interp.Hermite:
				t = InterpHermiteFunc( num - (float)num3 );
				t2 = InterpHermiteFunc( num2 - (float)num4 );
				break;
			case Interp.Quintic:
				t = InterpQuinticFunc( num - (float)num3 );
				t2 = InterpQuinticFunc( num2 - (float)num4 );
				break;
		}

		Float2 @float = CELL_2D[Hash2D( seed, num3, num4 ) & 0xFF];
		Float2 float2 = CELL_2D[Hash2D( seed, x2, num4 ) & 0xFF];
		float a = Lerp( @float.x, float2.x, t );
		float a2 = Lerp( @float.y, float2.y, t );
		@float = CELL_2D[Hash2D( seed, num3, y2 ) & 0xFF];
		float2 = CELL_2D[Hash2D( seed, x2, y2 ) & 0xFF];
		float b = Lerp( @float.x, float2.x, t );
		float b2 = Lerp( @float.y, float2.y, t );
		x += Lerp( a, b, t2 ) * perturbAmp;
		y += Lerp( a2, b2, t2 ) * perturbAmp;
	}
}
