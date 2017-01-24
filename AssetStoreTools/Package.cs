using System;
using System.Collections.Generic;
using UnityEngine;

internal class Package
{
	internal enum PublishedStatus
	{
		Draft,
		Disabled,
		Published,
		PendingReview
	}

	private int m_Id = -1;

	public int versionId = -1;

	public string Name = string.Empty;

	public string VersionName = string.Empty;

	public string ProjectPath = string.Empty;

	public string RootPath = string.Empty;

	public string RootGUID = string.Empty;

	public List<string> MainAssets = new List<string>();

	public bool IsCompleteProjects;

	public string PreviewURL;

	private Image m_IconKeyimage = new Image();

	private Package.PublishedStatus m_Status;

	public int Id
	{
		get
		{
			return this.m_Id;
		}
	}

	public Texture2D Icon
	{
		get
		{
			return this.m_IconKeyimage.Texture;
		}
	}

	public Package.PublishedStatus Status
	{
		get
		{
			return this.m_Status;
		}
	}

	public Package(int id)
	{
		this.m_Id = id;
	}

	public Package(int id, string name, string iconUrl) : this(id)
	{
		this.Name = name;
		this.m_IconKeyimage.Name = "icon";
		if (iconUrl != null)
		{
			this.SetIconURL(iconUrl);
		}
	}

	public void SetIconURL(string url)
	{
		if (url.StartsWith("//"))
		{
			url = "http:" + url;
		}
		this.m_IconKeyimage = new Image();
		this.m_IconKeyimage.mUrl = url;
		this.m_IconKeyimage.Name = "icon";
		ImageCache.DownloadImage(this.m_IconKeyimage, null);
	}

	public void SetStatus(string status)
	{
		status = status.ToLowerInvariant();
		if (status == "draft")
		{
			this.m_Status = Package.PublishedStatus.Draft;
		}
		else if (status == "published")
		{
			this.m_Status = Package.PublishedStatus.Published;
		}
		else if (status == "pendingreview")
		{
			this.m_Status = Package.PublishedStatus.PendingReview;
		}
		else
		{
			this.m_Status = Package.PublishedStatus.Disabled;
		}
	}
}
