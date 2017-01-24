using System;
using UnityEditor;
using UnityEngine;

public class AssetBundler3 : IAssetBundler
{
	public bool CanPreview()
	{
		return false;
	}

	public bool CanGenerateBundles()
	{
		return PlayerSettings.advancedLicense;
	}

	public bool CreateBundle(UnityEngine.Object asset, string targetPath)
	{
		return BuildPipeline.BuildAssetBundle(asset, null, targetPath, (BuildAssetBundleOptions)1048576);
	}

	public void Preview(string assetpath)
	{
		throw new NotImplementedException();
	}
}
