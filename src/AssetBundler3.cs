using System;

namespace AssetStoreTools
{
    public class AssetBundler3 : IAssetBundler
    {
        public bool CanPreview()
        {
            return false;
        }

        public bool CanGenerateBundles()
        {
            return false; // old code: PlayerSettings.advancedLicense;
        }

        public bool CreateBundle(UnityEngine.Object asset, string targetPath)
        {
            // BuildPipeline.BuildAssetBundle is obsolete and this AssetBundler is just a fallback.
            // So we can just say "no".
            return false;

            // return BuildPipeline.BuildAssetBundle(asset, null, targetPath, (BuildAssetBundleOptions)1048576);
        }

        public void Preview(string assetpath)
        {
            throw new NotImplementedException();
        }
    }

}