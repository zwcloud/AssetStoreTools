using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
    [System.Serializable]
    public class ChecklistItem : ScriptableObject
    {
        internal void Init(ValidatorData.CheckItemData data)
        {
            this.Type = data.Type;
            EditorUtility.SetDirty(this);
        }

        internal void AddPath(string path)
        {
            if (!this.AssetPaths.Contains(path))
            {
                this.AssetPaths.Add(path);
            }
        }

        internal void AddPaths(List<string> paths)
        {
            foreach (string path in paths)
            {
                this.AddPath(path);
            }
        }

        internal void Clear()
        {
            this.AssetPaths.Clear();
            this.Status = CheckStatus.Pass;
            this.Failed = false;
        }

        internal void UpdateState()
        {
            DetectionType detection = ValidatorData.ItemData[(int)this.Type].Detection;
            bool flag = (detection != DetectionType.ErrorOnAbsence) ? this.AssetPaths.Any<string>() : (!this.AssetPaths.Any<string>());
            this.Status = ((!flag) ? CheckStatus.Pass : ((detection != DetectionType.WarningOnDetect) ? CheckStatus.Error : CheckStatus.Warning));
            this.Foldout = flag;
        }

        internal void CheckAssetsForDeletion(string[] deletedAssets)
        {
            int count = this.AssetPaths.Count;
            deletedAssets = (from d in deletedAssets
                             select Path.GetFullPath(d)).ToArray<string>();
            this.AssetPaths.RemoveAll((string asset) => deletedAssets.Contains(Path.GetFullPath(asset)));
            if (this.AssetPaths.Count != count)
            {
                this.UpdateState();
            }
        }

        internal void CheckAssetsForMove(string[] movedFromAssetPaths, string[] movedAssets)
        {
            bool flag = false;
            for (int i = 0; i < movedAssets.Length; i++)
            {
                string m = movedFromAssetPaths[i];
                int num = this.AssetPaths.FindIndex((string x) => Path.GetFullPath(x).Equals(Path.GetFullPath(m)));
                if (num > -1)
                {
                    this.AssetPaths[num] = movedAssets[i];
                    flag = true;
                }
            }
            if (flag)
            {
                this.UpdateState();
            }
        }

        public CheckType Type;

        [SerializeField]
        public List<string> AssetPaths = new List<string>();

        public CheckStatus Status;

        public bool Active = true;

        public bool Foldout;

        public bool FoldoutMessage = true;

        public bool FoldoutPaths = true;

        public bool Failed;
    }
}
