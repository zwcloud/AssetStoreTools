using UnityEditor;
using UnityEngine;

namespace AssetStoreTools
{
    internal static class GUIUtil
    {
        public static GUIUtil.GUIStyles Styles
        {
            get
            {
                if (GUIUtil.s_Styles == null)
                {
                    GUIUtil.s_Styles = new GUIUtil.GUIStyles();
                }
                return GUIUtil.s_Styles;
            }
        }

        public static Texture2D Logo
        {
            get
            {
                if (GUIUtil.sLogo == null)
                {
                    GUIUtil.sLogo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AssetStoreTools/Editor/icon.png", typeof(Texture2D));
                    GUIUtil.sLogo.hideFlags = (HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.NotEditable);
                }
                return GUIUtil.sLogo;
            }
        }

        public static GUIContent Notice
        {
            get
            {
                if (GUIUtil.sNotice == null)
                {
                    Texture2D texture2D = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AssetStoreTools/Editor/notice.png", typeof(Texture2D));
                    texture2D.hideFlags = (HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.NotEditable);
                    GUIUtil.sNotice = new GUIContent(texture2D);
                }
                return GUIUtil.sNotice;
            }
        }

        private static Texture2D LoadRequiredIcon(string name)
        {
            Texture2D texture2D = EditorGUIUtility.Load("Icons/" + name) as Texture2D;
            return (!(texture2D != null)) ? (EditorGUIUtility.LoadRequired("Builtin Skins/Icons/" + name) as Texture2D) : texture2D;
        }

        public static Texture2D WarningIcon
        {
            get
            {
                if (GUIUtil.sIconWarningSmall == null)
                {
                    GUIUtil.sIconWarningSmall = GUIUtil.LoadRequiredIcon("console.warnicon.sml.png");
                }
                return GUIUtil.sIconWarningSmall;
            }
        }

        public static GUIContent StatusWheel
        {
            get
            {
                if (GUIUtil.sStatusWheel == null)
                {
                    GUIUtil.sStatusWheel = new GUIContent[12];
                    for (int i = 0; i < 12; i++)
                    {
                        GUIContent guicontent = new GUIContent();
                        guicontent.image = GUIUtil.LoadRequiredIcon("WaitSpin" + i.ToString("00") + ".png");
                        GUIUtil.sStatusWheel[i] = guicontent;
                    }
                }
                int num = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10f, 11.99f);
                return GUIUtil.sStatusWheel[num];
            }
        }

        public static Rect RectOnRect(float width, float height, Rect target)
        {
            float x = target.x;
            if (target.width >= width)
            {
                x += (target.width - width) * 0.5f;
            }
            float y = 100f;
            if (target.height >= height)
            {
                y = target.y;
            }
            return new Rect(x, y, width, height);
        }

        private static GUIUtil.GUIStyles s_Styles;

        private static Texture2D sLogo;

        private static GUIContent sNotice;

        private static Texture2D sIconWarningSmall;

        private static GUIContent[] sStatusWheel;

        public class GUIStyles
        {
            internal GUIStyles()
            {
                this.delimiter = new GUIStyle(this.delimiter);
                this.delimiter.margin = new RectOffset(0, 0, 0, 0);
                this.delimiter.padding = new RectOffset(0, 0, 0, 0);
                this.delimiter.border = new RectOffset(0, 0, 1, 0);
                this.delimiter.fixedHeight = 1f;
                this.verticalDelimiter = new GUIStyle(this.verticalDelimiter);
                this.verticalDelimiter.margin = new RectOffset(0, 0, 0, 0);
                this.verticalDelimiter.padding = new RectOffset(0, 0, 0, 0);
                this.verticalDelimiter.border = new RectOffset(1, 0, 0, 0);
                this.verticalDelimiter.fixedWidth = 1f;
                this.dimmedTextArea = new GUIStyle(GUI.skin.textArea);
                this.dimmedTextArea.normal.textColor = Color.gray;
            }

            internal readonly GUIStyle delimiter = "GroupBox";

            internal readonly GUIStyle verticalDelimiter = "GroupBox";

            internal readonly GUIStyle dimmedTextArea;
        }
    }

}