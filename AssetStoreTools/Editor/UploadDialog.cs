using System;
using UnityEditor;

internal class UploadDialog
{
	public static void CreateInstance(AssetStorePackageController packageController)
	{
		if (UploadDialog.packageController != null)
		{
			DebugUtils.LogError("New UploadDialog instance being created before an old one has finished");
		}
		UploadDialog.packageController = packageController;
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(UploadDialog.PackageControllerUpdatePump));
	}

	public static bool IsUploading
	{
		get
		{
			return UploadDialog.packageController != null;
		}
	}

	private static void PackageControllerUpdatePump()
	{
		UploadDialog.packageController.Update();
		if (UploadDialog.packageController.IsUploading)
		{
			float getUploadProgress = UploadDialog.packageController.GetUploadProgress;
			string text = string.Format("Uploading {1}... {0}%", (getUploadProgress * 100f).ToString("N0"), UploadDialog.packageController.SelectedPackage.Name);
			string text2 = "Closing this window will stop the ongoing upload process";
			if (EditorUtility.DisplayCancelableProgressBar(text, text2, getUploadProgress))
			{
				UploadDialog.packageController.OnClickUpload();
				UploadDialog.FinishInstance();
			}
		}
		else
		{
			UploadDialog.FinishInstance();
		}
	}

	private static void FinishInstance()
	{
		EditorUtility.ClearProgressBar();
		UploadDialog.packageController = null;
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(UploadDialog.PackageControllerUpdatePump));
		DebugUtils.Log("Upload progress dialog finished it's job");
	}

	private static AssetStorePackageController packageController;
}
