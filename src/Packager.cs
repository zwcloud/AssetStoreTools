using System;
using System.Collections.Generic;
using System.Reflection;

internal static class Packager
{
	internal static string[] CollectAllChildren(string guid, string[] collection)
	{
		List<string> methods = new List<string>
		{
			"UnityEditor.AssetDatabase.CollectAllChildren",
			"UnityEditor.AssetServer.CollectAllChildren"
		};
		return (string[])BackwardsCompatibilityUtility.TryStaticInvoke(methods, new object[]
		{
			guid,
			collection
		});
	}

	internal static void ExportPackage(string[] guids, string fileName)
	{
		List<string> methods = new List<string>
		{
			"UnityEditor.PackageUtility.ExportPackage",
			"UnityEditor.AssetServer.ExportPackage"
		};
		object[] parameters = new object[]
		{
			guids,
			fileName
		};
		BackwardsCompatibilityUtility.TryStaticInvoke(methods, parameters);
	}

	internal static string[] BuildExportPackageAssetListGuids(string[] guids, bool dependencies)
	{
		List<string> methods = new List<string>
		{
			"UnityEditor.PackageUtility.BuildExportPackageItemsList",
			"UnityEditor.AssetServer.BuildExportPackageAssetListAssetsItems"
		};
		MethodInfo methodInfo = BackwardsCompatibilityUtility.GetMethodInfo(methods, new Type[]
		{
			typeof(string[]),
			typeof(bool)
		});
		object[] parameters = new object[]
		{
			guids,
			dependencies
		};
		object[] array = (object[])methodInfo.Invoke(null, parameters);
		string[] array2 = new string[array.Length];
		FieldInfo field = methodInfo.ReturnType.GetElementType().GetField("guid");
		for (int i = 0; i < array.Length; i++)
		{
			string text = (string)field.GetValue(array[i]);
			array2[i] = text;
		}
		return array2;
	}
}
