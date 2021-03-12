using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ASTools.Validator
{
	public class PrefabScanner : Scanner
	{
		public PrefabScanner(ChecklistItem colliderItem, ChecklistItem transformItem, ChecklistItem emptyItem, ChecklistItem lodsItem)
		{
			this.colliderCheck = colliderItem;
			this.transformCheck = transformItem;
			this.emptyCheck = emptyItem;
			this.lodsCheck = lodsItem;
		}

		public override ChecklistItem[] GetChecklistItems
		{
			get
			{
				return new ChecklistItem[]
				{
					this.colliderCheck,
					this.transformCheck,
					this.emptyCheck,
					this.lodsCheck
				};
			}
		}

		public override void Scan()
		{
			List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(ValidatorData.PREFAB_EXTENSIONS, null);
			foreach (string path in pathsWithExtensions)
			{
				GameObject gameObject = ValidatorData.LoadAssetAtPath<GameObject>(path);
				if (gameObject != null)
				{
					if (PrefabScanner.NeedsCollider(gameObject))
					{
						this.colliderCheck.AddPath(path);
					}
					if (PrefabScanner.NeedsTransformReset(gameObject))
					{
						this.transformCheck.AddPath(path);
					}
					if (PrefabScanner.IsPrefabEmpty(gameObject))
					{
						this.emptyCheck.AddPath(path);
					}
					if (PrefabScanner.HasIncorrectLODs(gameObject))
					{
						this.lodsCheck.AddPath(path);
					}
				}
				else
				{
					this.emptyCheck.AddPath(path);
				}
			}
		}

		private static bool NeedsCollider(GameObject go)
		{
			List<Mesh> meshes = ValidatorData.GetMeshes(go);
			if (!meshes.Any<Mesh>())
			{
				return false;
			}
			Collider[] componentsInChildren = go.GetComponentsInChildren<Collider>(true);
			return !componentsInChildren.Any<Collider>();
		}

		private static bool NeedsTransformReset(GameObject go)
		{
			List<Mesh> meshes = ValidatorData.GetMeshes(go);
			return meshes.Any<Mesh>() && !go.transform.localToWorldMatrix.isIdentity;
		}

		private static bool IsPrefabEmpty(GameObject go)
		{
			Component[] components = go.GetComponents<Component>();
			return components.Length <= 1 && go.transform.childCount <= 0;
		}

		private static bool HasIncorrectLODs(GameObject go)
		{
			MeshFilter[] componentsInChildren = go.GetComponentsInChildren<MeshFilter>();
			bool flag = go.GetComponent<LODGroup>() != null;
			foreach (MeshFilter meshFilter in componentsInChildren)
			{
				if (meshFilter.name.Contains("LOD") && !flag)
				{
					return true;
				}
			}
			return false;
		}

		private ChecklistItem colliderCheck;

		private ChecklistItem transformCheck;

		private ChecklistItem emptyCheck;

		private ChecklistItem lodsCheck;
	}
}
