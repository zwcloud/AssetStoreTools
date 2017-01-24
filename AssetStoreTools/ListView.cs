using System;
using System.Collections.Generic;
using UnityEngine;

internal class ListView<T>
{
	public delegate void SelectionCallback(T selected);

	protected IDataSource<T> m_DataSource;

	protected IListViewGUI<T> m_GUI;

	protected int m_KeyboardControlID = -1;

	protected Vector2 m_ScrollPosition = default(Vector2);

	protected Rect m_ScrollWindow = default(Rect);

	protected T m_Selected = default(T);

	protected ListView<T>.SelectionCallback m_SelectionConfirmCallback;

	protected ListView<T>.SelectionCallback m_SelectionChangeCallback;

	public T Selected
	{
		get
		{
			return this.m_Selected;
		}
		set
		{
			this.m_Selected = value;
			if (this.m_Selected != null)
			{
				this.EnsureSelectionIsInView();
			}
		}
	}

	public ListView(IDataSource<T> dataSource, IListViewGUI<T> gui)
	{
		this.m_DataSource = dataSource;
		this.m_GUI = gui;
	}

	public ListView(IDataSource<T> dataSource, IListViewGUI<T> gui, ListView<T>.SelectionCallback selectionChangeCallback, ListView<T>.SelectionCallback selectionConfirmCallback) : this(dataSource, gui)
	{
		this.m_SelectionConfirmCallback = selectionConfirmCallback;
		this.m_SelectionChangeCallback = selectionChangeCallback;
	}

	protected void OffsetSelection(int delta)
	{
		IList<T> visibleRows = this.m_DataSource.GetVisibleRows();
		if (visibleRows.Count == 0)
		{
			return;
		}
		int num = visibleRows.IndexOf(this.m_Selected);
		int num2 = num + delta;
		num2 = Mathf.Clamp(num2, 0, visibleRows.Count - 1);
		this.m_Selected = visibleRows[num2];
	}

	public virtual void EnsureSelectionIsInView()
	{
		IList<T> visibleRows = this.m_DataSource.GetVisibleRows();
		if (visibleRows.Count == 0)
		{
			return;
		}
		int num = visibleRows.IndexOf(this.m_Selected);
		if (num < 0)
		{
			return;
		}
		int num2 = (int)this.m_GUI.GetTopLeftNodePixel(num, visibleRows).y;
		int num3 = num2 - Mathf.FloorToInt(this.m_ScrollWindow.height) + (int)this.m_GUI.GetNodeArea(this.m_Selected).y;
		this.m_ScrollPosition.y = Mathf.Clamp(this.m_ScrollPosition.y, (float)num3, (float)num2);
	}

	protected bool hasFocus()
	{
		return GUIUtility.keyboardControl == this.m_KeyboardControlID;
	}

	private void ChangeSelection(T node)
	{
		this.m_Selected = node;
		if (this.m_SelectionChangeCallback == null)
		{
			return;
		}
		this.m_SelectionChangeCallback(this.Selected);
	}

	protected virtual void HandleNodeEvent(T node, Rect nodeArea)
	{
		Event current = Event.current;
		EventType type = current.type;
		if (type != EventType.MouseDown)
		{
			if (type == EventType.MouseUp)
			{
				if (GUIUtility.hotControl == this.m_KeyboardControlID)
				{
					GUIUtility.hotControl = 0;
					current.Use();
				}
			}
		}
		else if (nodeArea.Contains(Event.current.mousePosition))
		{
			this.ChangeSelection(node);
			this.GetKeyboardControl();
			GUIUtility.hotControl = this.m_KeyboardControlID;
			current.Use();
			if (Event.current.clickCount >= 2)
			{
				this.ConfirmSelection();
			}
		}
	}

	public virtual void OnGUI(Rect rect)
	{
		Vector2 displayArea = new Vector2(rect.width, rect.height);
		IList<T> visibleRows = this.m_DataSource.GetVisibleRows();
		Vector2 totalSize = this.m_GUI.GetTotalSize(visibleRows, displayArea);
		Rect viewRect = new Rect(0f, 0f, totalSize.x, totalSize.y);
		this.m_KeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
		this.m_GUI.BeginRowsGUI();
		this.m_ScrollPosition = GUI.BeginScrollView(rect, this.m_ScrollPosition, viewRect);
		foreach (T current in visibleRows)
		{
			bool selected = current.Equals(this.m_Selected);
			Rect nodeArea = this.m_GUI.OnRowGUI(current, totalSize, selected, this.hasFocus());
			this.HandleNodeEvent(current, nodeArea);
		}
		GUI.EndScrollView();
		this.m_GUI.EndRowsGUI();
		if (Event.current.type != EventType.Layout)
		{
			this.m_ScrollWindow = rect;
		}
		if (this.m_ScrollWindow.Contains(Event.current.mousePosition))
		{
			EventType type = Event.current.type;
			if (type != EventType.MouseDown)
			{
				if (type == EventType.ScrollWheel)
				{
					this.m_ScrollPosition += Event.current.delta;
					DebugUtils.LogWarning(Event.current.delta.ToString());
				}
			}
			else
			{
				this.GetKeyboardControl();
			}
		}
		this.KeyboardHandling();
	}

	protected void GetKeyboardControl()
	{
		GUIUtility.keyboardControl = this.m_KeyboardControlID;
	}

	protected void ConfirmSelection()
	{
		if (this.m_SelectionConfirmCallback == null)
		{
			return;
		}
		this.m_SelectionConfirmCallback(this.Selected);
	}

	protected virtual void KeyboardHandling()
	{
		if (Event.current.type == EventType.KeyDown && this.m_KeyboardControlID == GUIUtility.keyboardControl)
		{
			KeyCode keyCode = Event.current.keyCode;
			if (keyCode != KeyCode.UpArrow)
			{
				if (keyCode != KeyCode.DownArrow)
				{
					if (keyCode == KeyCode.Return)
					{
						this.ConfirmSelection();
					}
				}
				else
				{
					this.OffsetSelection(1);
					this.EnsureSelectionIsInView();
					Event.current.Use();
				}
			}
			else
			{
				this.OffsetSelection(-1);
				this.EnsureSelectionIsInView();
				Event.current.Use();
			}
		}
	}
}
