using System;
using System.Reflection;

namespace AssetStoreTools
{
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
                    DebugUtils.Log("AssetBundler runtime Detection");
                    IAssetBundler assetBundler = new AssetBundler4();
                    return assetBundler;
                }
            }
            return new AssetBundler3();
        }
    }

}