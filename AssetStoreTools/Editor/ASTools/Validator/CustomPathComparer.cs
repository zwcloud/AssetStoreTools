using System.Collections.Generic;
using System.IO;

namespace ASTools.Validator
{
	internal class CustomPathComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			x = x.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			y = y.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			return x.Equals(y);
		}

		public int GetHashCode(string s)
		{
			return s.GetHashCode();
		}
	}
}
