using System;
using System.Collections.Generic;
using System.Reflection;

public class BackwardsCompatibilityUtility
{
	public static MethodInfo GetMethodInfo(List<string> methods, Type[] parametersType = null)
	{
		MethodInfo methodInfo = null;
		foreach (string text in methods)
		{
			string[] array = text.Split(new char[]
			{
				'.'
			});
			Assembly assembly = Assembly.Load(array[0]);
			string name = string.Format("{0}.{1}", array[0], array[1]);
			string name2 = array[2];
			Type type = assembly.GetType(name);
			if (type != null)
			{
				if (parametersType == null)
				{
					methodInfo = type.GetMethod(name2, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}
				else
				{
					methodInfo = type.GetMethod(name2, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parametersType, null);
				}
			}
			if (methodInfo != null)
			{
				break;
			}
		}
		if (methodInfo == null)
		{
			throw new MissingMethodException(methods[0]);
		}
		return methodInfo;
	}

	public static object TryStaticInvoke(List<string> methods, object[] parameters)
	{
		MethodInfo methodInfo = BackwardsCompatibilityUtility.GetMethodInfo(methods, null);
		return methodInfo.Invoke(null, parameters);
	}
}
