using System;
using System.Collections.Generic;

internal class PackageDataSource : IDataSource<Package>
{
	public delegate void DoneCallback();

	private List<Package> m_PackageList = new List<Package>();

	private List<Package> m_PackageListFiltered = new List<Package>();

	private string m_Filter = string.Empty;

	private PackageDataSource.DoneCallback m_DataReadyCallback;

	public PackageDataSource()
	{
	}

	public PackageDataSource(PackageDataSource.DoneCallback callback) : this()
	{
		this.m_DataReadyCallback = callback;
	}

	private static int AlphabeticalPackageComrparer(Package a, Package b)
	{
		if (a.Name == null && b.Name == null)
		{
			return 0;
		}
		if (a.Name == null)
		{
			return -1;
		}
		if (b.Name == null)
		{
			return 1;
		}
		return a.Name.CompareTo(b.Name);
	}

	public void OnDataReceived(string errMessage)
	{
		DebugUtils.Log("GetPackageList done;");
		if (!string.IsNullOrEmpty(errMessage))
		{
			DebugUtils.LogError("Error fetching packageList " + errMessage);
			return;
		}
		this.m_PackageList.Sort(new Comparison<Package>(PackageDataSource.AlphabeticalPackageComrparer));
		this.RefreshFilteredList();
		if (this.m_DataReadyCallback != null)
		{
			this.m_DataReadyCallback();
		}
	}

	public IList<Package> GetVisibleRows()
	{
		if (this.m_Filter.Length > 0)
		{
			return this.m_PackageListFiltered;
		}
		return this.m_PackageList;
	}

	public IList<Package> GetAllPackages()
	{
		return this.m_PackageList;
	}

	public void SetFilter(string filter)
	{
		if (filter != this.m_Filter)
		{
			this.m_Filter = filter;
			this.RefreshFilteredList();
		}
	}

	private void RefreshFilteredList()
	{
		this.m_PackageListFiltered.Clear();
		if (this.m_Filter.Length > 0)
		{
			foreach (Package current in this.m_PackageList)
			{
				bool flag = current.Name.IndexOf(this.m_Filter, StringComparison.OrdinalIgnoreCase) >= 0;
				if (current.Name != null && flag)
				{
					this.m_PackageListFiltered.Add(current);
				}
			}
		}
	}

	public Package FindByID(int packId)
	{
		return this.m_PackageList.Find((Package pack) => pack.Id == packId);
	}
}
