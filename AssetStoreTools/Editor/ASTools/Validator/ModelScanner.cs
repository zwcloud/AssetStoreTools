using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
	public class ModelScanner : Scanner
	{
		public ModelScanner(ChecklistItem prefabsCheck, ChecklistItem mixamoCheck, ChecklistItem animationCheck, ChecklistItem orientationCheck)
		{
			this.prefabsCheck = prefabsCheck;
			this.mixamoCheck = mixamoCheck;
			this.animationCheck = animationCheck;
			this.orientationCheck = orientationCheck;
		}

		public override ChecklistItem[] GetChecklistItems
		{
			get
			{
				return new ChecklistItem[]
				{
					this.prefabsCheck,
					this.mixamoCheck,
					this.animationCheck,
					this.orientationCheck
				};
			}
		}

		public override void Scan()
		{
			this.ScanForPrefabs();
			this.ScanForMixamo();
			this.ScanForAnimations();
			this.ScanForOrientations();
		}

		private void ScanForPrefabs()
		{
			List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(ValidatorData.PREFAB_EXTENSIONS, null);
			HashSet<string> hashSet = new HashSet<string>();
			foreach (string path in pathsWithExtensions)
			{
				GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(path);
				if (gameObject != null)
				{
					List<Mesh> meshes = ValidatorData.GetMeshes(gameObject);
					foreach (Mesh mesh in meshes)
					{
						string assetPath = AssetDatabase.GetAssetPath(mesh);
						hashSet.Add(assetPath);
					}
				}
			}
			List<string> modelPaths = ValidatorData.GetModelPaths();
			List<string> paths = modelPaths.Except(hashSet, new CustomPathComparer()).ToList<string>();
			this.prefabsCheck.AddPaths(paths);
		}

		private void ScanForMixamo()
		{
			List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(new string[]
			{
				".fbx"
			}, null);
			foreach (string text in pathsWithExtensions)
			{
				bool flag = ValidatorUtils.IsMixamoFbx(text);
				if (flag)
				{
					this.mixamoCheck.AddPath(text);
				}
			}
		}

		private void ScanForAnimations()
		{
			List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(ValidatorData.MODEL_EXTENSIONS, null);
			foreach (string text in pathsWithExtensions)
			{
				List<ModelImporterClipAnimation> list = new List<ModelImporterClipAnimation>();
				ModelImporter modelImporter = (ModelImporter)AssetImporter.GetAtPath(text);
				list.AddRange(modelImporter.clipAnimations);
				list.AddRange(modelImporter.defaultClipAnimations);
				HashSet<string> hashSet = new HashSet<string>();
				for (int i = 0; i < list.Count; i++)
				{
					if (!hashSet.Add(list[i].name))
					{
						this.animationCheck.AddPath(text);
						break;
					}
				}
			}
		}

		private void ScanForOrientations()
		{
			List<string> modelPaths = ValidatorData.GetModelPaths();
			foreach (string path in modelPaths)
			{
				GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(path);
				if (!ModelScanner.IsUpright(gameObject))
				{
					this.orientationCheck.AddPath(AssetDatabase.GetAssetPath(gameObject));
				}
			}
		}

		private static bool IsUpright(GameObject model)
		{
			Transform[] componentsInChildren = model.GetComponentsInChildren<Transform>(true);
			foreach (Transform transform in componentsInChildren)
			{
				if (transform.localRotation != Quaternion.identity && ValidatorData.GetMesh(transform))
				{
					return false;
				}
			}
			return true;
		}

		private ChecklistItem prefabsCheck;

		private ChecklistItem mixamoCheck;

		private ChecklistItem animationCheck;

		private ChecklistItem orientationCheck;
	}
}
