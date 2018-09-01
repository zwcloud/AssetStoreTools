public interface IAssetBundler
{
	bool CanPreview();

	bool CanGenerateBundles();

	bool CreateBundle(UnityEngine.Object asset, string targetPath);

	void Preview(string assetpath);
}
