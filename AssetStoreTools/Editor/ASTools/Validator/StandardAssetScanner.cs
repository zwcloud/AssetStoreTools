using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ASTools.Validator
{
	public class StandardAssetScanner : Scanner
	{
		public StandardAssetScanner(ChecklistItem check)
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
			List<string> paths = (from s in Directory.GetDirectories(Application.dataPath)
			where s.Contains("Standard Assets")
			select s).ToList<string>();
			this.checklistItem.AddPaths(paths);
		}

		private ChecklistItem checklistItem;
	}
}
