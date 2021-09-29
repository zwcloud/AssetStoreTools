using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AssetStoreTools
{
    public class AssetBundler4 : IAssetBundler
    {
        public bool CanPreview()
        {
            return true;
        }

        public bool CanGenerateBundles()
        {
            return true;
        }

        public bool CreateBundle(Object asset, string targetPath)
        {
            return AssetStoreToolUtils.BuildAssetStoreAssetBundle(asset, targetPath);
        }

        public AssetBundleCreateRequest LoadFromMemoryAsync(byte[] bytes)
        {
            List<string> methods = new List<string>
        {
            "UnityEngine.AssetBundle.LoadFromMemoryAsync",
            "UnityEngine.AssetBundle.CreateFromMemory"
        };
            MethodInfo methodInfo = BackwardsCompatibilityUtility.GetMethodInfo(methods, new System.Type[]
            {
            typeof(byte[])
            });
            return (AssetBundleCreateRequest)methodInfo.Invoke(null, new object[]
            {
            bytes
            });
        }

        public void Preview(string assetpath)
        {
            //UnityEditorInternal.AssetStoreToolUtils.PreviewAssetStoreAssetBundleInInspector
            //and
            //UnityEditorInternal.AssetStoreToolUtils.UpdatePreloadingInternal
            //is missing from Unity 2020.x+. It's available in 2019.4 or earlier.
            //So it's impossible for this method to work normally.
#if !UNITY_2020_1_OR_NEWER
            string path = MainAssetsUtil.CreateBundle(assetpath);
            AssetStoreAsset assetStoreAsset = new AssetStoreAsset();
            assetStoreAsset.name = "Preview";
            byte[] bytes = File.ReadAllBytes(path);
            AssetBundleCreateRequest assetBundleCreateRequest = this.LoadFromMemoryAsync(bytes);
            if (assetBundleCreateRequest == null)
            {
                DebugUtils.Log("Unable to generate preview");
                return;
            }
            while (!assetBundleCreateRequest.isDone)
            {
                AssetStoreToolUtils.UpdatePreloadingInternal();
            }
            AssetBundle assetBundle = assetBundleCreateRequest.assetBundle;
            if (assetBundle == null)
            {
                DebugUtils.Log("No bundle at path");
                return;
            }
            AssetStoreToolUtils.PreviewAssetStoreAssetBundleInInspector(assetBundle, assetStoreAsset);
            assetBundle.Unload(false);
            File.Delete(path);
#endif
        }
    }

}