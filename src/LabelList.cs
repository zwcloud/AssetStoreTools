using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal class LabelList : ScriptableObject
{
	public int Count
	{
		get
		{
			return this.m_Labels.Length;
		}
	}

	public string this[int index]
	{
		get
		{
			return this.m_Labels[index];
		}
	}

	public int IndexOf(string label)
	{
		return Array.IndexOf<string>(this.m_Labels, label);
	}

	public string[] ToArray()
	{
		return this.m_Labels;
	}

	public string Add(string label)
	{
		if (Array.IndexOf<string>(this.m_Labels, label) == -1)
		{
			string[] array = new string[this.m_Labels.Length + 1];
			this.m_Labels.CopyTo(array, 0);
			array[this.m_Labels.Length] = label;
			this.m_Labels = array;
			EditorUtility.SetDirty(this);
		}
		return label;
	}

	public bool Remove(string label)
	{
		int num = Array.IndexOf<string>(this.m_Labels, label);
		if (num == -1)
		{
			return false;
		}
		string[] array = new string[this.m_Labels.Length - 1];
		Array.Copy(this.m_Labels, 0, array, 0, num);
		Array.Copy(this.m_Labels, num + 1, array, num, this.m_Labels.Length - num - 1);
		this.m_Labels = array;
		EditorUtility.SetDirty(this);
		return true;
	}

	[HideInInspector]
	public string[] m_Labels = new string[0];
}
