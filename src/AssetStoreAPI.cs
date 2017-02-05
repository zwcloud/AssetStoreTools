using System;
using System.Collections.Generic;

internal static class AssetStoreAPI
{
	public delegate void DoneCallback(string errorMessage);

	public delegate void UploadBundleCallback(string filepath, string errorMessage);

	private const string kBundlePath = "/package/{0}/assetbundle/{1}";

	private const string kUnityPackagePath = "/package/{0}/unitypackage";

	private static bool Parse(AssetStoreResponse response, out string error, out JSONValue jval)
	{
		jval = default(JSONValue);
		error = null;
		if (response.failed)
		{
			error = string.Format("Error receiving response from server ({0}): {1}", response.HttpStatusCode, response.HttpErrorMessage ?? "n/a");
			return true;
		}
		try
		{
			JSONParser jSONParser = new JSONParser(response.data);
			jval = jSONParser.Parse();
		}
		catch (JSONParseException ex)
		{
			error = "Error parsing reply from AssetStore";
			DebugUtils.LogError("Error parsing server reply: " + response.data);
			DebugUtils.LogError(ex.Message);
			return true;
		}
		if (jval.ContainsKey("error"))
		{
			error = jval["error"].AsString(true);
		}
		else if (jval.ContainsKey("status") && jval["status"].AsString(true) != "ok")
		{
			error = jval["message"].AsString(true);
		}
		return error != null;
	}

	public static void GetMetaData(AssetStorePublisher account, PackageDataSource packageDataSource, AssetStoreAPI.DoneCallback callback)
	{
		AssetStoreClient.CreatePendingGet("metadata", "/metadata/0", delegate(AssetStoreResponse res)
		{
			DebugUtils.Log(res.data);
			string errorMessage;
			JSONValue jval;
			bool flag = AssetStoreAPI.Parse(res, out errorMessage, out jval);
			if (flag && !jval.ContainsKey("error_fields"))
			{
				callback(errorMessage);
				return;
			}
			string str = "none";
			try
			{
				str = "account";
				string text = AssetStoreAPI.OnAssetStorePublisher(jval, account, packageDataSource);
				if (text != null)
				{
					callback(text);
					return;
				}
			}
			catch (JSONTypeException ex)
			{
				callback("Malformed metadata response from server: " + str + " - " + ex.Message);
			}
			catch (KeyNotFoundException ex2)
			{
				callback("Malformed metadata response from server. " + str + " - " + ex2.Message);
			}
			callback(null);
		});
	}

	private static string OnAssetStorePublisher(JSONValue jval, AssetStorePublisher account, PackageDataSource packageDataSource)
	{
		string str = "unknown field";
		try
		{
			str = "publisher";
			Dictionary<string, JSONValue> dictionary = jval["publisher"].AsDict(false);
			account.mStatus = AssetStorePublisher.Status.New;
			if (dictionary.ContainsKey("name"))
			{
				account.mStatus = AssetStorePublisher.Status.Existing;
				str = "publisher -> id";
				account.publisherId = int.Parse(dictionary["id"].AsString(false));
				str = "publisher -> name";
				account.publisherName = dictionary["name"].AsString(false);
			}
			str = "publisher";
			if (AssetStoreManager.sDbg)
			{
				DebugUtils.Log("publisher " + jval["publisher"].ToString(string.Empty, "    "));
				DebugUtils.Log("packs " + jval["packages"].ToString(string.Empty, "    "));
			}
			str = "packages";
			if (!jval.Get("packages").IsNull())
			{
				AssetStoreAPI.OnPackages(jval["packages"], packageDataSource);
			}
		}
		catch (JSONTypeException ex)
		{
			string result = "Malformed response from server: " + str + " - " + ex.Message;
			return result;
		}
		catch (KeyNotFoundException ex2)
		{
			string result = "Malformed response from server. " + str + " - " + ex2.Message;
			return result;
		}
		return null;
	}

	private static void OnPackages(JSONValue jv, PackageDataSource packageDataSource)
	{
		IList<Package> allPackages = packageDataSource.GetAllPackages();
		Dictionary<string, JSONValue> dictionary = jv.AsDict(false);
		string text = string.Empty;
		foreach (KeyValuePair<string, JSONValue> current in dictionary)
		{
			int num = int.Parse(current.Key);
			JSONValue value = current.Value;
			Package package = packageDataSource.FindByID(num);
			if (package == null)
			{
				package = new Package(num);
			}
			text += AssetStoreAPI.OnPackageReceived(value, package);
			text += AssetStoreAPI.RefreshMainAssets(value, package);
			allPackages.Add(package);
		}
		packageDataSource.OnDataReceived(text);
	}

	private static void Upload(string path, string filepath, AssetStoreAPI.DoneCallback callback, AssetStoreClient.ProgressCallback progress = null)
	{
		AssetStoreClient.Pending pending = AssetStoreClient.CreatePendingUpload(path, path, filepath, delegate(AssetStoreResponse resp)
		{
			string errorMessage = null;
			JSONValue jSONValue;
			AssetStoreAPI.Parse(resp, out errorMessage, out jSONValue);
			callback(errorMessage);
		});
		pending.progressCallback = progress;
	}

