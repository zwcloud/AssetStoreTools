using System;
using System.Reflection;
using UnityEditor;

internal static class Packager
{
	internal static string[] CollectAllChildren(string guid, string[] collection)
	{
		return AssetServer.CollectAllChildren(guid, collection);
	}

	internal static string GetRootGUID()
	{
		return AssetServer.GetRootGUID();
	}

	internal static void ExportPackage(string[] guids, string fileName)
	{
		Assembly assembly = Assembly.Load("UnityEditor");
		Type type = assembly.GetType("UnityEditor.AssetServer");
		MethodInfo method = type.GetMethod("ExportPackage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (method == null)
		{
			type = assembly.GetType("PackageUtility");
			if (type != null)
			{
				method = type.GetMethod("ExportPackage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}
		if (method != null)
		{
			object[] parameters = new object[]
			{
				guids,
				fileName
			};
			method.Invoke(null, parameters);
			return;
		}
		throw new MissingMethodException("Packager", "ExportPackage");
	}

	internal static AssetsItem[] BuildExportPackageAssetListAssetsItems(string[] guids, bool dependencies)
	{
		return AssetServer.BuildExportPackageAssetListAssetsItems(guids, dependencies);
	}
}
