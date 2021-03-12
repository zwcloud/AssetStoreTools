using System.Collections.Generic;
using UnityEngine;

internal class DefaultListViewGUI<T> : IListViewGUI<T>
{
	private static DefaultListViewGUI<T>.GUIStyles Styles
	{
		get
		{
			if (DefaultListViewGUI<T>.s_Styles == null)
			{
				DefaultListViewGUI<T>.s_Styles = new DefaultListViewGUI<T>.GUIStyles();
			}
			return DefaultListViewGUI<T>.s_Styles;
		}
	}

	protected virtual GUIStyle GetLineStyle()
	{
		GUIStyle listNodeTextField = DefaultListViewGUI<T>.Styles.ListNodeTextField;
		listNodeTextField.padding.left = 5;
		return listNodeTextField;
	}

	public virtual Rect OnRowGUI(T node, Vector2 contentSize, bool selected, bool focus)
	{
		Vector2 nodeArea = this.GetNodeArea(node);
		Rect rect = new Rect(0f, (float)this.m_HeightOffset, contentSize.x, nodeArea.y);
		this.m_HeightOffset += (int)rect.height;
		if (Event.current.type == (EventType)7)
		{
			GUIContent guicontent = new GUIContent(string.Empty);
			GUIStyle lineStyle = this.GetLineStyle();
			guicontent.text = this.GetDisplayName(node);
			guicontent.image = this.GetDisplayIcon(node);
			lineStyle.Draw(rect, guicontent, false, selected, selected, focus);
		}
		return rect;
	}

	protected virtual string GetDisplayName(T node)
	{
		return string.Empty;
	}

	protected virtual Texture GetDisplayIcon(T node)
	{
		return null;
	}

	public virtual void BeginRowsGUI()
	{
		this.m_HeightOffset = 0;
	}

	public virtual void EndRowsGUI()
	{
		this.m_HeightOffset = 0;
	}

	public virtual Vector2 GetTopLeftNodePixel(int index, IList<T> visibleRows)
	{
		Vector2 result = new Vector2(0f, 0f);
		float y = this.GetNodeArea(visibleRows[index]).y;
		result.y = (float)index * y;
		return result;
	}

	public virtual Vector2 GetNodeArea(T node)
	{
		return DefaultListViewGUI<T>.Styles.ListNodeTextField.CalcSize(GUIContent.none);
	}

	public virtual Vector2 GetTotalSize(IList<T> visibleRows, Vector2 displayArea)
	{
        Vector2 result = new Vector2(0f, 0f);
		if (visibleRows == null || visibleRows.Count == 0)
		{
			return result;
		}
		float y = this.GetNodeArea(visibleRows[0]).y;
		result.y = (float)visibleRows.Count * y;
		if (result.x == 0f)
		{
			result.x = displayArea.x;
			if (result.y > displayArea.y)
			{
				result.x -= DefaultListViewGUI<T>.Styles.VerticalScrollBar.CalcSize(GUIContent.none).x;
			}
		}
		if (result.y == 0f)
		{
			result.y = displayArea.y;
			if (result.x > displayArea.x)
			{
				result.y -= DefaultListViewGUI<T>.Styles.HorizontalScrollbar.CalcSize(GUIContent.none).y;
			}
		}
		return result;
	}

	private static DefaultListViewGUI<T>.GUIStyles s_Styles;

	protected int m_HeightOffset;

	private class GUIStyles
	{
		public GUIStyles()
		{
			this.ListNodeTextField.alignment = TextAnchor.MiddleLeft;
			this.ListNodeTextField.padding.top = 2;
			this.ListNodeTextField.padding.bottom = 2;
		}

		internal GUIStyle ListNodeTextField = new GUIStyle("PR Label");

		internal GUIStyle VerticalScrollBar = "verticalScrollbar";

		internal GUIStyle HorizontalScrollbar = "horizontalScrollbar";
	}
}
