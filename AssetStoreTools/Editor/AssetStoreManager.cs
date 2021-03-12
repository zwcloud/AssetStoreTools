using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

internal class AssetStoreManager : EditorWindow
{
	private AssetStoreManager()
	{
	}

	[MenuItem("Asset Store Tools/Package Upload", false, 0)]
	private static void Launch()
	{
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(AssetStoreClient.Update));
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(AssetStoreClient.Update));
		if (!AssetStoreManager.isOpen)
		{
			AssetStoreManager.Login("To upload packages, please log in to your Asset Store Publisher account.");
		}
	}

	[MenuItem("Asset Store Tools/Package Upload", true, 0)]
	private static bool ValidateLaunch()
	{
		return !UploadDialog.IsUploading;
	}

	private static void Login(string whyMessage)
	{
		if (!LoginWindow.IsLoggedIn)
		{
			LoginWindow.Login(whyMessage, new LoginWindow.LoginCallback(AssetStoreManager.OnLoggedIn));
		}
		else
		{
			AssetStoreManager.OnLoggedIn(null);
		}
	}

	private static void Login(string whyMessage, string errorMessage)
	{
		if (!LoginWindow.IsLoggedIn)
		{
			LoginWindow.ShowLoginWindow(whyMessage, errorMessage, new LoginWindow.LoginCallback(AssetStoreManager.OnLoggedIn));
		}
		else
		{
			AssetStoreManager.OnLoggedIn(null);
		}
	}

	private static void OnLoggedIn(string errorMessage)
	{
		if (errorMessage != null)
		{
			if (!errorMessage.StartsWith("Cancelled"))
			{
				DebugUtils.LogError("Error logging in: " + errorMessage);
			}
			else
			{
				DebugUtils.Log("Closing package manager because login cancelled");
				AssetStoreManager assetStoreManager = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
				assetStoreManager.Close();
			}
			return;
		}
		Console.WriteLine("Asset Store Upload Tool logged in. V5.0.2");
		AssetStoreManager assetStoreManager2 = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
		if (!AssetStoreManager.isOpen)
		{
			AssetStoreManager.isOpen = true;
			assetStoreManager2.Init();
			assetStoreManager2.Show();
		}
		else
		{
			assetStoreManager2.Focus();
		}
	}

	[MenuItem("Asset Store Tools/Publisher Portal", false, 1)]
	private static void LaunchPublisherAdminInExternalBrowser()
	{
		Application.OpenURL("https://publisher.assetstore.unity3d.com/?xunitysession=" + AssetStoreClient.XUnitySession);
	}

	private static void LaunchNewPackageInExternalBrowser()
	{
		Application.OpenURL("https://publisher.assetstore.unity3d.com/packages.html?xunitysession=" + AssetStoreClient.XUnitySession);
	}

	[MenuItem("Asset Store Tools/Submission Guidelines", false, 2)]
	private static void LaunchGuidelinesInExternalBrowser()
	{
		Application.OpenURL("https://unity3d.com/asset-store/sell-assets/submission-guidelines");
	}

	[MenuItem("Asset Store Tools/Log Out", false, 51)]
	private static void AssetStoreLogout()
	{
		if (EditorUtility.DisplayDialog("Logout Confirmation", "Are you sure you want to log out of Asset Store Tools?", "Confirm", "Cancel"))
		{
			DebugUtils.Log("Logged out of Asset Store");
			AssetStoreManager assetStoreManager = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
			assetStoreManager.Close();
			AssetStoreClient.Logout();
		}
	}

	[MenuItem("Asset Store Tools/Log Out", true, 51)]
	private static bool ValidateAssetStoreLogout()
	{
		return LoginWindow.IsLoggedIn;
	}

	internal static AssetStoreManager Window()
	{
		return (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
	}

	private void Init()
	{
		this.Account.Reset();
		this.Account.mStatus = AssetStorePublisher.Status.NotLoaded;
		this.m_PackageDataSource = new PackageDataSource();
		this.m_PackageController = new AssetStorePackageController(this.m_PackageDataSource);
	}

	private void PackageControllerUpdatePump()
	{
		this.m_PackageController.Update();
		if (this.m_PackageController.Dirty)
		{
			this.Repaint();
		}
	}

	private AssetStorePublisher Account
	{
		get
		{
			if (this.m_AccountConfigure == null)
			{
				this.m_AccountConfigure = new AssetStorePublisher();
			}
			return this.m_AccountConfigure;
		}
	}

	public PackageDataSource packageDataSource
	{
		get
		{
			return this.m_PackageDataSource;
		}
		set
		{
			this.m_PackageDataSource = value;
		}
	}

	private void OnEnable()
	{
		this.m_PackageDataSource = new PackageDataSource();
		this.m_PackageController = new AssetStorePackageController(this.m_PackageDataSource);
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(AssetStoreClient.Update));
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(AssetStoreClient.Update));
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(this.PackageControllerUpdatePump));
		Application.runInBackground = true;
		DebugUtils.Log("OnEnable" + base.GetType().ToString());
	}

	private void OnDisable()
	{
		if (this.m_PackageController.IsUploading)
		{
			UploadDialog.CreateInstance(this.m_PackageController);
		}
		EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(this.PackageControllerUpdatePump));
		AssetStoreManager.isOpen = false;
	}

	private void OnGUI()
	{
		Event current = Event.current;
		if (current.commandName == "refresh")
		{
			this.RefreshPackages();
		}
		if (base.minSize.x != this.windowSize.x)
		{
			base.minSize = this.windowSize;
		}
		if (!LoginWindow.IsLoggedIn)
		{
			GUI.enabled = false;
		}
		this.RenderMenu();
		GUILayout.BeginVertical(AssetStoreManager.Styles.MarginBox, new GUILayoutOption[0]);
		if (this.Account.mStatus == AssetStorePublisher.Status.Existing)
		{
			this.m_PackageController.Render();
		}
		else
		{
			GUILayout.FlexibleSpace();
		}
		EditorGUILayout.Space();
		GUILayout.EndVertical();
		this.RenderFooter();
	}

	private void RenderFooter()
	{
		GUILayout.Box(GUIContent.none, GUIUtil.Styles.delimiter, new GUILayoutOption[]
		{
			GUILayout.MinHeight(1f),
			GUILayout.ExpandWidth(true)
		});
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		Color color = GUI.color;
		GUI.color = Color.gray;
		GUILayout.Label("Version " + "V5.0.2".Substring(1), new GUILayoutOption[0]);
		GUI.color = color;
		GUILayout.FlexibleSpace();
		bool enabled = GUI.enabled;
		GUI.enabled = this.m_PackageController.CanUpload;
		if (GUILayout.Button("Upload", new GUILayoutOption[]
		{
			GUILayout.Width(100f),
			GUILayout.Height(30f)
		}))
		{
			this.m_PackageController.OnClickUpload();
		}
		GUI.enabled = enabled;
		GUILayout.Space(10f);
		GUILayout.EndHorizontal();
		GUILayout.Space(6f);
	}

	private void RefreshPackages()
	{
		this.Account.mStatus = AssetStorePublisher.Status.Loading;
		AssetStoreAPI.GetMetaData(this.Account, this.m_PackageDataSource, delegate(string errMessage)
		{
			if (errMessage != null)
			{
				Debug.LogError("Error fetching metadata: " + errMessage);
				LoginWindow.Logout();
				AssetStoreManager.Login("To upload packages, please log in to your Asset Store Publisher account.");
				this.Repaint();
				return;
			}
			this.m_PackageController.AutoSetSelected(this);
			this.Repaint();
		});
	}

	private void RenderToolbar()
	{
		GUILayout.FlexibleSpace();
		bool enabled = GUI.enabled;
		GUI.enabled = !this.m_PackageController.IsUploading;
		if (GUILayout.Button("Refresh Packages", EditorStyles.toolbarButton, new GUILayoutOption[0]))
		{
			this.RefreshPackages();
		}
		if (AssetStoreManager.sDbgButtons)
		{
			GUILayout.Label("Debug: ", AssetStoreManager.Styles.ToolbarLabel, new GUILayoutOption[0]);
			if (GUILayout.Button("FileSelector", EditorStyles.toolbarButton, new GUILayoutOption[0]))
			{
				FileSelector.Show("/", new List<string>(), delegate(List<string> newList)
				{
					foreach (string str in newList)
					{
						DebugUtils.Log(str);
					}
				});
			}
			if (GUILayout.Button("Logout", EditorStyles.toolbarButton, new GUILayoutOption[0]))
			{
				AssetStoreClient.Logout();
			}
		}
		GUI.enabled = enabled;
	}

	private void RenderMenu()
	{
		GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
		GUILayout.FlexibleSpace();
		this.RenderToolbar();
		GUILayout.EndHorizontal();
		bool flag = false;
		if (!LoginWindow.IsLoggedIn)
		{
			if (!LoginWindow.IsVisible)
			{
				LoginWindow.Login("To upload packages, please log in to your Asset Store Publisher account.", new LoginWindow.LoginCallback(AssetStoreManager.OnLoggedIn), GUIUtil.RectOnRect(360f, 180f, base.position));
			}
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			GUILayout.Space(10f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			GUILayout.Label(GUIUtil.StatusWheel, new GUILayoutOption[0]);
			GUILayout.Label("Please login", new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
		else
		{
			if (this.Account.mStatus == AssetStorePublisher.Status.NotLoaded)
			{
				this.Account.mStatus = AssetStorePublisher.Status.Loading;
				AssetStoreAPI.GetMetaData(this.Account, this.m_PackageDataSource, delegate(string errMessage)
				{
					if (errMessage != null)
					{
						Debug.LogError("Error fetching metadata: " + errMessage);
						LoginWindow.Logout();
						AssetStoreManager.Login("To upload packages, please log in to your Asset Store Publisher account.", "Account is not registered as a Publisher. \nPlease create a Publisher ID.");
						this.Repaint();
						return;
					}
					this.m_PackageController.AutoSetSelected(this);
					this.Repaint();
				});
			}
			if (this.Account.mStatus == AssetStorePublisher.Status.NotLoaded || this.Account.mStatus == AssetStorePublisher.Status.Loading)
			{
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				GUILayout.Space(10f);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				GUILayout.Label(GUIUtil.StatusWheel, new GUILayoutOption[0]);
				GUILayout.Label("Fetching account information", new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				this.Repaint();
			}
			else
			{
				bool enabled = GUI.enabled;
				if (this.Account.mStatus != AssetStorePublisher.Status.Existing)
				{
					GUI.enabled = false;
				}
				GUI.enabled = enabled;
				flag = true;
			}
		}
		if (!flag && this.m_PackageController.SelectedPackage != null)
		{
			this.m_PackageController.SelectedPackage = null;
		}
	}

	private void OnDestroy()
	{
		DebugUtils.Log("OnDestroy " + base.GetType().ToString());
	}

	private const string windowTitle = "Package Upload";

	private const string loginInfoMessage = "To upload packages, please log in to your Asset Store Publisher account.";

	internal static bool sDbg;

	internal static bool sDbgButtons;

	internal static bool isOpen;

	internal readonly Vector2 windowSize = new Vector2(530f, 540f);

	internal static Thread sUploadThread;

	private AssetStorePublisher m_AccountConfigure = new AssetStorePublisher();

	private AssetStorePackageController m_PackageController;

	private PackageDataSource m_PackageDataSource = new PackageDataSource();

	private static class Styles
	{
		static Styles()
		{
			AssetStoreManager.Styles.MarginBox.padding.top = 5;
			AssetStoreManager.Styles.MarginBox.padding.right = 15;
			AssetStoreManager.Styles.MarginBox.padding.bottom = 5;
			AssetStoreManager.Styles.MarginBox.padding.left = 15;
			AssetStoreManager.Styles.ToolbarLabel.padding = new RectOffset(0, 1, 2, 0);
		}

		public static GUIStyle MarginBox = new GUIStyle();

		public static GUIStyle ToolbarLabel = new GUIStyle("MiniLabel");
	}
}
