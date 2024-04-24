using Sandbox.Utility;
using System;
public sealed class GrassObject
{

	public GameObject gameObject = null;

	public bool exists()
	{
		return gameObject != null;
	}

}
