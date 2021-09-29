using UnityEditor;

namespace ASTools.Validator
{
    internal class ValidatorPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            Checklist checklist = ValidatorData.LoadAssetAtPath<Checklist>(ValidatorData.MANAGER_PATH);
            if (checklist == null)
            {
                return;
            }
            foreach (ChecklistItem checklistItem in checklist.Checks)
            {
                checklistItem.CheckAssetsForDeletion(deletedAssets);
                checklistItem.CheckAssetsForMove(movedFromAssetPaths, movedAssets);
            }
        }
    }
}