	public static string GetUploadBundlePath(Package package, string relativeAssetPath)
	{
		return string.Format("/package/{0}/assetbundle/{1}", package.versionId, Uri.EscapeDataString(relativeAssetPath));
	}

	public static void UploadBundle(string path, string filepath, AssetStoreAPI.UploadBundleCallback callback, AssetStoreClient.ProgressCallback progress)
	{
		AssetStoreClient.UploadLargeFile(path, filepath, null, delegate(AssetStoreResponse resp)
		{
			string errorMessage = null;
			JSONValue jSONValue;
			AssetStoreAPI.Parse(resp, out errorMessage, out jSONValue);
			callback(filepath, errorMessage);
		}, progress);
	}

	public static void UploadAssets(Package package, string newGUID, string newRootPath, string newProjectpath, string filepath, AssetStoreAPI.DoneCallback callback, AssetStoreClient.ProgressCallback progress)
	{
		string path = string.Format("/package/{0}/unitypackage", package.versionId);
		AssetStoreClient.UploadLargeFile(path, filepath, new Dictionary<string, string>
		{
			{
				"root_guid",
				newGUID
			},
			{
				"root_path",
				newRootPath
			},
			{
				"project_path",
				newProjectpath
			}
		}, delegate(AssetStoreResponse resp)
		{
			if (resp.HttpStatusCode == -2)
			{
				callback("aborted");
				return;
			}
			string errorMessage = null;
			JSONValue jSONValue;
			AssetStoreAPI.Parse(resp, out errorMessage, out jSONValue);
			callback(errorMessage);
		}, progress);
	}

	private static string OnPackageReceived(JSONValue jval, Package package)
	{
		string text = "unknown";
		try
		{
			if (!jval.ContainsKey("id"))
			{
				string result = null;
				return result;
			}
			string empty = string.Empty;
			string empty2 = string.Empty;
			string empty3 = string.Empty;
			string empty4 = string.Empty;
			string empty5 = string.Empty;
			bool isCompleteProjects = false;
			string empty6 = string.Empty;
			string empty7 = string.Empty;
			string empty8 = string.Empty;
			text = "id";
			if (!jval[text].IsNull())
			{
				package.versionId = int.Parse(jval[text].AsString(false));
			}
			text = "name";
			jval.Copy(text, ref empty, false);
			text = "version_name";
			jval.Copy(text, ref empty2, false);
			text = "root_guid";
			jval.Copy(text, ref empty3, false);
			text = "root_path";
			jval.Copy(text, ref empty4, false);
			text = "project_path";
			jval.Copy(text, ref empty5, false);
			text = "is_complete_project";
			jval.Copy(text, ref isCompleteProjects);
			text = "preview_url";
			jval.Copy(text, ref empty6);
			text = "icon_url";
			jval.Copy(text, ref empty7);
			text = "status";
			jval.Copy(text, ref empty8);
			package.Name = empty;
			package.VersionName = empty2;
			package.RootGUID = empty3;
			package.RootPath = empty4;
			package.ProjectPath = empty5;
			package.IsCompleteProjects = isCompleteProjects;
			package.PreviewURL = empty6;
			package.SetStatus(empty8);
			if (!string.IsNullOrEmpty(empty7))
			{
				package.SetIconURL(empty7);
			}
		}
		catch (JSONTypeException ex)
		{
			string result = string.Concat(new string[]
			{
				"Malformed metadata response for package '",
				package.Name,
				"' field '",
				text,
				"': ",
				ex.Message
			});
			return result;
		}
		catch (KeyNotFoundException ex2)
		{
			string result = string.Concat(new string[]
			{
				"Malformed metadata response for package. '",
				package.Name,
				"' field '",
				text,
				"': ",
				ex2.Message
			});
			return result;
		}
		return null;
	}

	private static string RefreshMainAssets(JSONValue jval, Package package)
	{
		string text = "unknown";
		try
		{
			text = "assetbundles";
			JSONValue jSONValue = jval.Get(text);
			if (!jSONValue.IsNull())
			{
				List<string> list = new List<string>();
				List<JSONValue> list2 = jSONValue.AsList(false);
				foreach (JSONValue current in list2)
				{
					list.Add(current.AsString(false));
				}
				package.MainAssets = list;
			}
		}
		catch (JSONTypeException ex)
		{
			string result = string.Concat(new string[]
			{
				"Malformed metadata response for mainAssets '",
				package.Name,
				"' field '",
				text,
				"': ",
				ex.Message
			});
			return result;
		}
		catch (KeyNotFoundException ex2)
		{
			string result = string.Concat(new string[]
			{
				"Malformed metadata response for package. '",
				package.Name,
				"' field '",
				text,
				"': ",
				ex2.Message
			});
			return result;
		}
		return null;
	}
}
