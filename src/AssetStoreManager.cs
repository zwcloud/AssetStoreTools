using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#pragma warning disable 0649 // "field is never assigned" warning

namespace AssetStoreTools
{
    internal class AssetStoreManager : EditorWindow
    {
        private AssetStoreManager()
        {
        }

        [MenuItem("Asset Store Tools/Package Upload", false, 0)]
        private static void Launch()
        {
#if !UNITY_2017 && !UNITY_2018
            if (Application.webSecurityEnabled)
            {
                bool flag = EditorUtility.DisplayDialog("Web player platform active", "You are currently using the Web Player platform. To upload Asset Store packages please switch platform to PC and Mac standalone in File -> Build Settings...", "Switch my Active Platform.", "Cancel");
                if (!flag)
                {
                    return;
                }
                try
                {
                    Assembly assembly = Assembly.Load("UnityEditor");
                    Type type = assembly.GetType("UnityEditor.EditorUserBuildSettings");
                    MethodInfo method = type.GetMethod("SwitchActiveBuildTarget");
                    method.Invoke(null, new object[]
                    {
                    EditorUserBuildSettings.selectedStandaloneTarget
                    });
                }
                catch
                {
                    DebugUtils.LogError("Unable to invoke UnityEditor.EditorUserBuildSettings");
                }
            }
#endif

            EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(AssetStoreClient.Update));
            EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(AssetStoreClient.Update));
            AssetStoreManager.Login("Login to fetch current list of published packages");
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
            Console.WriteLine("Asset Store Upload Tool logged in. V4.1.0");
            AssetStoreManager assetStoreManager2 = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
            assetStoreManager2.Init();
            assetStoreManager2.Show();
        }

        [MenuItem("Asset Store Tools/Publisher Adminstration", false, 1)]
        private static void LaunchPublisherAdminInExternalBrowser()
        {
            Application.OpenURL("https://publisher.assetstore.unity3d.com/?xunitysession=" + AssetStoreClient.XUnitySession);
        }

        private static void LaunchNewPackageInExternalBrowser()
        {
            Application.OpenURL("https://publisher.assetstore.unity3d.com/packages.html?xunitysession=" + AssetStoreClient.XUnitySession);
        }

        [MenuItem("Asset Store Tools/Guidelines", false, 2)]
        private static void LaunchGuidelinesInExternalBrowser()
        {
            Application.OpenURL("https://unity3d.com/asset-store/sell-assets/submission-guidelines");
        }

        [MenuItem("Asset Store Tools/Asset Store Logout", false, 3)]
        private static void AssetStoreLogout()
        {
            DebugUtils.Log("Logged out of Asset Store");
            AssetStoreManager assetStoreManager = (AssetStoreManager)EditorWindow.GetWindow(typeof(AssetStoreManager), false, "Package Upload");
            assetStoreManager.Close();
            AssetStoreClient.Logout();
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
                base.Repaint();
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
        }

        private void OnDisable()
        {
            EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(this.PackageControllerUpdatePump));
        }

        private void OnGUI()
        {
            if (base.minSize.x != 530f)
            {
                base.minSize = new Vector2(530f, 200f);
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
            GUILayout.Label("Version " + "V4.1.0".Substring(1), new GUILayoutOption[0]);
            GUI.color = color;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", new GUILayoutOption[]
            {
            GUILayout.Width(100f),
            GUILayout.Height(30f)
            }))
            {
                base.Close();
            }
            GUILayout.Space(10f);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void RenderDebug()
        {
            if (AssetStoreManager.sDbgButtons)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Debug: ", AssetStoreManager.Styles.ToolbarLabel, new GUILayoutOption[0]);
                if (GUILayout.Button("FileSelector", EditorStyles.toolbarButton, new GUILayoutOption[0]))
                {
                    FileSelector.Show("/", new List<string>(), delegate (List<string> newList)
                    {
                        foreach (string str in newList)
                        {
                            DebugUtils.Log(str);
                        }
                    });
                }
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, new GUILayoutOption[0]))
                {
                    AssetStoreAPI.GetMetaData(this.Account, this.m_PackageDataSource, delegate (string errMessage)
                    {
                        if (errMessage != null)
                        {
                            Debug.LogError("Error fetching metadata: " + errMessage);
                            LoginWindow.Logout();
                            AssetStoreManager.Login("Login to fetch current list of published packages");
                            base.Repaint();
                            return;
                        }
                        this.m_PackageController.AutoSetSelected(this);
                        base.Repaint();
                    });
                }
                if (GUILayout.Button("Logout", EditorStyles.toolbarButton, new GUILayoutOption[0]))
                {
                    AssetStoreClient.Logout();
                }
            }
        }

        private void RenderMenu()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            this.RenderDebug();
            GUILayout.EndHorizontal();
            bool flag = false;
            if (!LoginWindow.IsLoggedIn)
            {
                if (!LoginWindow.IsVisible)
                {
                    LoginWindow.Login("Please re-login to asset store in order to fetch publisher details", new LoginWindow.LoginCallback(AssetStoreManager.OnLoggedIn), GUIUtil.RectOnRect(360f, 140f, base.position));
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
                    AssetStoreAPI.GetMetaData(this.Account, this.m_PackageDataSource, delegate (string errMessage)
                    {
                        if (errMessage != null)
                        {
                            Debug.LogError("Error fetching metadata: " + errMessage);
                            LoginWindow.Logout();
                            AssetStoreManager.Login("Login to fetch current list of published packages");
                            base.Repaint();
                            return;
                        }
                        this.m_PackageController.AutoSetSelected(this);
                        base.Repaint();
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
                    base.Repaint();
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
            DebugUtils.Log("Ondestroy");
        }

        private const string windowTitle = "Package Upload";

        internal static bool sDbg;

        internal static bool sDbgButtons;

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

}

#pragma warning restore 0649