using System;
using System.Reflection;

namespace AssetStoreTools.Editor.InternalBridge
{
    public class UnityConnectSession
    {
        public static UnityConnectSession instance
        {
            get
            {
                return UnityConnectSession._instance;
            }
        }

        public string GetAccessToken()
        {
            if (string.IsNullOrEmpty(UnityConnectSession.s_AccessToken))
            {
                Type type = BackwardsCompatibilityUtility.FindTypeByName("UnityEditor.Connect.UnityConnect");
                MethodInfo method = type.GetMethod("GetAccessToken");
                UnityConnectSession.s_AccessToken = (string)method.Invoke(UnityConnectSession.GetUnityConnectInstance(), null);
            }
            return UnityConnectSession.s_AccessToken;
        }

        public bool LoggedIn()
        {
            Type type = BackwardsCompatibilityUtility.FindTypeByName("UnityEditor.Connect.UnityConnect");
            PropertyInfo property = type.GetProperty("loggedIn");
            UnityConnectSession.s_LoggedIn = (bool)property.GetValue(UnityConnectSession.GetUnityConnectInstance(), null);
            return UnityConnectSession.s_LoggedIn;
        }

        private static object GetUnityConnectInstance()
        {
            if (UnityConnectSession.s_UnityConnectInstance == null)
            {
                Type type = BackwardsCompatibilityUtility.FindTypeByName("UnityEditor.Connect.UnityConnect");
                UnityConnectSession.s_UnityConnectInstance = type.GetProperty("instance").GetValue(null, null);
            }
            return UnityConnectSession.s_UnityConnectInstance;
        }

        private static UnityConnectSession _instance = new UnityConnectSession();

        private static object s_UnityConnectInstance;

        private static string s_AccessToken;

        private static bool s_LoggedIn;
    }
}
