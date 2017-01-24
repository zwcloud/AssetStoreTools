using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class ImageCache
{
	public delegate void DownloadCallback(Image img, ImageCache imgcache, string errorMessage);

	private const float k_FadeTime = 0.7f;

	private const int kImageCacheSize = 103;

	private double m_DownloadedAt;

	private static Dictionary<string, ImageCache> s_EntriesByUrl;

	public Texture2D Texture
	{
		get;
		set;
	}

	public double LastUsed
	{
		get;
		set;
	}

	public float Progress
	{
		get;
		set;
	}

	public float FadeAlpha
	{
		get
		{
			return (this.m_DownloadedAt >= 0.0) ? Mathf.Min(1f, (float)(EditorApplication.timeSinceStartup - this.m_DownloadedAt) / 0.7f) : 0f;
		}
	}

	public bool Failed
	{
		get;
		private set;
	}

	public static int MaxCacheSize
	{
		get
		{
			return 103;
		}
	}

	public static Dictionary<string, ImageCache> EntriesByUrl
	{
		get
		{
			if (ImageCache.s_EntriesByUrl == null)
			{
				ImageCache.s_EntriesByUrl = new Dictionary<string, ImageCache>();
			}
			return ImageCache.s_EntriesByUrl;
		}
	}

	public static ImageCache DownloadImage(Image img, ImageCache.DownloadCallback callback)
	{
		if (img == null || img.mUrl == null)
		{
			return null;
		}
		ImageCache ic;
		if (ImageCache.EntriesByUrl.TryGetValue(img.mUrl, out ic))
		{
			if (ic.Texture != null || ic.Progress >= 0f || ic.Failed)
			{
				img.mImgCache = ic;
				return ic;
			}
		}
		else
		{
			ImageCache.EnsureCacheSpace();
			ic = new ImageCache();
			ic.Failed = false;
			ic.Texture = null;
			ic.m_DownloadedAt = -1.0;
			ic.LastUsed = EditorApplication.timeSinceStartup;
			ImageCache.EntriesByUrl[img.mUrl] = ic;
		}
		img.mImgCache = ic;
		ic.Progress = 0f;
		AssetStoreClient.ProgressCallback progress = delegate(double pctUp, double pctDown)
		{
			ic.Progress = (float)pctDown;
		};
		AssetStoreClient.DoneCallback callback2 = delegate(AssetStoreResponse resp)
		{
			ic.Progress = -1f;
			ic.LastUsed = EditorApplication.timeSinceStartup;
			ic.m_DownloadedAt = ic.LastUsed;
			ic.Failed = resp.failed;
			if (resp.ok && resp.binData != null && resp.binData.Length > 0)
			{
				Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				texture2D.LoadImage(resp.binData);
				ic.Texture = texture2D;
			}
			if (callback != null)
			{
				string text = string.Format("Error fetching {0}", img.Name);
				if (resp.failed)
				{
					DebugUtils.LogWarning(text + string.Format(" : ({0}) {1} from {2}", resp.HttpStatusCode, resp.HttpErrorMessage ?? "n/a", img.mUrl));
				}
				callback(img, ic, (!resp.ok) ? text : null);
			}
		};
		AssetStoreClient.LoadFromUrl(img.mUrl, callback2, progress);
		return ic;
	}

	public static ImageCache PushImage(string url, Texture2D tex)
	{
		ImageCache.EnsureCacheSpace();
		ImageCache imageCache = new ImageCache();
		imageCache.Failed = false;
		imageCache.Texture = tex;
		imageCache.LastUsed = EditorApplication.timeSinceStartup;
		imageCache.Progress = -1f;
		ImageCache.EntriesByUrl[url] = imageCache;
		return imageCache;
	}

	public static ImageCache PushImage(Image img, Texture2D tex)
	{
		img.mImgCache = ImageCache.PushImage(img.mUrl, tex);
		return img.mImgCache;
	}

	private static void EnsureCacheSpace()
	{
		while (ImageCache.EntriesByUrl.Count > 103)
		{
			string key = null;
			double num = EditorApplication.timeSinceStartup;
			foreach (KeyValuePair<string, ImageCache> current in ImageCache.EntriesByUrl)
			{
				if (current.Value.LastUsed < num)
				{
					key = current.Key;
					num = current.Value.LastUsed;
				}
			}
			ImageCache.EntriesByUrl.Remove(key);
		}
	}
}
