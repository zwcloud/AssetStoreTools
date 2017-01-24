using System;
using System.Runtime.CompilerServices;

namespace UnityEditor
{
	internal sealed class AssetServer
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string[] CollectAllChildren(string guid, string[] collection);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string GetRootGUID();

		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern AssetsItem[] BuildExportPackageAssetListAssetsItems(string[] guids, bool dependencies);
	}
}
