using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ASTools.Validator
{
	public class TextureScanner : Scanner
	{
		public TextureScanner(ChecklistItem check)
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
			List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(ValidatorData.TEXTURE_EXTENSIONS, null);
			foreach (string text in pathsWithExtensions)
			{
				Texture2D texture2D = new Texture2D(1, 1);
				ImageConversion.LoadImage(texture2D, File.ReadAllBytes(text));
				TextureImporter textureImporter = AssetImporter.GetAtPath(text) as TextureImporter;
				if (!(textureImporter == null))
				{
					if (texture2D.height > textureImporter.maxTextureSize || texture2D.width > textureImporter.maxTextureSize)
					{
						this.checklistItem.AddPath(text);
					}
				}
			}
		}

		private ChecklistItem checklistItem;
	}
}
