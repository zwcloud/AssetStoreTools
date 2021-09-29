using AssetStoreTools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    [InitializeOnLoad]
    public class ValidatorWindow : EditorWindow
    {
        public string PackagePath { get; set; }

        [MenuItem("Asset Store Tools/Package Validator", false, 3)]
        public static void ShowWindow()
        {
            if (ValidatorWindow.window == null)
            {
                ValidatorWindow.window = EditorWindow.GetWindow<ValidatorWindow>();
                ValidatorWindow.window.titleContent = new GUIContent("Validator");
                ValidatorWindow.window.minSize = new Vector2(600f, 500f);
            }
            ValidatorWindow.window.Show();
        }

        public static ValidatorWindow GetAndShowWindow()
        {
            if (ValidatorWindow.window == null)
            {
                ValidatorWindow.window = EditorWindow.GetWindow<ValidatorWindow>();
                ValidatorWindow.window.titleContent = new GUIContent("Validator");
                ValidatorWindow.window.minSize = new Vector2(600f, 500f);
            }
            ValidatorWindow.window.Show();
            return ValidatorWindow.window;
        }

        public void OnEnable()
        {
            if (!this.checkIcon)
            {
                this.checkIcon = (EditorGUIUtility.IconContent("lightMeter/greenLight").image as Texture2D);
            }
            if (!this.errorIcon)
            {
                this.errorIcon = (EditorGUIUtility.IconContent("lightMeter/redLight").image as Texture2D);
            }
            if (!this.warningIcon)
            {
                this.warningIcon = (EditorGUIUtility.IconContent("lightMeter/orangeLight").image as Texture2D);
            }
        }

        private void Indent(int indentions = 1)
        {
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Space((float)(15 * indentions));
        }

        private void DrawEntryIcon(Texture2D icon, int iconSize)
        {
            Rect rect = GUILayoutUtility.GetRect((float)iconSize, (float)iconSize, new GUILayoutOption[]
            {
                GUILayout.MaxWidth((float)iconSize)
            });
            rect.x = 18.5f;
            rect.y += 1.5f;
            GUI.DrawTexture(rect, icon, (ScaleMode)2);
        }

        private void DisplayIncorrectPathPopup(string message)
        {
            EditorUtility.DisplayDialog("Incorrect Path", message, "Close");
        }

        private bool IsValidPath(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                this.DisplayIncorrectPathPopup("The selected path is outside of Project's Assets Folder.\n\nPlease select a location within Assets Folder.");
                return false;
            }
            if (Directory.GetFileSystemEntries(path).Length == 0)
            {
                this.DisplayIncorrectPathPopup("The selected path is an empty folder.\n\nPlease ensure that the selected folder is not empty.");
                return false;
            }
            return true;
        }

        public void OnGUI()
        {
            Checklist checkList = Checklist.GetCheckList();
            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Package Validator", EditorStyles.boldLabel, new GUILayoutOption[0]);
            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Scan your package to check that it does not have common package submission mistakes. Passing this scan does not guarantee that your package will get accepted as the final decision is made by the Unity Asset Store team.\n\nYou can upload your package even if it does not pass some of the criteria as it depends on the type of assets that you upload. For more information, view the messages next to the criteria in the checklist or contact our support team.", new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            }, new GUILayoutOption[0]);
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
            this.PackagePath = EditorGUILayout.TextField("Package path:", this.PackagePath, new GUILayoutOption[0]);
            GUI.SetNextControlName("Set Path");
            if (GUILayout.Button("Set Path", new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(false)
            }))
            {
                string text = EditorUtility.OpenFolderPanel("Select Package Folder", string.Empty, string.Empty);
                if (!string.IsNullOrEmpty(text))
                {
                    string text2 = ValidatorData.ToProjectRelativePath(text);
                    if (this.IsValidPath(text2))
                    {
                        this.PackagePath = text2;
                        GUI.FocusControl("Set Path");
                    }
                }
            }
            if (!string.IsNullOrEmpty(this.PackagePath))
            {
                ValidatorData.SetScanPath(this.PackagePath);
            }
            GUILayout.Space(15f);
            if (GUILayout.Button("Scan", new GUILayoutOption[]
            {
                GUILayout.Width(100f)
            }) && this.IsValidPath(this.PackagePath))
            {
                this.showChecklist = true;
                checkList.Scan();
                this.showErrorItems = true;
                this.showWarningItems = true;
                this.showPassItems = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider, new GUILayoutOption[0]);
            GUIStyle guistyle = new GUIStyle(EditorStyles.boldLabel);
            guistyle.alignment = (TextAnchor)4;
            if (this.showChecklist)
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
                EditorGUILayout.LabelField("Checklist", EditorStyles.boldLabel, new GUILayoutOption[]
                {
                    GUILayout.MaxWidth(75f)
                });
                GUILayout.FlexibleSpace();
                this.showPassItems = GUILayout.Toggle(this.showPassItems, new GUIContent(this.checkIcon, "Passed"), EditorStyles.toolbarButton, new GUILayoutOption[]
                {
                    GUILayout.Width(30f)
                });
                this.showWarningItems = GUILayout.Toggle(this.showWarningItems, new GUIContent(this.warningIcon, "Warnings"), EditorStyles.toolbarButton, new GUILayoutOption[]
                {
                    GUILayout.Width(30f)
                });
                this.showErrorItems = GUILayout.Toggle(this.showErrorItems, new GUIContent(this.errorIcon, "Errors"), EditorStyles.toolbarButton, new GUILayoutOption[]
                {
                    GUILayout.Width(30f)
                });
                EditorGUILayout.EndHorizontal();
                this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos, new GUILayoutOption[0]);
                GUILayout.Space(6f);
                GUIStyle guistyle2 = new GUIStyle(EditorStyles.foldout);
                guistyle2.fontStyle = (FontStyle)1;
                IEnumerable<ChecklistItem> enumerable = from c in checkList.Checks
                                                        where c.Status == CheckStatus.Error & !c.Failed
                                                        select c;
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.showErrorItems = EditorGUILayout.Foldout(this.showErrorItems, "     Errors (" + enumerable.Count<ChecklistItem>() + ")", guistyle2);
                this.DrawEntryIcon(this.errorIcon, 16);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                if (this.showErrorItems)
                {
                    foreach (ChecklistItem check in enumerable)
                    {
                        this.ChecklistItemGUI(check);
                        GUILayout.Space(6f);
                    }
                }
                enumerable = from c in checkList.Checks
                             where c.Status == CheckStatus.Warning || c.Failed
                             orderby c.Failed
                             select c;
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.showWarningItems = EditorGUILayout.Foldout(this.showWarningItems, "     Warnings (" + enumerable.Count<ChecklistItem>() + ")", guistyle2);
                this.DrawEntryIcon(this.warningIcon, 16);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                if (this.showWarningItems)
                {
                    foreach (ChecklistItem check2 in enumerable)
                    {
                        this.ChecklistItemGUI(check2);
                        GUILayout.Space(6f);
                    }
                }
                enumerable = from c in checkList.Checks
                             where c.Status == CheckStatus.Pass & !c.Failed
                             select c;
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.showPassItems = EditorGUILayout.Foldout(this.showPassItems, "     Passed (" + enumerable.Count<ChecklistItem>() + ")", guistyle2);
                this.DrawEntryIcon(this.checkIcon, 16);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
                if (this.showPassItems)
                {
                    foreach (ChecklistItem check3 in enumerable)
                    {
                        this.ChecklistItemGUI(check3);
                        GUILayout.Space(6f);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Scan the selected Package path to receive validation feedback.", guistyle, new GUILayoutOption[0]);
            }
        }

        private void ChecklistItemGUI(ChecklistItem check)
        {
            if (!check.Active)
            {
                return;
            }
            this.Indent(1);
            GUIStyle guistyle = new GUIStyle(EditorStyles.foldout);
            string title = ValidatorData.ItemData[(int)check.Type].Title;
            check.Foldout = EditorGUILayout.Foldout(check.Foldout, " " + title, guistyle);
            GUILayout.EndHorizontal();
            if (check.Foldout)
            {
                string text = ValidatorData.ItemData[(int)check.Type].Message;
                if (check.Failed)
                {
                    text = "<color=#" + ColorUtility.ToHtmlStringRGB(GUIUtil.ErrorColor) + ">An exception occurred when performing this check! Please view the Console for more information.</color>\n\n" + text;
                }
                this.ChecklistMessageGUI(check, text);
                this.ChecklistAssetsGUI(check, check.AssetPaths);
            }
        }

        private void ChecklistMessageGUI(ChecklistItem check, string message)
        {
            this.Indent(2);
            GUILayout.EndHorizontal();
            this.Indent(3);
            EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true
            }, new GUILayoutOption[0]);
            GUILayout.EndHorizontal();
        }

        private void SelectAndPing(List<string> paths)
        {
            IEnumerable<Object> source = from f in paths
                                         select AssetDatabase.LoadMainAssetAtPath(f);
            Selection.objects = source.ToArray<Object>();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private void ChecklistAssetsGUI(ChecklistItem check, List<string> paths)
        {
            if (!paths.Any<string>())
            {
                return;
            }
            this.Indent(2);
            check.FoldoutPaths = EditorGUILayout.Foldout(check.FoldoutPaths, " Related Assets:");
            if (GUILayout.Button("Select All", new GUILayoutOption[]
            {
                GUILayout.MaxWidth(80f),
                GUILayout.MinWidth(80f),
                GUILayout.MaxHeight(20f),
                GUILayout.MinHeight(20f)
            }))
            {
                IEnumerable<Object> source = from f in paths
                                             select AssetDatabase.LoadMainAssetAtPath(f);
                Selection.objects = source.ToArray<Object>();
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            if (check.FoldoutPaths)
            {
                foreach (string text in paths)
                {
                    GUILayout.EndHorizontal();
                    this.Indent(3);
                    Object @object = AssetDatabase.LoadMainAssetAtPath(text);
                    EditorGUILayout.ObjectField(@object, typeof(Object), false, new GUILayoutOption[0]);
                }
            }
            GUILayout.EndHorizontal();
        }

        private const float ChecklistSpacing = 6f;

        private static ValidatorWindow window;

        private Vector2 scrollPos;

        private Texture2D errorIcon;

        private Texture2D warningIcon;

        private Texture2D checkIcon;

        private bool showErrorItems = true;

        private bool showWarningItems = true;

        private bool showPassItems = true;

        private bool showChecklist;
    }
}
