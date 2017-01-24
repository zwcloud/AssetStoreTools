using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

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

	public bool CreateBundle(UnityEngine.Object asset, string targetPath)
	{
		return AssetStoreToolUtils.BuildAssetStoreAssetBundle(asset, targetPath);
	}

	public void Preview(string assetpath)
	{
		string path = MainAssetsUtil.CreateBundle(assetpath);
		AssetStoreAsset assetStoreAsset = new AssetStoreAsset();
		assetStoreAsset.name = "Preview";
		byte[] binary = File.ReadAllBytes(path);
		AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(binary);
		if (assetBundleCreateRequest == null)
		{
			DebugUtils.Log("Unable to generate preview");
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
	}
}
