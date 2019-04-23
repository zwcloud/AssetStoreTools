using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AssetStoreTools
{
    internal static class AssetStoreClient
    {
        static AssetStoreClient()
        {
            ServicePointManager.ServerCertificateValidationCallback = ((object A_0, X509Certificate A_1, X509Chain A_2, SslPolicyErrors A_3) => true);
        }

        public static string LoginErrorMessage
        {
            get
            {
                return AssetStoreClient.sLoginErrorMessage;
            }
        }

        public static NameValueCollection Dict2Params(Dictionary<string, string> d)
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            foreach (KeyValuePair<string, string> keyValuePair in d)
            {
                nameValueCollection.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return nameValueCollection;
        }

        private static void InitClientPool()
        {
            if (AssetStoreClient.sClientPool != null)
            {
                return;
            }
            AssetStoreClient.sClientPool = new Stack<AssetStoreWebClient>(5);
            for (int i = 0; i < 5; i++)
            {
                AssetStoreWebClient assetStoreWebClient = new AssetStoreWebClient();
                assetStoreWebClient.Encoding = Encoding.UTF8;
                AssetStoreClient.sClientPool.Push(assetStoreWebClient);
            }
        }

        private static AssetStoreWebClient AcquireClient()
        {
            AssetStoreClient.InitClientPool();
            if (AssetStoreClient.sClientPool.Count != 0)
            {
                return AssetStoreClient.sClientPool.Pop();
            }
            return null;
        }

        private static void ReleaseClient(AssetStoreWebClient client)
        {
            AssetStoreClient.InitClientPool();
            if (client != null)
            {
                client.Headers.Remove("X-HttpMethod");
                AssetStoreClient.sClientPool.Push(client);
            }
        }

        private static string AssetStoreUrl
        {
            get
            {
                DebugUtils.Log(EditorPrefs.GetString("kharma.server", string.Empty));
                if (!string.IsNullOrEmpty(EditorPrefs.GetString("kharma.server", string.Empty)))
                {
                    Match match = Regex.Match(EditorPrefs.GetString("kharma.server", string.Empty), "(.*?//[^/]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
                string input = File.ReadAllText(EditorApplication.applicationContentsPath + "/Resources/loader.html");
                Match match2 = Regex.Match(input, "location.href.*?=.*?'(.*?//[^/']+)");
                if (match2.Success)
                {
                    return match2.Groups[1].Value;
                }
                return "https://kharma.unity3d.com";
            }
        }

        private static string GetProperPath(string partialPath)
        {
            return string.Format("{0}/api/asset-store-tools{1}.json", AssetStoreClient.AssetStoreUrl, partialPath);
        }

        private static Uri APIUri(string path)
        {
            return AssetStoreClient.APIUri(path, null);
        }

        private static Uri APIUri(string path, IDictionary<string, string> extraQuery)
        {
            Dictionary<string, string> dictionary;
            if (extraQuery != null)
            {
                dictionary = new Dictionary<string, string>(extraQuery);
            }
            else
            {
                dictionary = new Dictionary<string, string>();
            }
            dictionary.Add("unityversion", Application.unityVersion);
            dictionary.Add("toolversion", "V4.1.0");
            dictionary.Add("xunitysession", AssetStoreClient.ActiveOrUnauthSessionID);
            UriBuilder uriBuilder = new UriBuilder(AssetStoreClient.GetProperPath(path));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                string key = keyValuePair.Key;
                string arg = Uri.EscapeDataString(keyValuePair.Value);
                stringBuilder.AppendFormat("&{0}={1}", key, arg);
            }
            if (!string.IsNullOrEmpty(uriBuilder.Query))
            {
                uriBuilder.Query = uriBuilder.Query.Substring(1) + stringBuilder;
            }
            else
            {
                uriBuilder.Query = stringBuilder.Remove(0, 1).ToString();
            }
            DebugUtils.Log("preparing: " + uriBuilder.Uri);
            return uriBuilder.Uri;
        }

        private static string SavedSessionID
        {
            get
            {
                if (AssetStoreClient.RememberSession)
                {
                    return EditorPrefs.GetString("kharma.sessionid", string.Empty);
                }
                return string.Empty;
            }
            set
            {
                EditorPrefs.SetString("kharma.sessionid", value);
            }
        }

        public static bool HasSavedSessionID
        {
            get
            {
                return !string.IsNullOrEmpty(AssetStoreClient.SavedSessionID);
            }
        }

        private static string ActiveSessionID
        {
            get
            {
                Assembly assembly = Assembly.Load("UnityEditor");
                MethodInfo method = assembly.GetType("UnityEditor.AssetStoreContext").GetMethod("SessionGetString");
                string text = AssetStoreClient.SavedSessionID;
                if (method != null)
                {
                    object obj = method.Invoke(null, new object[]
                    {
                    "kharma.active_sessionid"
                    });
                    text = (string)obj;
                }
                else if (string.IsNullOrEmpty(text))
                {
                    text = AssetStoreClient.sActiveSessionIdBackwardsCompatibility;
                }
                if (text != null)
                {
                    return text;
                }
                return string.Empty;
            }
            set
            {
                Assembly assembly = Assembly.Load("UnityEditor");
                MethodInfo method = assembly.GetType("UnityEditor.AssetStoreContext").GetMethod("SessionSetString");
                if (method != null)
                {
                    method.Invoke(null, new object[]
                    {
                    "kharma.active_sessionid",
                    value
                    });
                }
                else
                {
                    if (AssetStoreManager.sDbg && string.IsNullOrEmpty(AssetStoreClient.sActiveSessionIdBackwardsCompatibility))
                    {
                        DebugUtils.Log("Backwards compatibility mode asset store set session");
                    }
                    AssetStoreClient.sActiveSessionIdBackwardsCompatibility = value;
                }
            }
        }

        public static bool HasActiveSessionID
        {
            get
            {
                return !string.IsNullOrEmpty(AssetStoreClient.ActiveSessionID);
            }
        }

        private static string ActiveOrUnauthSessionID
        {
            get
            {
                string activeSessionID = AssetStoreClient.ActiveSessionID;
                if (activeSessionID == string.Empty)
                {
                    return "26c4202eb475d02864b40827dfff11a14657aa41";
                }
                return activeSessionID;
            }
        }

        public static bool RememberSession
        {
            get
            {
                return EditorPrefs.GetString("kharma.remember_session") == "1";
            }
            set
            {
                EditorPrefs.SetString("kharma.remember_session", (!value) ? "0" : "1");
            }
        }

        private static string GetLicenseHash()
        {
            return InternalEditorUtility.GetAuthToken().Substring(0, 40);
        }

        private static string GetHardwareHash()
        {
            return InternalEditorUtility.GetAuthToken().Substring(40, 40);
        }

        public static string XUnitySession
        {
            get
            {
                return AssetStoreClient.ActiveOrUnauthSessionID;
            }
        }

        public static bool LoggedIn()
        {
            return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.LOGGED_IN;
        }

        public static bool LoginError()
        {
            return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.LOGIN_ERROR;
        }

        public static bool LoggedOut()
        {
            return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.LOGGED_OUT;
        }

        public static bool LoginInProgress()
        {
            return AssetStoreClient.sLoginState == AssetStoreClient.LoginState.IN_PROGRESS;
        }

        private static string UserIconUrl { get; set; }

        internal static void LoginWithCredentials(string username, string password, bool rememberMe, AssetStoreClient.DoneLoginCallback callback)
        {
            if (AssetStoreClient.sLoginState == AssetStoreClient.LoginState.IN_PROGRESS)
            {
                DebugUtils.LogWarning("Tried to login with credentials while already in progress of logging in");
                return;
            }
            AssetStoreClient.sLoginState = AssetStoreClient.LoginState.IN_PROGRESS;
            AssetStoreClient.RememberSession = rememberMe;
            AssetStoreClient.sLoginErrorMessage = null;
            Uri address = new Uri(string.Format("{0}/login", AssetStoreClient.AssetStoreUrl));
            AssetStoreWebClient assetStoreWebClient = new AssetStoreWebClient();
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection.Add("user", username);
            nameValueCollection.Add("pass", password);
            nameValueCollection.Add("unityversion", Application.unityVersion);
            nameValueCollection.Add("toolversion", "V4.1.0");
            nameValueCollection.Add("license_hash", AssetStoreClient.GetLicenseHash());
            nameValueCollection.Add("hardware_hash", AssetStoreClient.GetHardwareHash());
            AssetStoreClient.Pending pending = new AssetStoreClient.Pending();
            pending.conn = assetStoreWebClient;
            pending.id = "login";
            pending.callback = AssetStoreClient.WrapLoginCallback(callback);
            AssetStoreClient.pending.Add(pending);
            assetStoreWebClient.Headers.Add("Accept", "application/json");
            assetStoreWebClient.UploadValuesCompleted += AssetStoreClient.UploadValuesCallback;
            try
            {
                assetStoreWebClient.UploadValuesAsync(address, "POST", nameValueCollection, pending);
            }
            catch (WebException ex)
            {
                pending.ex = ex;
                AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
            }
        }

        internal static void LoginWithRememberedSession(AssetStoreClient.DoneLoginCallback callback)
        {
            if (AssetStoreClient.sLoginState == AssetStoreClient.LoginState.IN_PROGRESS)
            {
                DebugUtils.LogWarning("Tried to login with remembered session while already in progress of logging in");
                return;
            }
            AssetStoreClient.sLoginState = AssetStoreClient.LoginState.IN_PROGRESS;
            AssetStoreClient.sLoginErrorMessage = null;
            if (!AssetStoreClient.RememberSession)
            {
                AssetStoreClient.SavedSessionID = string.Empty;
            }
            Uri address = new Uri(string.Format("{0}/login?reuse_session={1}&unityversion={2}&toolversion={3}&xunitysession={4}", new object[]
            {
            AssetStoreClient.AssetStoreUrl,
            AssetStoreClient.SavedSessionID,
            Uri.EscapeDataString(Application.unityVersion),
            Uri.EscapeDataString("V4.1.0"),
            "26c4202eb475d02864b40827dfff11a14657aa41"
            }));
            AssetStoreWebClient assetStoreWebClient = new AssetStoreWebClient();
            AssetStoreClient.Pending pending = new AssetStoreClient.Pending();
            pending.conn = assetStoreWebClient;
            pending.id = "login";
            pending.callback = AssetStoreClient.WrapLoginCallback(callback);
            AssetStoreClient.pending.Add(pending);
            assetStoreWebClient.Headers.Add("Accept", "application/json");
            assetStoreWebClient.DownloadStringCompleted += AssetStoreClient.DownloadStringCallback;
            try
            {
                assetStoreWebClient.DownloadStringAsync(address, pending);
            }
            catch (WebException ex)
            {
                pending.ex = ex;
                AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
            }
        }

        private static AssetStoreClient.DoneCallback WrapLoginCallback(AssetStoreClient.DoneLoginCallback callback)
        {
            return delegate (AssetStoreResponse resp)
            {
                AssetStoreClient.UserIconUrl = null;
                int num = -1;
                if (resp.HttpHeaders != null)
                {
                    num = Array.IndexOf<string>(resp.HttpHeaders.AllKeys, "X-Unity-Reason");
                }
                if (!resp.ok)
                {
                    AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
                    AssetStoreClient.sLoginErrorMessage = ((num < 0) ? "Failed communication" : resp.HttpHeaders.Get(num));
                    DebugUtils.LogError(resp.HttpErrorMessage ?? "Unknown http error");
                }
                else if (resp.data.StartsWith("<!DOCTYPE"))
                {
                    AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGIN_ERROR;
                    AssetStoreClient.sLoginErrorMessage = ((num < 0) ? "Failed to login" : resp.HttpHeaders.Get(num));
                    DebugUtils.LogError(resp.data ?? "no data");
                }
                else
                {
                    AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGGED_IN;
                    JSONValue jsonvalue = JSONParser.SimpleParse(resp.data);
                    AssetStoreClient.ActiveSessionID = jsonvalue["xunitysession"].AsString(false);
                    AssetStoreClient.UserIconUrl = jsonvalue.Get("keyimage.icon").AsString(false);
                    if (AssetStoreClient.RememberSession)
                    {
                        AssetStoreClient.SavedSessionID = AssetStoreClient.ActiveSessionID;
                    }
                }
                callback(AssetStoreClient.sLoginErrorMessage);
            };
        }

        public static void Logout()
        {
            AssetStoreClient.UserIconUrl = null;
            AssetStoreClient.ActiveSessionID = string.Empty;
            AssetStoreClient.SavedSessionID = string.Empty;
            AssetStoreClient.sLoginState = AssetStoreClient.LoginState.LOGGED_OUT;
        }

        public static void Abort(string name)
        {
            AssetStoreClient.pending.RemoveAll(delegate (AssetStoreClient.Pending p)
            {
                if (p.id != name)
                {
                    return false;
                }
                if (p.conn.IsBusy)
                {
                    p.conn.CancelAsync();
                }
                return true;
            });
        }

        public static void Abort(AssetStoreClient.Pending removePending)
        {
            AssetStoreClient.pending.RemoveAll(delegate (AssetStoreClient.Pending p)
            {
                if (p != removePending)
                {
                    return false;
                }
                if (p.conn.IsBusy)
                {
                    p.conn.CancelAsync();
                }
                return true;
            });
        }

        private static void DownloadStringCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            if (e.Error != null)
            {
                pending.ex = e.Error;
                return;
            }
            if (!e.Cancelled)
            {
                pending.bytesReceived = pending.totalBytesToReceive;
                pending.statsUpdated = false;
                pending.data = e.Result;
            }
            else
            {
                pending.data = string.Empty;
            }
        }

        private static void DownloadDataCallback(object sender, DownloadDataCompletedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            if (e.Error != null)
            {
                pending.ex = e.Error;
                return;
            }
            if (!e.Cancelled)
            {
                pending.bytesReceived = pending.totalBytesToReceive;
                pending.statsUpdated = false;
                pending.binData = e.Result;
            }
            else
            {
                pending.binData = new byte[0];
            }
        }

        private static void UploadStringCallback(object sender, UploadStringCompletedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            if (e.Error != null)
            {
                pending.ex = e.Error;
                return;
            }
            pending.data = string.Empty;
            if (!e.Cancelled)
            {
                pending.bytesReceived = pending.totalBytesToReceive;
                pending.statsUpdated = false;
                pending.data = e.Result;
            }
            else
            {
                pending.data = string.Empty;
            }
        }

        private static void UploadValuesCallback(object sender, UploadValuesCompletedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            if (e.Error != null)
            {
                pending.ex = e.Error;
                return;
            }
            if (!e.Cancelled)
            {
                pending.bytesReceived = pending.totalBytesToReceive;
                pending.statsUpdated = false;
                pending.data = Encoding.UTF8.GetString(e.Result);
            }
            else
            {
                pending.data = string.Empty;
            }
        }

        private static void UploadFileCallback(object sender, UploadFileCompletedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            if (e.Error != null)
            {
                pending.ex = e.Error;
                return;
            }
            if (!e.Cancelled)
            {
                pending.bytesReceived = pending.totalBytesToReceive;
                pending.statsUpdated = false;
                pending.data = Encoding.UTF8.GetString(e.Result);
            }
            else
            {
                pending.data = string.Empty;
            }
        }

        private static void UploadProgressCallback(object sender, UploadProgressChangedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            pending.bytesSend = (uint)e.BytesSent;
            pending.totalBytesToSend = (uint)e.TotalBytesToSend;
            pending.bytesReceived = (uint)e.BytesReceived;
            pending.totalBytesToReceive = (uint)e.TotalBytesToReceive;
            pending.statsUpdated = true;
            Console.WriteLine("{0} uploaded {1} of {2} bytes. {3} % complete...", new object[]
            {
            pending.id,
            e.BytesSent,
            e.TotalBytesToSend,
            e.ProgressPercentage
            });
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            AssetStoreClient.Pending pending = (AssetStoreClient.Pending)e.UserState;
            pending.bytesReceived = (uint)e.BytesReceived;
            pending.totalBytesToReceive = (uint)e.TotalBytesToReceive;
            pending.bytesSend = 0u;
            pending.totalBytesToSend = 0u;
            pending.statsUpdated = true;
            Console.WriteLine("{0} downloaded {1} of {2} bytes. {3} % complete...", new object[]
            {
            pending.id,
            e.BytesReceived,
            e.TotalBytesToReceive,
            e.ProgressPercentage
            });
        }

        private static AssetStoreClient.Pending CreatePending(string name, AssetStoreClient.DoneCallback callback)
        {
            foreach (AssetStoreClient.Pending pending in AssetStoreClient.pending)
            {
                if (pending.id == name)
                {
                    DebugUtils.Log("CreatePending name conflict!");
                }
            }
            AssetStoreClient.Pending pending2 = new AssetStoreClient.Pending();
            pending2.id = name;
            pending2.callback = callback;
            AssetStoreClient.pending.Add(pending2);
            return pending2;
        }

        public static AssetStoreClient.Pending CreatePendingGet(string name, string path, AssetStoreClient.DoneCallback callback)
        {
            AssetStoreClient.Pending p = AssetStoreClient.CreatePending(name, callback);
            AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = delegate ()
            {
                p.conn = AssetStoreClient.AcquireClient();
                if (p.conn == null)
                {
                    return false;
                }
                try
                {
                    p.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                    p.conn.DownloadProgressChanged += AssetStoreClient.DownloadProgressCallback;
                    p.conn.DownloadStringCompleted += AssetStoreClient.DownloadStringCallback;
                    p.conn.DownloadStringAsync(AssetStoreClient.APIUri(path), p);
                }
                catch (WebException ex)
                {
                    p.ex = ex;
                    return false;
                }
                return true;
            };
            if (!pendingQueueDelegate())
            {
                p.queueDelegate = pendingQueueDelegate;
            }
            return p;
        }

        public static AssetStoreClient.Pending CreatePendingGetBinary(string name, string url, AssetStoreClient.DoneCallback callback)
        {
            AssetStoreClient.Pending p = AssetStoreClient.CreatePending(name, callback);
            AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = delegate ()
            {
                p.conn = AssetStoreClient.AcquireClient();
                if (p.conn == null)
                {
                    return false;
                }
                try
                {
                    p.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                    p.conn.DownloadProgressChanged += AssetStoreClient.DownloadProgressCallback;
                    p.conn.DownloadDataCompleted += AssetStoreClient.DownloadDataCallback;
                    p.conn.DownloadDataAsync(new Uri(url), p);
                }
                catch (WebException ex)
                {
                    p.ex = ex;
                    return false;
                }
                return true;
            };
            if (!pendingQueueDelegate())
            {
                p.queueDelegate = pendingQueueDelegate;
            }
            return p;
        }

        public static AssetStoreClient.Pending CreatePendingPost(string name, string path, NameValueCollection param, AssetStoreClient.DoneCallback callback)
        {
            AssetStoreClient.Pending p = AssetStoreClient.CreatePending(name, callback);
            AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = delegate ()
            {
                p.conn = AssetStoreClient.AcquireClient();
                if (p.conn == null)
                {
                    return false;
                }
                try
                {
                    p.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                    p.conn.UploadProgressChanged += AssetStoreClient.UploadProgressCallback;
                    p.conn.UploadValuesCompleted += AssetStoreClient.UploadValuesCallback;
                    p.conn.UploadValuesAsync(AssetStoreClient.APIUri(path), "POST", param, p);
                }
                catch (WebException ex)
                {
                    p.ex = ex;
                    return false;
                }
                return true;
            };
            if (!pendingQueueDelegate())
            {
                p.queueDelegate = pendingQueueDelegate;
            }
            return p;
        }

        public static AssetStoreClient.Pending CreatePendingPost(string name, string path, string postData, AssetStoreClient.DoneCallback callback)
        {
            AssetStoreClient.Pending p = AssetStoreClient.CreatePending(name, callback);
            AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = delegate ()
            {
                p.conn = AssetStoreClient.AcquireClient();
                if (p.conn == null)
                {
                    return false;
                }
                try
                {
                    p.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                    p.conn.UploadProgressChanged += AssetStoreClient.UploadProgressCallback;
                    p.conn.UploadStringCompleted += AssetStoreClient.UploadStringCallback;
                    p.conn.UploadStringAsync(AssetStoreClient.APIUri(path), "POST", postData, p);
                }
                catch (WebException ex)
                {
                    p.ex = ex;
                    return false;
                }
                return true;
            };
            if (!pendingQueueDelegate())
            {
                p.queueDelegate = pendingQueueDelegate;
            }
            return p;
        }

        public static void UploadLargeFile(string path, string filepath, Dictionary<string, string> extraParams, AssetStoreClient.DoneCallback callback, AssetStoreClient.ProgressCallback progressCallback)
        {
            AssetStoreClient.LargeFilePending item = new AssetStoreClient.LargeFilePending(path, filepath, extraParams, callback, progressCallback);
            AssetStoreClient.s_PendingLargeFiles.Add(item);
        }

        private static string UpdateLargeFilesUpload()
        {
            if (AssetStoreClient.s_UploadingLargeFile == null)
            {
                if (AssetStoreClient.s_PendingLargeFiles.Count == 0)
                {
                    return null;
                }
                AssetStoreClient.s_UploadingLargeFile = AssetStoreClient.s_PendingLargeFiles[0];
                try
                {
                    AssetStoreClient.s_UploadingLargeFile.Open();
                }
                catch (Exception ex)
                {
                    DebugUtils.LogError("Unable to start uploading:" + AssetStoreClient.s_UploadingLargeFile.FilePath + " Reason: " + ex.Message);
                    AssetStoreClient.s_PendingLargeFiles.Remove(AssetStoreClient.s_UploadingLargeFile);
                    AssetStoreClient.s_PendingLargeFiles = null;
                    return null;
                }
            }
            AssetStoreClient.LargeFilePending largeFilePending = AssetStoreClient.s_UploadingLargeFile;
            StreamReader streamReader = null;
            WebResponse webResponse = null;
            try
            {
                if (largeFilePending == null || largeFilePending.Request == null)
                {
                    return null;
                }
                byte[] buffer = largeFilePending.Buffer;
                int num = 0;
                for (int i = 0; i < 2; i++)
                {
                    num = largeFilePending.RequestFileStream.Read(buffer, 0, buffer.Length);
                    if (num == 0)
                    {
                        break;
                    }
                    largeFilePending.RequestStream.Write(buffer, 0, num);
                    largeFilePending.BytesSent += (long)num;
                }
                if (num != 0)
                {
                    try
                    {
                        double num2 = (double)largeFilePending.BytesSent;
                        double num3 = (double)largeFilePending.BytesToSend;
                        double pctUp = num2 / num3 * 100.0;
                        if (largeFilePending.RequestProgressCallback != null)
                        {
                            largeFilePending.RequestProgressCallback(pctUp, 0.0);
                        }
                    }
                    catch (Exception ex2)
                    {
                        DebugUtils.LogWarning("Progress update error " + ex2.Message);
                    }
                    return null;
                }
                AssetStoreClient.s_PendingLargeFiles.Remove(largeFilePending);
                AssetStoreClient.s_UploadingLargeFile = null;
                DebugUtils.Log("Finished Uploading: " + largeFilePending.Id);
                webResponse = largeFilePending.Request.GetResponse();
                Stream responseStream = webResponse.GetResponseStream();
                string text;
                try
                {
                    streamReader = new StreamReader(responseStream);
                    text = streamReader.ReadToEnd();
                    streamReader.Close();
                }
                catch (Exception ex3)
                {
                    DebugUtils.LogError("StreamReader sr");
                    throw ex3;
                }
                AssetStoreResponse job = AssetStoreClient.parseAssetStoreResponse(text, null, null, webResponse.Headers);
                largeFilePending.Close();
                largeFilePending.RequestDoneCallback(job);
                return text;
            }
            catch (Exception ex4)
            {
                DebugUtils.LogError("UploadingLarge Files Exception:" + ex4.Source);
                if (streamReader != null)
                {
                    streamReader.Close();
                }
                AssetStoreResponse job2 = AssetStoreClient.parseAssetStoreResponse(null, null, ex4, (webResponse == null) ? null : webResponse.Headers);
                largeFilePending.RequestDoneCallback(job2);
                largeFilePending.Close();
                AssetStoreClient.s_PendingLargeFiles.Remove(largeFilePending);
            }
            return null;
        }

        public static void AbortLargeFilesUpload()
        {
            if (AssetStoreClient.s_PendingLargeFiles.Count == 0)
            {
                return;
            }
            AssetStoreClient.s_PendingLargeFiles.RemoveAll(delegate (AssetStoreClient.LargeFilePending assetUpload)
            {
                if (assetUpload == null)
                {
                    return true;
                }
                AssetStoreResponse job = AssetStoreClient.parseAssetStoreResponse(null, null, null, null);
                job.HttpStatusCode = -2;
                if (assetUpload.RequestDoneCallback != null)
                {
                    assetUpload.RequestDoneCallback(job);
                }
                assetUpload.Close();
                return true;
            });
        }

        public static AssetStoreClient.Pending CreatePendingUpload(string name, string path, string filepath, AssetStoreClient.DoneCallback callback)
        {
            DebugUtils.Log("CreatePendingUpload");
            AssetStoreClient.Pending p = AssetStoreClient.CreatePending(name, callback);
            AssetStoreClient.PendingQueueDelegate pendingQueueDelegate = delegate ()
            {
                p.conn = AssetStoreClient.AcquireClient();
                if (p.conn == null)
                {
                    return false;
                }
                try
                {
                    p.conn.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                    p.conn.UploadProgressChanged += AssetStoreClient.UploadProgressCallback;
                    p.conn.UploadFileCompleted += AssetStoreClient.UploadFileCallback;
                    p.conn.UploadFileAsync(AssetStoreClient.APIUri(path), "PUT", filepath, p);
                }
                catch (WebException ex)
                {
                    p.ex = ex;
                    return false;
                }
                return true;
            };
            if (!pendingQueueDelegate())
            {
                p.queueDelegate = pendingQueueDelegate;
            }
            return p;
        }

        public static void Update()
        {
            List<AssetStoreClient.Pending> obj = AssetStoreClient.pending;
            lock (obj)
            {
                AssetStoreClient.pending.RemoveAll(delegate (AssetStoreClient.Pending p)
                {
                    if (p.conn == null)
                    {
                        if (p.queueDelegate == null)
                        {
                            DebugUtils.LogWarning("Invalid pending state while communicating with asset store");
                            return true;
                        }
                        if (!p.queueDelegate() && p.conn == null)
                        {
                            return false;
                        }
                        p.queueDelegate = null;
                    }
                    if (!p.conn.IsBusy)
                    {
                        if (p.ex == null && p.data == null)
                        {
                            if (p.binData == null)
                            {
                                goto IL_19F;
                            }
                        }
                        try
                        {
                            AssetStoreResponse job = AssetStoreClient.parseAssetStoreResponse(p.data, p.binData, p.ex, (p.conn != null) ? p.conn.ResponseHeaders : null);
                            if (AssetStoreManager.sDbg)
                            {
                                DebugUtils.Log(string.Concat(new string[]
                                {
                                "Pending done: ",
                                Thread.CurrentThread.ManagedThreadId.ToString(),
                                " ",
                                p.id,
                                " ",
                                job.data ?? "<nodata>"
                                }));
                                if (job.HttpHeaders != null && job.HttpHeaders.Get("X-Unity-Reason") != null)
                                {
                                    DebugUtils.LogWarning("X-Unity-Reason: " + job.HttpHeaders.Get("X-Unity-Reason"));
                                }
                            }
                            p.callback(job);
                        }
                        catch (Exception ex)
                        {
                            DebugUtils.LogError("Uncaught exception in async net callback: " + ex.Message);
                            DebugUtils.LogError(ex.StackTrace);
                        }
                        AssetStoreClient.ReleaseClient(p.conn);
                        p.conn = null;
                        return true;
                    }
                    IL_19F:
                    if (p.progressCallback != null && p.statsUpdated)
                    {
                        p.statsUpdated = false;
                        double pctUp = (p.totalBytesToSend <= 0) 
                            ? 0 
                            : p.bytesSend / p.totalBytesToSend * 100.0;

                        double pctDown = (p.totalBytesToReceive <= 0)
                            ? 0
                            : p.bytesReceived / p.totalBytesToReceive * 100.0;

                        try
                        {
                            p.progressCallback(pctUp, pctDown);
                        }
                        catch (Exception ex2)
                        {
                            DebugUtils.LogError("Uncaught exception in async net progress callback: " + ex2.Message);
                        }
                    }
                    return false;
                });
            }
            AssetStoreClient.UpdateLargeFilesUpload();
        }

        private static AssetStoreResponse parseAssetStoreResponse(string data, byte[] binData, Exception ex, WebHeaderCollection responseHeaders)
        {
            AssetStoreResponse result = default(AssetStoreResponse);
            result.data = data;
            result.binData = binData;
            result.ok = true;
            result.HttpErrorMessage = null;
            result.HttpStatusCode = -1;
            if (ex != null)
            {
                WebException ex2 = null;
                try
                {
                    ex2 = (WebException)ex;
                }
                catch (Exception)
                {
                }
                if (ex2 == null || ex2.Response == null || ex2.Response.Headers == null)
                {
                    DebugUtils.LogError("Invalid server response " + ex.Message);
                    DebugUtils.LogError("Stacktrace:" + ex.StackTrace);
                }
                else
                {
                    result.HttpHeaders = ex2.Response.Headers;
                    result.HttpStatusCode = (int)((HttpWebResponse)ex2.Response).StatusCode;
                    result.HttpErrorMessage = ex2.Message;
                    if (result.HttpStatusCode != 401 && AssetStoreManager.sDbg)
                    {
                        WebHeaderCollection headers = ex2.Response.Headers;
                        DebugUtils.LogError("\nDisplaying ex the response headers\n");
                        for (int i = 0; i < headers.Count; i++)
                        {
                            DebugUtils.LogError("\t" + headers.GetKey(i) + " = " + headers.Get(i));
                        }
                        DebugUtils.Log("status code: " + result.HttpStatusCode.ToString());
                    }
                }
            }
            else
            {
                result.HttpStatusCode = 200;
                result.HttpHeaders = responseHeaders;
            }
            if (result.HttpStatusCode / 100 != 2)
            {
                result.ok = false;
                if (AssetStoreManager.sDbg)
                {
                    DebugUtils.LogError("Request statusCode: " + result.HttpStatusCode.ToString());
                }
                if (ex != null)
                {
                    result.HttpErrorMessage = ex.Message;
                }
                else
                {
                    result.HttpErrorMessage = "Request status: " + result.HttpStatusCode.ToString();
                }
            }
            if (ex != null)
            {
                result.ok = false;
                if (AssetStoreManager.sDbg)
                {
                    DebugUtils.LogError("Request exception: " + ex.GetType().ToString() + " - " + ex.Message);
                }
                result.HttpErrorMessage = ex.Message;
            }
            return result;
        }

        public static void LoadFromUrl(string url, AssetStoreClient.DoneCallback callback, AssetStoreClient.ProgressCallback progress)
        {
            AssetStoreClient.Pending pending = AssetStoreClient.CreatePendingGetBinary(url, url, callback);
            pending.progressCallback = progress;
        }

        public const string TOOL_VERSION = "V4.1.0";

        private const string ASSET_STORE_PROD_URL = "https://kharma.unity3d.com";

        private const string UNAUTH_SESSION_ID = "26c4202eb475d02864b40827dfff11a14657aa41";

        private const int kClientPoolSize = 5;

        private const int kSendBufferSize = 32768;

        private static string sActiveSessionIdBackwardsCompatibility;

        private static AssetStoreClient.LoginState sLoginState = AssetStoreClient.LoginState.LOGGED_OUT;

        private static string sLoginErrorMessage = null;

        private static Stack<AssetStoreWebClient> sClientPool;

        private static List<AssetStoreClient.LargeFilePending> s_PendingLargeFiles = new List<AssetStoreClient.LargeFilePending>();

        private static AssetStoreClient.LargeFilePending s_UploadingLargeFile = null;

        private static List<AssetStoreClient.Pending> pending = new List<AssetStoreClient.Pending>();

        private enum LoginState
        {
            LOGGED_OUT,
            IN_PROGRESS,
            LOGGED_IN,
            LOGIN_ERROR
        }

        private class LargeFilePending
        {
            public LargeFilePending(string url, string filepath, Dictionary<string, string> extraParams, AssetStoreClient.DoneCallback doneCallback, AssetStoreClient.ProgressCallback progressCallback)
            {
                this.Id = filepath;
                this.URI = url;
                this.FilePath = filepath;
                this.RequestDoneCallback = doneCallback;
                this.RequestProgressCallback = progressCallback;
                this.m_extraParams = extraParams;
            }

            public void Open()
            {
                try
                {
                    this.RequestFileStream = new FileStream(this.FilePath, FileMode.Open, FileAccess.Read);
                    this.Request = (HttpWebRequest)WebRequest.Create(AssetStoreClient.APIUri(this.URI, this.m_extraParams));
                    this.Request.AllowWriteStreamBuffering = false;
                    this.Request.Timeout = 36000000;
                    this.Request.Headers.Set("X-Unity-Session", AssetStoreClient.ActiveOrUnauthSessionID);
                    this.Request.KeepAlive = false;
                    this.Request.ContentLength = this.RequestFileStream.Length;
                    this.Request.Method = "PUT";
                    this.BytesToSend = this.RequestFileStream.Length;
                    this.BytesSent = 0L;
                    this.RequestStream = this.Request.GetRequestStream();
                    if (this.Buffer == null)
                    {
                        this.Buffer = new byte[32768];
                    }
                }
                catch (Exception ex)
                {
                    AssetStoreResponse job = AssetStoreClient.parseAssetStoreResponse(null, null, ex, null);
                    this.RequestDoneCallback(job);
                    this.Close();
                    throw ex;
                }
            }

            public void Close()
            {
                if (this.RequestFileStream != null)
                {
                    this.RequestFileStream.Close();
                    this.RequestFileStream = null;
                }
                if (this.RequestStream != null)
                {
                    this.RequestStream.Close();
                    this.RequestStream = null;
                }
                this.Request = null;
                this.Buffer = null;
            }

            public string Id;

            public string FilePath;

            public string URI;

            public FileStream RequestFileStream;

            public HttpWebRequest Request;

            public Stream RequestStream;

            public long BytesToSend;

            public long BytesSent;

            public AssetStoreClient.DoneCallback RequestDoneCallback;

            public AssetStoreClient.ProgressCallback RequestProgressCallback;

            public byte[] Buffer;

            private Dictionary<string, string> m_extraParams;
        }

        public class Pending
        {
            internal AssetStoreClient.PendingQueueDelegate queueDelegate;

            public AssetStoreWebClient conn;

            public Exception ex;

            public string data;

            public byte[] binData;

            public volatile uint bytesReceived;

            public volatile uint totalBytesToReceive;

            public volatile uint bytesSend;

            public volatile uint totalBytesToSend;

            public volatile bool statsUpdated;

            public string id;

            public AssetStoreClient.DoneCallback callback;

            public AssetStoreClient.ProgressCallback progressCallback;
        }

        public delegate void DoneLoginCallback(string errorMessage);

        internal delegate void DoneCallback(AssetStoreResponse job);

        public delegate void ProgressCallback(double pctUp, double pctDown);

        internal delegate bool PendingQueueDelegate();
    }

}