using UnityEngine;

public interface IAssetBundler
{
	bool CanPreview();

	bool CanGenerateBundles();

	bool CreateBundle(Object asset, string targetPath);

	void Preview(string assetpath);
}
