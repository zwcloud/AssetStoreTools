using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FileSelector : EditorWindow
{
	private class AlphabeticComparer : IComparer<FileSelector.FileNode>
	{
		public int Compare(FileSelector.FileNode a, FileSelector.FileNode b)
		{
			return a.Name.CompareTo(b.Name);
		}
	}

	private class FileNode
	{
		private static IComparer<FileSelector.FileNode> alphabeticalComparer = new FileSelector.AlphabeticComparer();

		private string m_RelativePath;

		private string m_Name;

		private bool m_isDir;

		private List<FileSelector.FileNode> m_SubDirectories;

		private List<FileSelector.FileNode> m_SubFiles;

		private List<FileSelector.FileNode> m_Children;

		private Texture m_Icon;

		private bool m_Expanded;

		private bool m_Selected;

		private int m_Depth;

		public List<FileSelector.FileNode> Childrens
		{
			get
			{
				return this.m_Children;
			}
		}

		public List<FileSelector.FileNode> SubDirectories
		{
			get
			{
				return this.m_SubDirectories;
			}
		}

		public List<FileSelector.FileNode> Files
		{
			get
			{
				return this.m_SubFiles;
			}
		}

		public string Name
		{
			get
			{
				return this.m_RelativePath;
			}
		}

		public bool isDirectory
		{
			get
			{
				return this.m_isDir;
			}
		}

		public bool Selected
		{
			get
			{
				return this.m_Selected;
			}
			set
			{
				this.m_Selected = value;
			}
		}

		public bool Expanded
		{
			get
			{
				return this.m_Expanded;
			}
			set
			{
				this.m_Expanded = value;
			}
		}

		public int Depth
		{
			get
			{
				return this.m_Depth;
			}
		}

		public FileNode(FileSystemInfo fileInfo, int depth = 0)
		{
			string fullName = fileInfo.FullName;
			this.m_Name = fileInfo.Name;
			this.m_Depth = depth;
			this.m_RelativePath = "Assets/" + fullName.Substring(Application.dataPath.Length + 1);
			this.m_RelativePath = this.m_RelativePath.Replace("\\", "/");
			this.m_SubFiles = new List<FileSelector.FileNode>();
			this.m_SubDirectories = new List<FileSelector.FileNode>();
			this.m_Children = new List<FileSelector.FileNode>();
			if (fileInfo is DirectoryInfo)
			{
				this.m_Icon = EditorGUIUtility.FindTexture("_Folder");
				if (this.m_Icon == null)
				{
					this.m_Icon = EditorGUIUtility.FindTexture("Folder Icon");
				}
				this.m_isDir = true;
				DirectoryInfo directoryInfo = fileInfo as DirectoryInfo;
				FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
				FileSystemInfo[] array = fileSystemInfos;
				for (int i = 0; i < array.Length; i++)
				{
					FileSystemInfo fileSystemInfo = array[i];
					if (!fileSystemInfo.Name.EndsWith(".meta") && !fileSystemInfo.Name.StartsWith(".") && !fileSystemInfo.Name.EndsWith(".unity"))
					{
						FileSelector.FileNode item = new FileSelector.FileNode(fileSystemInfo, this.m_Depth + 1);
						if (fileSystemInfo is DirectoryInfo)
						{
							this.m_SubDirectories.Add(item);
						}
						else
						{
							this.m_SubFiles.Add(item);
						}
						this.m_Children.Add(item);
					}
				}
				this.m_Children.Sort(FileSelector.FileNode.alphabeticalComparer);
				return;
			}
			this.m_Icon = (AssetDatabase.GetCachedIcon(this.m_RelativePath) as Texture2D);
			if (!this.m_Icon)
			{
				this.m_Icon = EditorGUIUtility.ObjectContent(null, typeof(MonoBehaviour)).image;
			}
		}

		public void RenderIconText()
		{
			GUIContent gUIContent = new GUIContent();
			gUIContent.image = this.m_Icon;
			gUIContent.text = this.m_Name;
			GUILayout.Label(gUIContent.image, new GUILayoutOption[]
			{
				GUILayout.Height(21f),
				GUILayout.Width(21f)
			});
			GUILayout.Label(gUIContent.text, new GUILayoutOption[]
			{
				GUILayout.Height(21f)
			});
			//GUILayout.FlexibleSpace();
		}
	}

	public delegate void DoneCallback(List<string> updatedMainAssets);

	private string m_Directory;

	private FileSelector.FileNode m_RootDir;

	private LinkedList<FileSelector.FileNode> m_SelectedFiles;

	private Vector2 m_FileScrollPos;

	private Vector2 m_FileSelectedScrollPos;

	private FileSelector.DoneCallback m_OnFinishSelecting;

	public static FileSelector Show(string directory, List<string> preSelectedFiles, FileSelector.DoneCallback onFinishSelecting)
	{
		FileSelector fileSelector = EditorWindow.GetWindow(typeof(FileSelector), true, "Please Select Main Assets") as FileSelector;
		fileSelector.minSize = new Vector2(400f, 300f);
		if (!directory.EndsWith("/"))
		{
			directory += "/";
		}
		fileSelector.Init(directory, preSelectedFiles, onFinishSelecting);
		fileSelector.Show();
		return fileSelector;
	}

	public void Init(string directory, List<string> preSelectedFiles, FileSelector.DoneCallback onFinishSelecting)
	{
		this.m_Directory = directory;
		this.m_FileScrollPos = default(Vector2);
		this.m_FileSelectedScrollPos = default(Vector2);
		this.m_OnFinishSelecting = onFinishSelecting;
		string path = Application.dataPath + this.m_Directory;
		DirectoryInfo fileInfo = new DirectoryInfo(path);
		this.m_RootDir = new FileSelector.FileNode(fileInfo, 0);
		preSelectedFiles.Sort();
		this.SelectFiles(preSelectedFiles);
	}

	public void SelectFiles(IList<string> toBeSelected)
	{
		this.m_SelectedFiles = this.GetFileListByName(toBeSelected);
		foreach (FileSelector.FileNode fileNode in this.m_SelectedFiles)
		{
			fileNode.Selected = true;
		}
	}

	public void Accept()
	{
		List<string> list = new List<string>();
		foreach (FileSelector.FileNode fileNode in this.m_SelectedFiles)
		{
			list.Add(fileNode.Name);
		}
		this.m_OnFinishSelecting(list);
		base.Close();
	}

	private LinkedList<FileSelector.FileNode> GetFileListByName(IList<string> selected)
	{
		int num = 0;
		LinkedList<FileSelector.FileNode> linkedList = new LinkedList<FileSelector.FileNode>();
		LinkedList<FileSelector.FileNode> linkedList2 = new LinkedList<FileSelector.FileNode>();
		linkedList2.AddFirst(this.m_RootDir);
		LinkedList<FileSelector.FileNode> linkedList3 = new LinkedList<FileSelector.FileNode>();
		while (linkedList2.Count > 0 && num < selected.Count)
		{
			LinkedListNode<FileSelector.FileNode> first = linkedList2.First;
			linkedList3.Clear();
			if (first.Value.isDirectory)
			{
				foreach (FileSelector.FileNode value in first.Value.Childrens)
				{
					linkedList3.AddFirst(value);
				}
			}
			else
			{
				if (first.Value.Name == selected[num])
				{
					num++;
					linkedList.AddLast(first.Value);
				}
				else if (first.Value.Name.CompareTo(selected[num]) > 0)
				{
					num++;
				}
				if (num >= selected.Count)
				{
					return linkedList;
				}
			}
			linkedList2.RemoveFirst();
			foreach (FileSelector.FileNode value2 in linkedList3)
			{
				linkedList2.AddFirst(value2);
			}
		}
		return linkedList;
	}

    void SelectNode(FileNode fileNode, bool select = true)
    {
        if (!fileNode.isDirectory && fileNode.Selected != select)
        {
            if (select)
                this.m_SelectedFiles.AddLast(fileNode);
            else
                this.m_SelectedFiles.Remove(fileNode);

            fileNode.Selected = select;
        }

        foreach (var node in fileNode.Childrens)
        {
            SelectNode(node, select);
        }
    }

    void ExpandNode(FileNode fileNode, bool expand)
    {
        if (!(fileNode.isDirectory))
            return;

        fileNode.Expanded = expand;

        foreach (var node in fileNode.Childrens)
        {
            if (node.isDirectory)
            {
                ExpandNode(node, expand);
            }
        }
    }

	private void RenderFileTree()
	{
		LinkedList<FileSelector.FileNode> linkedList = new LinkedList<FileSelector.FileNode>();
		linkedList.AddFirst(this.m_RootDir);
		LinkedList<FileSelector.FileNode> linkedList2 = new LinkedList<FileSelector.FileNode>();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Expand all"))
		{
		    foreach (var fileNode in linkedList)
		    {
			ExpandNode(fileNode, true);
		    }
		}

		if (GUILayout.Button("Collapse all"))
		{
		    foreach (var fileNode in linkedList)
		    {
			ExpandNode(fileNode, false);
		    }
		}

		GUILayout.EndHorizontal();

		while (linkedList.Count > 0)
		{
			LinkedListNode<FileSelector.FileNode> first = linkedList.First;
			linkedList2.Clear();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Space((float)(20 * first.Value.Depth));
			if (first.Value.isDirectory)
			{
				GUIStyle guistyle = "IN foldout";
				first.Value.Expanded = GUILayout.Toggle(first.Value.Expanded, GUIContent.none, guistyle, new GUILayoutOption[0]);
			}
			else
			{
				bool flag = GUILayout.Toggle(first.Value.Selected, GUIContent.none, new GUILayoutOption[0]);
				if (flag != first.Value.Selected)
				{
					if (flag)
					{
						this.m_SelectedFiles.AddLast(first.Value);
					}
					else
					{
						this.m_SelectedFiles.Remove(first.Value);
					}
					first.Value.Selected = flag;
				}
			}

            first.Value.RenderIconText();

            if (first.Value.isDirectory)
            {
                if (GUILayout.Button("+"))
                {
                    SelectNode(first.Value, true);
                }

                if (GUILayout.Button("-"))
                {
                    SelectNode(first.Value, false);
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
			if (first.Value.Expanded)
			{
				foreach (FileSelector.FileNode value in first.Value.SubDirectories)
				{
					linkedList2.AddFirst(value);
				}
				foreach (FileSelector.FileNode value2 in first.Value.Files)
				{
					linkedList2.AddFirst(value2);
				}
			}
			linkedList.RemoveFirst();
			foreach (FileSelector.FileNode value3 in linkedList2)
			{
				linkedList.AddFirst(value3);
			}
		}
	}

	private void RenderSelectedFileList()
	{
		LinkedListNode<FileSelector.FileNode> linkedListNode = this.m_SelectedFiles.First;
		while (linkedListNode != null)
		{
			FileSelector.FileNode value = linkedListNode.Value;
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			value.Selected = GUILayout.Toggle(value.Selected, GUIContent.none, new GUILayoutOption[0]);
			value.RenderIconText();
			GUILayout.FlexibleSpace();

			if (MainAssetsUtil.CanPreview && GUILayout.Button("Preview", new GUILayoutOption[0]))
			{
				MainAssetsUtil.Preview(value.Name);
			}
			if (!value.Selected)
			{
				LinkedListNode<FileSelector.FileNode> node = linkedListNode;
				linkedListNode = linkedListNode.Next;
				this.m_SelectedFiles.Remove(node);
			}
			else
			{
				linkedListNode = linkedListNode.Next;
				GUILayout.EndHorizontal();
			}
		}
	}

	public void OnGUI()
	{
		int num = (int)Math.Floor((double)(base.position.width / 2f));
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		GUILayout.Label("Main Assets", EditorStyles.boldLabel, new GUILayoutOption[0]);
        GUILayout.Label(
@"Please select from the list below the main assets in your package. 
You should select items that you consider to be the central parts of your package, and that would showcase your package.
The Asset Store will generate previews for the selected items. 
If you are uploading a Character, the Character prefab would be a good candidate for instance",
            EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);

		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.BeginVertical(new GUILayoutOption[]
		{
			GUILayout.Width((float)num)
		});
		GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[]
		{
			GUILayout.ExpandWidth(true)
		});
		GUILayout.Label("Package Files", EditorStyles.miniLabel, new GUILayoutOption[0]);
		GUILayout.EndHorizontal();
		this.m_FileScrollPos = EditorGUILayout.BeginScrollView(this.m_FileScrollPos, new GUILayoutOption[0]);
		this.RenderFileTree();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.Box(GUIContent.none, GUIUtil.Styles.verticalDelimiter, new GUILayoutOption[]
		{
			GUILayout.MinWidth(1f),
			GUILayout.ExpandHeight(true)
		});
		GUILayout.BeginVertical(new GUILayoutOption[]
		{
			GUILayout.Width((float)num)
		});
		GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[]
		{
			GUILayout.ExpandWidth(true)
		});
		GUILayout.Label("Selected Files", EditorStyles.miniLabel, new GUILayoutOption[0]);
		GUILayout.EndHorizontal();
		this.m_FileSelectedScrollPos = EditorGUILayout.BeginScrollView(this.m_FileSelectedScrollPos, new GUILayoutOption[0]);
		this.RenderSelectedFileList();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.Box(GUIContent.none, GUIUtil.Styles.delimiter, new GUILayoutOption[]
		{
			GUILayout.MinHeight(1f),
			GUILayout.ExpandWidth(true)
		});
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Done", new GUILayoutOption[]
		{
			GUILayout.Width(100f),
			GUILayout.Height(30f)
		}))
		{
			this.Accept();
		}
		GUILayout.EndHorizontal();
	}
}
