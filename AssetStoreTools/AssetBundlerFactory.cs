using System;
using System.Reflection;

public static class AssetBundlerFactory
{
	public static IAssetBundler GetBundler()
	{
		Assembly assembly = Assembly.Load("UnityEditor");
		if (assembly != null)
		{
			Type type = assembly.GetType("UnityEditorInternal.AssetStoreToolUtils");
			if (type != null && type.GetMethod("BuildAssetStoreAssetBundle") != null)
			{
				DebugUtils.Log("AssentBundler runatime Detection");
				IAssetBundler assetBundler = null;
				try
				{
					Assembly assembly2 = Assembly.LoadFrom("Assets\\AssetStoreTools\\Editor\\AssetStoreToolsExtra.dll");
					Type[] types = assembly2.GetTypes();
					Type[] array = types;
					for (int i = 0; i < array.Length; i++)
					{
						Type type2 = array[i];
						DebugUtils.Log(type2.ToString());
					}
					Type type3 = assembly2.GetType("AssetBundler4");
					object obj = Activator.CreateInstance(type3);
					assetBundler = (IAssetBundler)obj;
				}
				catch (Exception ex)
				{
					DebugUtils.LogError("Error Loading Assembly:" + ex.Message);
				}
				if (assetBundler == null)
				{
					DebugUtils.LogError("Error Instantiating bundler");
					assetBundler = new AssetBundler3();
				}
				return assetBundler;
			}
		}
		return new AssetBundler3();
	}
}
