using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    public class ReferenceScanner : Scanner
    {
        public ReferenceScanner(ChecklistItem check)
        {
            this.checklistItem = check;
        }

        public override ChecklistItem[] GetChecklistItems
        {
            get
            {
                return new ChecklistItem[]
                {
                    this.checklistItem
                };
            }
        }

        public override void Scan()
        {
            IEnumerable<string> enumerable = from p in AssetDatabase.GetAllAssetPaths()
                                             where ValidatorData.PathInAssetDir(p)
                                             select p;
            foreach (string path in enumerable)
            {
                GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(path);
                if (gameObject != null && ReferenceScanner.IsMissingReference(gameObject))
                {
                    this.checklistItem.AddPath(path);
                }
            }
        }

        private static bool IsMissingReference(GameObject asset)
        {
            Component[] components = asset.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (!component)
                {
                    return true;
                }
            }
            return false;
        }

        private ChecklistItem checklistItem;
    }
}
