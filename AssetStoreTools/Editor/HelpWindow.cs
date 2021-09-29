using UnityEditor;
using UnityEngine;

namespace AssetStoreTools
{
    public class HelpWindow : EditorWindow
    {
        [MenuItem("Asset Store Tools/Help", false, 50)]
        public static void ShowHelpWindow()
        {
            HelpWindow window = EditorWindow.GetWindow<HelpWindow>();
            GUIContent guicontent = EditorGUIUtility.IconContent("_Help");
            guicontent.text = "Help";
            window.titleContent = guicontent;
            window.minSize = HelpWindow.MIN_SIZE;
            window.Show();
        }

        private void OnGUI()
        {
            this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos, new GUILayoutOption[0]);
            GUILayout.Space(8f);
            this.BeginRenderSection("About Asset Store Tools", "Using the \"Asset Store Tools\" you can easily upload your content to the Unity Asset Store.\n\n  • To upload your content, click on the \"Package Upload\" tab in the Asset Store Tools menu, login to your Publisher Account, and follow the steps in the \"Package Upload\" window.\n  • To avoid the most common submission mistakes, you can scan the package that you want to upload using the  \"Package Validator\" window.\n\nIf you are facing any difficulties or have any further questions, please check the links in the \"Support\" section below.", ref this.foldoutAbout);
            this.EndRenderSection();
            GUILayout.Space(4f);
            this.BeginRenderSection("Support", string.Empty, ref this.foldoutSupport);
            if (this.foldoutSupport)
            {
                GUILayout.Space(8f);
                this.RenderHyperlink("https://unity3d.com/asset-store/sell-assets/submission-guidelines", "https://unity3d.com/asset-store/sell-assets/submission-guidelines", "Submission Guidelines:");
                GUILayout.Space(8f);
                this.RenderHyperlink("https://docs.unity3d.com/Manual/AssetStorePublishing.html", "https://docs.unity3d.com/Manual/AssetStorePublishing.html", "Asset Store Publishing:");
                GUILayout.Space(8f);
                this.RenderHyperlink("https://docs.unity3d.com/Manual/AssetStoreFAQ.html", "https://docs.unity3d.com/Manual/AssetStoreFAQ.html", "Asset Store FAQ:");
                GUILayout.Space(8f);
                this.RenderHyperlink("https://unity.com/support-services", "https://unity.com/support-services", "Support:");
                GUILayout.Space(4f);
            }
            this.EndRenderSection();
            EditorGUILayout.EndScrollView();
        }

        private void BeginRenderSection(string title, string content, ref bool shouldFoldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, new GUILayoutOption[0]);
            EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUIStyle guistyle = new GUIStyle(EditorStyles.foldout);
            guistyle.margin = new RectOffset(10, 0, 15, 0);
            if (shouldFoldout)
            {
                guistyle.normal.background = guistyle.onActive.background;
            }
            else
            {
                guistyle.normal.background = guistyle.active.background;
            }
            GUILayout.Label(string.Empty, guistyle, new GUILayoutOption[0]);
            GUIStyle guistyle2 = new GUIStyle(EditorStyles.helpBox);
            guistyle2.onHover.background = Texture2D.blackTexture;
            guistyle2.normal.background = Texture2D.blackTexture;
            guistyle2.fontSize = 16;
            if (EditorGUIUtility.isProSkin)
            {
                guistyle2.normal.textColor = new Color(1f, 1f, 1f);
            }
            EditorGUILayout.LabelField(new GUIContent(title, EditorGUIUtility.IconContent("d_console.infoicon").image), guistyle2, new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), (MouseCursor)4);
            if (GUIUtil.IsClickedOnLastRect())
            {
                shouldFoldout = !shouldFoldout;
            }
            if (shouldFoldout)
            {
                HelpWindow.RenderHorizontalLine(new Color32(37, 37, 37, byte.MaxValue));
                if (!string.IsNullOrEmpty(content))
                {
                    EditorGUILayout.LabelField(content, new GUIStyle(EditorStyles.wordWrappedLabel)
                    {
                        richText = true
                    }, new GUILayoutOption[0]);
                }
            }
        }

        private void EndRenderSection()
        {
            EditorGUILayout.EndVertical();
        }

        private void RenderHyperlink(string text, string hyperlink, string label = "")
        {
            EditorGUILayout.BeginVertical(new GUILayoutOption[0]);
            GUILayout.Label(label, new GUIStyle(EditorStyles.boldLabel)
            {
                padding = new RectOffset(2, 0, 0, 0),
                margin = new RectOffset(4, 0, 0, 0)
            }, new GUILayoutOption[0]);
            GUIStyle guistyle = new GUIStyle(EditorStyles.label);
            guistyle.richText = true;
            GUIContent guicontent = new GUIContent(string.Format("<color=#409a9b>{0}</color>", text));
            if (GUILayout.Button(guicontent, guistyle, new GUILayoutOption[0]))
            {
                Application.OpenURL(hyperlink);
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), (MouseCursor)4);
            EditorGUILayout.EndVertical();
        }

        private static void RenderHorizontalLine(Color color)
        {
            GUIStyle guistyle = new GUIStyle();
            guistyle.normal.background = EditorGUIUtility.whiteTexture;
            guistyle.margin = new RectOffset(6, 6, 4, 8);
            guistyle.fixedHeight = 1f;
            Color color2 = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, guistyle, new GUILayoutOption[0]);
            GUI.color = color2;
        }

        internal static readonly Vector2 MIN_SIZE = new Vector2(420f, 475f);

        private Vector2 scrollPos;

        private bool foldoutAbout = true;

        private bool foldoutSupport = true;
    }

}