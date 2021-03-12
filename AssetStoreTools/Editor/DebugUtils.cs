using System;
using System.Diagnostics;

public static class DebugUtils
{
	[Conditional("DEBUG")]
	public static void Debug(string str)
	{
		UnityEngine.Debug.Log(str);
	}

	public static void Log(string str)
	{
		Console.WriteLine("[Asset Store Tools] Log:" + str);
	}

	public static void LogError(string str)
	{
		Console.WriteLine("[Asset Store Tools] LogError:" + str);
	}

	public static void LogWarning(string str)
	{
		Console.WriteLine("[Asset Store Tools] LogWarning:" + str);
	}
}
