using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    [System.Serializable]
    public class Checklist : ScriptableObject
    {
        private static void CreateChecklist()
        {
            Checklist._checklist = ScriptableObject.CreateInstance<Checklist>();
            AssetDatabase.CreateAsset(Checklist._checklist, ValidatorData.MANAGER_PATH);
            foreach (ValidatorData.CheckItemData data in ValidatorData.ItemData)
            {
                Checklist._checklist.AddCheck(data);
            }
            EditorUtility.SetDirty(Checklist._checklist);
            AssetDatabase.ImportAsset(ValidatorData.MANAGER_PATH);
            AssetDatabase.SaveAssets();
        }

        private void AddCheck(ValidatorData.CheckItemData data)
        {
            ChecklistItem checklistItem = ScriptableObject.CreateInstance<ChecklistItem>();
            checklistItem.Init(data);
            AssetDatabase.AddObjectToAsset(checklistItem, ValidatorData.MANAGER_PATH);
            this.Checks.Add(checklistItem);
        }

        internal static Checklist GetCheckList()
        {
            Checklist._checklist = ValidatorData.LoadAssetAtPath<Checklist>(ValidatorData.MANAGER_PATH);
            if (Checklist._checklist == null)
            {
                Checklist.CreateChecklist();
            }
            return Checklist._checklist;
        }

        internal static ChecklistItem GetCheck(CheckType check)
        {
            return Checklist._checklist.Checks[(int)check];
        }

        public void Scan()
        {
            foreach (ChecklistItem checklistItem in this.Checks)
            {
                checklistItem.Clear();
            }
            List<Scanner> list = new List<Scanner>
            {
                new ExtensionScanner(Checklist.GetCheck(CheckType.Demo), ValidatorData.DEMO_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Jpg), ValidatorData.JPG_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Prepackage), ValidatorData.PACKAGE_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Documentation), ValidatorData.DOC_EXTENSIONS, ValidatorData.EXCLUDED_DIRECTORIES),
                new ExtensionScanner(Checklist.GetCheck(CheckType.JavaScript), ValidatorData.JS_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Mp3), ValidatorData.MP3_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Video), ValidatorData.VIDEO_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.Executable), ValidatorData.EXECUTABLE_EXTENSIONS, null),
                new ExtensionScanner(Checklist.GetCheck(CheckType.SpeedTree), ValidatorData.SPEEDTREE_EXTENSIONS, null),
                new PrefabScanner(Checklist.GetCheck(CheckType.PrefabCollider), Checklist.GetCheck(CheckType.PrefabTransform), Checklist.GetCheck(CheckType.PrefabEmpty), Checklist.GetCheck(CheckType.LODs)),
                new ReferenceScanner(Checklist.GetCheck(CheckType.MissingReference)),
                new ModelScanner(Checklist.GetCheck(CheckType.ModelPrefabs), Checklist.GetCheck(CheckType.Mixamo), Checklist.GetCheck(CheckType.Animation), Checklist.GetCheck(CheckType.Orientation)),
                new TextureScanner(Checklist.GetCheck(CheckType.Texture))
            };
            foreach (Scanner scanner in list)
            {
                try
                {
                    scanner.Scan();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Validator check failed with " + scanner.GetType().ToString() + "\n\n" + ex.ToString());
                    foreach (ChecklistItem checklistItem2 in scanner.GetChecklistItems)
                    {
                        checklistItem2.Failed = true;
                    }
                }
            }
            foreach (ChecklistItem checklistItem3 in this.Checks)
            {
                checklistItem3.UpdateState();
            }
        }

        private static Checklist _checklist;

        [SerializeField]
        internal List<ChecklistItem> Checks = new List<ChecklistItem>();
    }
}
