using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

internal class LoginWindow : EditorWindow
{
	public static void Login(string loginReason, LoginWindow.LoginCallback callback)
	{
		LoginWindow.Login(loginReason, callback, new Rect(100f, 100f, 360f, 140f));
	}

	public static void Login(string loginReason, LoginWindow.LoginCallback callback, Rect windowRect)
	{
		if (AssetStoreClient.HasActiveSessionID)
		{
			AssetStoreClient.Logout();
		}
		if (!AssetStoreClient.RememberSession || !AssetStoreClient.HasSavedSessionID)
		{
			LoginWindow.ShowLoginWindow(loginReason, callback, windowRect);
			return;
		}
		AssetStoreClient.LoginWithRememberedSession(delegate(string errorMessage)
		{
			if (string.IsNullOrEmpty(errorMessage))
			{
				callback(errorMessage);
			}
			else
			{
				LoginWindow.ShowLoginWindow(loginReason, callback, windowRect);
			}
		});
	}

	public static void Logout()
	{
		AssetStoreClient.Logout();
	}

	public static bool IsLoggedIn
	{
		get
		{
			return AssetStoreClient.HasActiveSessionID;
		}
	}

	public static void ShowLoginWindow(string loginReason, LoginWindow.LoginCallback callback)
	{
		LoginWindow.ShowLoginWindow(loginReason, callback, new Rect(100f, 100f, 360f, 140f));
	}

	private static void ShowLoginWindow(string loginReason, LoginWindow.LoginCallback callback, Rect windowRect)
	{
		LoginWindow.IsVisible = true;
		LoginWindow loginWindow = (LoginWindow)EditorWindow.GetWindowWithRect(typeof(LoginWindow), windowRect, true, "Login to Asset Store");
		loginWindow.position = windowRect;
		loginWindow.m_Password = string.Empty;
		loginWindow.m_LoginCallback = callback;
		loginWindow.m_LoginReason = loginReason;
		loginWindow.m_LoginRemoteMessage = null;
		loginWindow.Show();
	}

	public void OnEnabled()
	{
		LoginWindow.IsVisible = true;
	}

	public void OnDisable()
	{
		LoginWindow.IsVisible = false;
		if (this.m_LoginCallback != null)
		{
			this.m_LoginCallback(this.m_LoginRemoteMessage);
		}
		this.m_LoginCallback = null;
		this.m_Password = null;
		this.m_LoginRemoteMessage = "Cancelled";
	}

	public void OnGUI()
	{
		if (AssetStoreClient.LoginInProgress() || LoginWindow.IsLoggedIn)
		{
			GUI.enabled = false;
		}
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.Space(5f);
		GUILayout.Label(GUIUtil.Logo, GUIStyle.none, new GUILayoutOption[]
		{
			GUILayout.ExpandWidth(false)
		});
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.Space(6f);
		GUILayout.Label(this.m_LoginReason, EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
		Rect lastRect = GUILayoutUtility.GetLastRect();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.Space(6f);
		Rect lastRect2 = new Rect(0f, 0f, 0f, 0f);
		if (this.m_LoginRemoteMessage != null)
		{
			Color color = GUI.color;
			GUI.color = Color.red;
			GUILayout.Label(this.m_LoginRemoteMessage, EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
			GUI.color = color;
			lastRect2 = GUILayoutUtility.GetLastRect();
		}
		float num = lastRect.height + lastRect2.height + 110f;
		if (Event.current.type == EventType.Repaint && num != base.position.height)
		{
			base.position = new Rect(base.position.x, base.position.y, base.position.width, num);
			base.Repaint();
		}
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.Space(6f);
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		GUI.SetNextControlName("username");
		GUILayout.Label("Username", new GUILayoutOption[0]);
		GUILayout.Label("Password", new GUILayoutOption[0]);
		GUILayout.EndVertical();
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		this.m_Username = EditorGUILayout.TextField(this.m_Username, new GUILayoutOption[0]);
		this.m_Password = EditorGUILayout.PasswordField(this.m_Password, new GUILayoutOption[0]);
		GUILayout.EndVertical();
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		GUILayout.Label("     ", new GUILayoutOption[0]);
		Color color2 = GUI.color;
		GUI.color = Color.blue;
		if (GUILayout.Button("Forgot?", EditorStyles.miniLabel, new GUILayoutOption[0]))
		{
			Application.OpenURL("https://accounts.unity3d.com/password/new");
		}
		GUI.color = color2;
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		bool rememberSession = AssetStoreClient.RememberSession;
		bool flag = EditorGUILayout.Toggle("Remember me", rememberSession, new GUILayoutOption[0]);
		if (flag != rememberSession)
		{
			AssetStoreClient.RememberSession = flag;
		}
		GUILayout.EndVertical();
		GUILayout.Space(5f);
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		if (GUILayout.Button("Create account", new GUILayoutOption[0]))
		{
			AssetStore.Open("createuser/");
			this.m_LoginRemoteMessage = "Cancelled - create user";
			base.Close();
		}
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Cancel", new GUILayoutOption[0]))
		{
			this.m_LoginRemoteMessage = "Cancelled";
			base.Close();
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Login", new GUILayoutOption[0]))
		{
			this.Login();
			base.Repaint();
		}
		GUILayout.Space(5f);
		GUILayout.EndHorizontal();
		GUILayout.Space(5f);
		GUILayout.EndVertical();
		if (Event.current.Equals(Event.KeyboardEvent("return")))
		{
			this.Login();
			base.Repaint();
		}
		if (this.m_Username == string.Empty)
		{
			GUI.FocusControl("username");
		}
	}

	private void Login()
	{
		this.m_LoginRemoteMessage = null;
		if (AssetStoreClient.HasActiveSessionID)
		{
			AssetStoreClient.Logout();
		}
		AssetStoreClient.LoginWithCredentials(this.m_Username, this.m_Password, AssetStoreClient.RememberSession, delegate(string errorMessage)
		{
			this.m_LoginRemoteMessage = errorMessage;
			if (errorMessage == null)
			{
				base.Close();
			}
			else
			{
				base.Repaint();
			}
		});
	}

	public const float kDefaultWidth = 360f;

	public const float kDefaultHeight = 140f;

	private const float kBaseHeight = 110f;

	public static bool IsVisible;

	private string m_LoginReason;

	private string m_LoginRemoteMessage;

	private string m_Username = string.Empty;

	private string m_Password = string.Empty;

	private LoginWindow.LoginCallback m_LoginCallback;

	public delegate void LoginCallback(string errorMessage);
}
