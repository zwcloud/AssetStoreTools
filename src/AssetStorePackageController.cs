using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal class AssetStorePackageController
{
	internal AssetStorePackageController(PackageDataSource packageDataSource)
	{
		this.m_PkgSelectionCtrl = new PackageSelector(packageDataSource, new ListView<Package>.SelectionCallback(this.OnPackageSelected));
		this.ClearLocalState();
	}

	public bool Dirty
	{
		get
		{
			return this.m_Dirty;
		}
	}

	public Package SelectedPackage
	{
		get
		{
			return this.m_Package;
		}
		set
		{
			this.m_Package = value;
			this.m_PkgSelectionCtrl.Selected = this.m_Package;
			this.ClearLocalState();
		}
	}

	private bool HasUnsavedChanges()
	{
		return this.m_UnsavedChanges;
	}

	private void ClearLocalState()
	{
		this.m_AssetsState = AssetStorePackageController.AssetsState.None;
		this.m_DraftAssetsUploadProgress = 0f;
		this.m_DraftAssetsPath = string.Empty;
		this.m_DraftAssetsSize = 0L;
		this.m_DraftAssetsFileInfo = null;
		this.m_UnsavedChanges = false;
		if (this.m_Package == null)
		{
			this.m_MainAssets = new List<string>();
			this.m_LocalProjectPath = string.Empty;
			this.m_LocalRootPath = string.Empty;
			this.m_LocalRootGUID = string.Empty;
		}
		else
		{
			this.m_MainAssets = new List<string>(this.m_Package.MainAssets);
			this.m_LocalProjectPath = this.m_Package.ProjectPath;
			this.m_LocalRootPath = this.m_Package.RootPath;
			this.m_LocalRootGUID = this.m_Package.RootGUID;
		}
	}

	private bool IsValidProjectFolder(string directory)
	{
		return Application.dataPath.Length <= directory.Length && !(directory.Substring(0, Application.dataPath.Length) != Application.dataPath) && Directory.Exists(directory);
	}

	private bool IsValidRelativeProjectFolder(string relativeDirectory)
	{
		return this.IsValidProjectFolder(Application.dataPath + relativeDirectory);
	}

	internal void Render()
	{
		this.m_Dirty = false;
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		this.m_Scroll = GUILayout.BeginScrollView(this.m_Scroll, new GUILayoutOption[0]);
		bool enabled = GUI.enabled;
		if (this.m_AssetsState != AssetStorePackageController.AssetsState.None)
		{
			GUI.enabled = false;
		}
		this.RenderPackageSelection();
		GUI.enabled = enabled;
		EditorGUILayout.Space();
		if (this.m_Package != null)
		{
			this.RenderSettings();
			this.RenderUpload();
		}
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
	}

	private void RenderUpload()
	{
		GUILayout.Label("4. Upload package", new GUILayoutOption[0]);
		bool enabled = GUI.enabled;
		if (string.IsNullOrEmpty(this.m_Package.Name) || string.IsNullOrEmpty(this.m_Package.VersionName) || this.m_AssetsState != AssetStorePackageController.AssetsState.None || string.IsNullOrEmpty(this.m_LocalRootPath) || !this.IsValidRelativeProjectFolder(this.m_LocalRootPath) || this.m_Package.Status != Package.PublishedStatus.Draft)
		{
			GUI.enabled = false;
		}
		if (GUILayout.Button((this.m_AssetsState != AssetStorePackageController.AssetsState.UploadingPackage) ? "Upload" : "Stop", new GUILayoutOption[]
		{
			GUILayout.Width(90f)
		}))
		{
			if (this.m_AssetsState == AssetStorePackageController.AssetsState.UploadingPackage)
			{
				AssetStoreClient.AbortLargeFilesUpload();
				this.m_AssetsState = AssetStorePackageController.AssetsState.None;
			}
			else
			{
				this.Upload();
				GUIUtility.ExitGUI();
			}
		}
		GUI.enabled = enabled;
	}

	private void RenderSettings()
	{
		EditorGUILayout.Space();
		GUILayout.Label("2. Select assets folder", new GUILayoutOption[0]);
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		bool enabled = GUI.enabled;
		if (this.m_Package == null || this.m_Package.Status != Package.PublishedStatus.Draft || this.m_AssetsState != AssetStorePackageController.AssetsState.None)
		{
			GUI.enabled = false;
		}
		if (GUILayout.Button("Select...", new GUILayoutOption[]
		{
			GUILayout.Width(90f)
		}))
		{
			this.ChangeRootPathDialog();
		}
		GUI.enabled = enabled;
		this.RenderAssetsFolderStatus();
		GUILayout.EndHorizontal();
		EditorGUILayout.Space();
		if (MainAssetsUtil.CanGenerateBundles)
		{
			GUILayout.Label("3. Select main assets", new GUILayoutOption[0]);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			bool enabled2 = GUI.enabled;
			if (this.m_Package == null || this.m_Package.Status != Package.PublishedStatus.Draft || string.IsNullOrEmpty(this.m_LocalRootPath) || !this.IsValidRelativeProjectFolder(this.m_LocalRootPath) || this.m_AssetsState != AssetStorePackageController.AssetsState.None)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button("Select...", new GUILayoutOption[]
			{
				GUILayout.Width(90f)
			}))
			{
				MainAssetsUtil.ShowManager(this.m_LocalRootPath, this.m_MainAssets, delegate(List<string> updatedMainAssets)
				{
					this.m_Dirty = true;
					this.m_MainAssets = updatedMainAssets;
					this.m_UnsavedChanges = true;
				});
			}
			GUI.enabled = enabled2;
			this.RenderMainAssetsStatus();
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.Space();
	}

	private void RenderAssetsFolderStatus()
	{
		if (this.m_AssetsState == AssetStorePackageController.AssetsState.UploadingPackage)
		{
			string str = ((int)Mathf.Ceil(this.m_DraftAssetsUploadProgress * 100f)).ToString();
			if (AssetStorePackageController.CancelableProgressBar(this.m_DraftAssetsUploadProgress, "Uploading " + str + " %", "Cancel"))
			{
				this.m_DraftAssetsUploadProgress = 0f;
				this.m_AssetsState = AssetStorePackageController.AssetsState.None;
				this.m_DraftAssetsPath = string.Empty;
				this.m_DraftAssetsSize = 0L;
				AssetStoreClient.AbortLargeFilesUpload();
			}
		}
		else if (this.m_AssetsState == AssetStorePackageController.AssetsState.BuildingPackage)
		{
			GUILayout.Label(GUIUtil.StatusWheel, new GUILayoutOption[0]);
			GUILayout.Label("Please wait - building package", new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
		}
		else
		{
			Color color = GUI.color;
			string text = "No assets selected";
			if (this.m_LocalRootPath != null)
			{
				if (!this.IsValidRelativeProjectFolder(this.m_LocalRootPath))
				{
					GUI.color = Color.red;
				}
				text = " " + this.m_LocalRootPath;
			}
			GUILayout.Label(text, new GUILayoutOption[0]);
			GUI.color = color;
			GUILayout.FlexibleSpace();
		}
	}

	private void RenderMainAssetsStatus()
	{
		if (this.m_AssetsState == AssetStorePackageController.AssetsState.BuildingMainAssets)
		{
			GUILayout.Label(GUIUtil.StatusWheel, new GUILayoutOption[0]);
			GUILayout.Label("Please wait - building Main Assets", new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
		}
		else if (this.m_AssetsState == AssetStorePackageController.AssetsState.UploadingMainAssets)
		{
			float num = (float)this.m_MainAssetsUploadHelper.GetProgress();
			string str = Mathf.FloorToInt(num).ToString();
			if (AssetStorePackageController.CancelableProgressBar(num, "Uploading " + str + " %", "Cancel"))
			{
				this.m_AssetsState = AssetStorePackageController.AssetsState.None;
				this.m_DraftAssetsPath = string.Empty;
				this.m_DraftAssetsSize = 0L;
				AssetStoreClient.AbortLargeFilesUpload();
			}
		}
		else
		{
			string text;
			if (this.m_Package != null && !string.IsNullOrEmpty(this.m_LocalRootPath) && this.IsValidRelativeProjectFolder(this.m_LocalRootPath))
			{
				if (this.m_MainAssets.Count == 0)
				{
					text = "No Files Selected";
				}
				else if (this.m_MainAssets.Count == 1)
				{
					text = this.m_MainAssets[0] + " selected";
				}
				else
				{
					text = this.m_MainAssets.Count + " File(s) selected";
				}
			}
			else
			{
				text = "Please select a valid Assets folder";
			}
			GUILayout.Label(text, new GUILayoutOption[0]);
		}
	}

	private void ChangeRootPathDialog()
	{
		string text = EditorUtility.OpenFolderPanel("Select root folder of package", Application.dataPath, string.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			if (!this.IsValidProjectFolder(text))
			{
				EditorUtility.DisplayDialog("Wrong project path", "The path selected must be inside the currently active project. Note that the AssetStoreTools folder is removed automatically before the package enters the asset store", "Ok");
				return;
			}
			this.SetRootPath(text);
		}
	}

	private void SetRootPath(string path)
	{
		this.m_UnsavedChanges = true;
		this.m_LocalProjectPath = Application.dataPath;
		this.m_LocalRootPath = path.Substring(Application.dataPath.Length);
		if (this.m_LocalRootPath == string.Empty)
		{
			this.m_LocalRootPath = "/";
		}
		if (this.m_Package.RootPath != this.m_LocalRootPath)
		{
			this.m_MainAssets.Clear();
		}
	}

	private string CheckContent()
	{
		string text = string.Empty;
		string[] guids = this.GetGUIDS(this.NeedProjectSettings());
		foreach (string text2 in guids)
		{
			string text3 = AssetDatabase.GUIDToAssetPath(text2);
			foreach (string value in AssetStorePackageController.kForbiddenExtensions)
			{
				if (text3.EndsWith(value))
				{
					if (text != string.Empty)
					{
						text += "\n";
					}
					text = text + "Unallowed file type: " + text3;
				}
			}
		}
		return text;
	}

	private bool NeedProjectSettings()
	{
		return this.m_Package.IsCompleteProjects;
	}

	private void Export(string toPath)
	{
		File.Delete(toPath);
		this.m_AssetsState = AssetStorePackageController.AssetsState.BuildingPackage;
		Packager.ExportPackage(this.GetGUIDS(this.NeedProjectSettings()), toPath, this.NeedProjectSettings());
	}

	private static string GetLocalRootGUID(Package package)
	{
		string text = ("Assets" + (package.RootPath ?? string.Empty)).Trim(new char[]
		{
			'/'
		});
		return AssetDatabase.AssetPathToGUID(text);
	}

	private string[] GetGUIDS(bool includeProjectSettings)
	{
		string[] collection = new string[0];
		string text = ("Assets" + (this.m_LocalRootPath ?? string.Empty)).Trim(new char[]
		{
			'/'
		});
		string guid = AssetDatabase.AssetPathToGUID(text);
		string[] guids = Packager.CollectAllChildren(guid, collection);
		string[] array = Packager.BuildExportPackageAssetListGuids(guids, true);
		List<string> list = new List<string>();
		string value = text.ToLower();
		foreach (string text2 in array)
		{
			string text3 = AssetDatabase.GUIDToAssetPath(text2).ToLower();
			if (text3.StartsWith("assets/plugins") || text3.Contains("standard assets") || text3.StartsWith(value))
			{
				list.Add(text2);
			}
		}
		if (includeProjectSettings)
		{
			string[] files = Directory.GetFiles("ProjectSettings");
			foreach (string text4 in files)
			{
				string text5 = AssetDatabase.AssetPathToGUID(text4);
				if (text5.Length > 0)
				{
					list.Add(text5);
				}
			}
		}
		string[] array4 = new string[list.Count];
		list.CopyTo(array4);
		return array4;
	}

	private static bool CancelableProgressBar(float progress, string message, string buttonText)
	{
		Rect rect = GUILayoutUtility.GetRect(200f, 19f);
		EditorGUI.ProgressBar(rect, progress, message);
		bool result = GUILayout.Button(buttonText, new GUILayoutOption[0]);
		GUILayout.FlexibleSpace();
		return result;
	}

	private void CheckForPackageBuild()
	{
		if (this.m_AssetsState != AssetStorePackageController.AssetsState.BuildingPackage)
		{
			return;
		}
		if (this.m_DraftAssetsFileInfo == null)
		{
			this.m_DraftAssetsFileInfo = new FileInfo(this.m_DraftAssetsPath);
		}
		if (this.m_DraftAssetsLastCheckTime + 2.0 <= EditorApplication.timeSinceStartup)
		{
			this.m_DraftAssetsLastCheckTime = EditorApplication.timeSinceStartup;
			this.m_DraftAssetsFileInfo.Refresh();
			if (this.m_DraftAssetsFileInfo.Exists && this.m_DraftAssetsFileInfo.Length == this.m_DraftAssetsSize && this.m_DraftAssetsFileInfo.Length != 0L)
			{
				this.UploadPackage();
			}
			else if (this.m_DraftAssetsFileInfo.Exists)
			{
				this.m_DraftAssetsSize = this.m_DraftAssetsFileInfo.Length;
			}
		}
	}

	private void UploadPackage()
	{
		DebugUtils.Log("UploadPackage");
		this.m_AssetsState = AssetStorePackageController.AssetsState.UploadingPackage;
		if (string.IsNullOrEmpty(this.m_DraftAssetsPath))
		{
			DebugUtils.LogError("No assets to upload has been selected");
			this.m_AssetsState = AssetStorePackageController.AssetsState.None;
			return;
		}
		AssetStoreAPI.UploadAssets(this.m_Package, this.m_LocalRootGUID, this.m_LocalRootPath, this.m_LocalProjectPath, this.m_DraftAssetsPath, new AssetStoreAPI.DoneCallback(this.OnAssetsUploaded), new AssetStoreClient.ProgressCallback(this.OnAssetsUploading));
	}

	private void OnAssetsUploaded(string errorMessage)
	{
		DebugUtils.Log("OnAssetsUploaded" + (errorMessage ?? string.Empty));
		this.m_AssetsState = AssetStorePackageController.AssetsState.None;
		this.m_DraftAssetsPath = string.Empty;
		this.m_DraftAssetsFileInfo = null;
		this.m_Dirty = true;
		if (errorMessage != null)
		{
			if (errorMessage != "aborted")
			{
				EditorUtility.DisplayDialog("Error uploading assets", "An error occurred during assets upload\nPlease retry. " + errorMessage, "Close");
			}
			return;
		}
        //CHANGED: Don't call MainAssetsUtil.CanGenerateBundles to remove the second messed progress bar.
		this.OnUploadSuccessfull();
	}

	private void OnAssetsUploading(double pctUp, double pctDown)
	{
		this.m_DraftAssetsUploadProgress = (float)(pctUp / 100.0);
		this.m_Dirty = true;
	}

	private void UploadAssetBundles()
	{
		this.m_AssetsState = AssetStorePackageController.AssetsState.BuildingMainAssets;
		if (this.m_MainAssets.Count == 0)
		{
			this.OnUploadAssetBundlesFinished(null);
			return;
		}
		this.m_MainAssetsUploadHelper = new MainAssetsUploadHelper(this, this.m_MainAssets, new Action<string>(this.OnUploadAssetBundlesFinished));
		this.m_MainAssetsUploadHelper.GenerateAssetBundles();
		this.m_AssetsState = AssetStorePackageController.AssetsState.UploadingMainAssets;
		this.m_MainAssetsUploadHelper.UploadAllAssetBundles();
		this.m_Dirty = true;
	}

	private void OnUploadAssetBundlesFinished(string errorMessage)
	{
		this.m_AssetsState = AssetStorePackageController.AssetsState.None;
		this.m_MainAssetsUploadHelper = null;
		DebugUtils.Log("OnUploadAssetBundlesFinished");
		if (errorMessage != null)
		{
			EditorUtility.DisplayDialog("Error uploading previews", errorMessage, "Ok");
			return;
		}
		this.OnUploadSuccessfull();
	}

	private void Upload()
	{
		DebugUtils.Log("Upload");
		if (this.m_LocalRootPath == null)
		{
			EditorUtility.DisplayDialog("Package Assets folder not set", "You haven't set the Asset Folder yet. ", "Ok");
			return;
		}
		DebugUtils.Log(Application.dataPath + this.m_LocalRootPath);
		if (!Directory.Exists(Application.dataPath + this.m_LocalRootPath))
		{
			EditorUtility.DisplayDialog("Project not found!", "The root folder you selected does not exist in the current project.\nPlease make sure you have the correct project open or you have selected the right root folder", "Ok");
			return;
		}
		this.m_DraftAssetsUploadProgress = 0f;
		this.m_LocalProjectPath = Application.dataPath;
		this.m_LocalRootGUID = AssetStorePackageController.GetLocalRootGUID(this.m_Package);
		string text = this.CheckContent();
		if (string.IsNullOrEmpty(text))
		{
			this.m_DraftAssetsPath = "Temp/uploadtool_" + this.m_LocalRootPath.Trim(new char[]
			{
				'/'
			}).Replace('/', '_') + ".unitypackage";
			this.m_DraftAssetsSize = 0L;
			this.m_DraftAssetsLastCheckTime = EditorApplication.timeSinceStartup;
			this.m_AssetsState = AssetStorePackageController.AssetsState.InitiateBuilding;
			this.m_Dirty = true;
			return;
		}
		string text2 = AssetStorePackageController.kForbiddenExtensions[0];
		for (int i = 1; i < AssetStorePackageController.kForbiddenExtensions.Length; i++)
		{
			text2 = text2 + ", " + AssetStorePackageController.kForbiddenExtensions[i];
		}
		Debug.LogWarning(text);
		EditorUtility.DisplayDialog("Invalid files", "Your project contains file types that are not allowed in the AssetStore.\nPlease remove files with the following extensions:\n" + text2 + "\nYou can find more details in the console.", "Ok");
	}

	private void OnUploadSuccessfull()
	{
		this.m_AssetsState = AssetStorePackageController.AssetsState.AllUploadsFinished;
		this.m_Dirty = true;
	}

	private void OnSubmitionFail()
	{
		this.m_AssetsState = AssetStorePackageController.AssetsState.None;
		this.m_Dirty = true;
	}

	private void ShowUploadSucessfull()
	{
		EditorUtility.DisplayDialog("Upload successful", "The package has been uploaded please visit the Publisher Administration to submit this version for review.", "Ok");
		this.ClearLocalState();
		DebugUtils.Log("Closing package manager after successful submission");
		AssetStoreManager assetStoreManager = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "AssetStoreMgr");
		assetStoreManager.Close();
	}

	public void Update()
	{
		if (this.m_AssetsState != AssetStorePackageController.AssetsState.None && this.m_AssetsState != AssetStorePackageController.AssetsState.AllUploadsFinished)
		{
			this.m_Dirty = true;
		}
		switch (this.m_AssetsState)
		{
		case AssetStorePackageController.AssetsState.InitiateBuilding:
			this.Export(this.m_DraftAssetsPath);
			break;
		case AssetStorePackageController.AssetsState.BuildingPackage:
			this.CheckForPackageBuild();
			break;
		case AssetStorePackageController.AssetsState.AllUploadsFinished:
			if (!this.m_Dirty)
			{
				this.ShowUploadSucessfull();
			}
			break;
		}
	}

	public void AutoSetSelected(AssetStoreManager assetStoreManager)
	{
		if (this.Dirty)
		{
			return;
		}
		this.SelectedPackage = null;
		List<Package> list = new List<Package>();
		IList<Package> allPackages = assetStoreManager.packageDataSource.GetAllPackages();
		foreach (Package item in allPackages)
		{
			list.Add(item);
		}
		list.RemoveAll((Package pc) => string.IsNullOrEmpty(pc.RootGUID) || pc.RootGUID != AssetStorePackageController.GetLocalRootGUID(pc));
		if (list.Count == 1)
		{
			this.SelectedPackage = list[0];
			return;
		}
		if (list.Count == 0)
		{
			foreach (Package item2 in allPackages)
			{
				list.Add(item2);
			}
		}
		list.RemoveAll((Package pc) => pc.RootPath == null || (Application.dataPath != pc.ProjectPath && !Directory.Exists(Application.dataPath + pc.RootPath)));
		if (list.Count == 1)
		{
			this.SelectedPackage = list[0];
			return;
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (Package package in list)
		{
			if (package.RootPath != null && Directory.Exists(Application.dataPath + package.RootPath) && Application.dataPath == package.ProjectPath)
			{
				this.SelectedPackage = package;
				return;
			}
		}
		if (this.SelectedPackage != null)
		{
			return;
		}
		foreach (Package package2 in list)
		{
			if (package2.RootPath != null && Directory.Exists(Application.dataPath + package2.RootPath))
			{
				this.SelectedPackage = package2;
				return;
			}
		}
		if (this.SelectedPackage != null)
		{
			return;
		}
		foreach (Package package3 in list)
		{
			if (package3.ProjectPath != null && package3.ProjectPath == Application.dataPath)
			{
				this.SelectedPackage = package3;
				break;
			}
		}
	}

	private void OnPackageSelected(Package pkg)
	{
		if (this.HasUnsavedChanges() && !EditorUtility.DisplayDialog("Change working package", "The package you currently have open has unsaved changes, would you like to discard the changes and view another package?", "Ok", "Cancel"))
		{
			this.m_PkgSelectionCtrl.Selected = this.SelectedPackage;
			return;
		}
		this.SelectedPackage = pkg;
		this.m_Dirty = true;
	}

	private void RenderPackageSelection()
	{
		this.m_PkgSelectionCtrl.Render(200);
	}

	private Package m_Package;

	private Vector2 m_Scroll;

	private PackageSelector m_PkgSelectionCtrl;

	private List<string> m_MainAssets;

	private AssetStorePackageController.AssetsState m_AssetsState;

	private float m_DraftAssetsUploadProgress;

	private string m_DraftAssetsPath;

	private long m_DraftAssetsSize;

	private FileInfo m_DraftAssetsFileInfo;

	private double m_DraftAssetsLastCheckTime;

	private bool m_Dirty;

	private bool m_UnsavedChanges;

	private string m_LocalProjectPath;

	private string m_LocalRootPath;

	private string m_LocalRootGUID;

	private MainAssetsUploadHelper m_MainAssetsUploadHelper;

	private static string[] kForbiddenExtensions = new string[]
	{
		".mb",
		".ma",
		".max",
		".c4d",
		".blend",
		".3ds",
		".jas",
		".dds",
		".pvr"
	};

	private enum AssetsState
	{
		None,
		InitiateBuilding,
		BuildingPackage,
		UploadingPackage,
		BuildingMainAssets,
		UploadingMainAssets,
		AllUploadsFinished
	}
}
