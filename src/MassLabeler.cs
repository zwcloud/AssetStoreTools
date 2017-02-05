using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal class MassLabeler : EditorWindow
{
	private const string kLabelListPath = "Assets/AssetStoreTools/Labels.asset";

	private static LabelList m_Labels;

	private string m_LabelAdditionField = string.Empty;

	private Dictionary<int, object> m_CheckedLabels = new Dictionary<int, object>();

	private Vector2 m_ListScroll = Vector2.zero;

	private static LabelList Labels
	{
		get
		{
			if (MassLabeler.m_Labels == null)
			{
				MassLabeler.m_Labels = (AssetDatabase.LoadAssetAtPath("Assets/AssetStoreTools/Labels.asset", typeof(LabelList)) as LabelList);
				if (MassLabeler.m_Labels == null)
				{
					DebugUtils.Log("Creating new label list");
					MassLabeler.m_Labels = (ScriptableObject.CreateInstance(typeof(LabelList)) as LabelList);
					if (MassLabeler.m_Labels == null)
					{
						DebugUtils.LogError("Failed to create label list");
						return null;
					}
					string directoryName = Path.GetDirectoryName("Assets/AssetStoreTools/Labels.asset");
					if (!Directory.Exists(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					AssetDatabase.CreateAsset(MassLabeler.m_Labels, "Assets/AssetStoreTools/Labels.asset");
					EditorUtility.SetDirty(MassLabeler.m_Labels);
					AssetDatabase.Refresh();
				}
			}
			return MassLabeler.m_Labels;
		}
	}

	[MenuItem("Asset Store Tools/Mass Labeler", false, 1000)]
	public static void Launch()
	{
		EditorWindow.GetWindow(typeof(MassLabeler)).Show();
	}

	private void OnEnable()
	{
		this.UpdateLabelSelection();
	}

	private void OnSelectionChange()
	{
		this.UpdateLabelSelection();
	}

	private void UpdateLabelSelection()
	{
		this.m_CheckedLabels = new Dictionary<int, object>();
		UnityEngine.Object[] objects = Selection.objects;
		for (int i = 0; i < objects.Length; i++)
		{
			UnityEngine.Object obj = objects[i];
			string[] labels = AssetDatabase.GetLabels(obj);
			string[] array = labels;
			for (int j = 0; j < array.Length; j++)
			{
				string label = array[j];
				MassLabeler.Labels.Add(label);
				this.m_CheckedLabels[MassLabeler.Labels.IndexOf(label)] = null;
			}
		}
		base.Repaint();
	}

	private void ApplyLabels()
	{
		List<string> list = new List<string>();
		foreach (int current in this.m_CheckedLabels.Keys)
		{
			list.Add(MassLabeler.Labels[current]);
		}
		UnityEngine.Object[] objects = Selection.objects;
		for (int i = 0; i < objects.Length; i++)
		{
			UnityEngine.Object @object = objects[i];
			AssetDatabase.SetLabels(@object, list.ToArray());
			EditorUtility.SetDirty(@object);
		}
	}

	private void OnGUI()
	{
		this.OnAddLabelAreaGUI();
		this.m_ListScroll = GUILayout.BeginScrollView(this.m_ListScroll, new GUILayoutOption[0]);
		this.OnLabelListGUI();
		GUILayout.EndScrollView();
		if (Selection.objects.Length > 1)
		{
			GUI.contentColor = Color.yellow;
			GUI.backgroundColor = Color.red;
			GUILayout.Box("WARNING: Applying on multi-select will override any labels only present on one item in the selection.", new GUILayoutOption[]
			{
				GUILayout.ExpandWidth(true)
			});
		}
		if (GUILayout.Button("Apply to selection", new GUILayoutOption[]
		{
			GUILayout.ExpandWidth(true)
		}))
		{
			this.ApplyLabels();
		}
	}

	private void OnAddLabelAreaGUI()
	{
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		this.m_LabelAdditionField = EditorGUILayout.TextField("New label", this.m_LabelAdditionField, new GUILayoutOption[0]).Replace(" ", string.Empty);
		if (GUILayout.Button("Add", new GUILayoutOption[0]))
		{
			MassLabeler.Labels.Add(this.m_LabelAdditionField);
			this.m_LabelAdditionField = string.Empty;
		}
		GUILayout.EndHorizontal();
	}

	private void OnLabelListGUI()
	{
		for (int i = 0; i < MassLabeler.Labels.Count; i++)
		{
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			if (GUILayout.Toggle(this.m_CheckedLabels.ContainsKey(i), MassLabeler.Labels[i], GUI.skin.GetStyle("Button"), new GUILayoutOption[]
			{
				GUILayout.ExpandWidth(true)
			}))
			{
				this.m_CheckedLabels[i] = null;
			}
			else
			{
				this.m_CheckedLabels.Remove(i);
			}
			if (GUILayout.Button("Delete", new GUILayoutOption[]
			{
				GUILayout.ExpandWidth(false)
			}))
			{
				this.m_CheckedLabels.Remove(i);
				MassLabeler.Labels.Remove(MassLabeler.Labels[i]);
			}
			GUILayout.EndHorizontal();
		}
	}
}
