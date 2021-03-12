using System.Collections.Generic;
using System.Linq;

namespace ASTools.Validator
{
	public class ExtensionScanner : Scanner
	{
		public ExtensionScanner(ChecklistItem check, string[] extensions, string[] exclusions = null)
		{
			this.extensions = extensions;
			this.checklistItem = check;
			this.exclusions = exclusions;
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
			List<string> pathsWithExtensions = ValidatorData.GetPathsWithExtensions(this.extensions, this.exclusions);
			if (pathsWithExtensions.Any<string>())
			{
				this.checklistItem.AddPaths(pathsWithExtensions);
			}
		}

		private string[] extensions;

		private string[] exclusions;

		private ChecklistItem checklistItem;
	}
}
