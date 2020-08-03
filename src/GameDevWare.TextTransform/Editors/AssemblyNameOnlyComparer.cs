using System.Collections.Generic;
using System.Reflection;

namespace GameDevWare.TextTransform.Editors
{
	internal class AssemblyNameOnlyComparer : IComparer<AssemblyName>, IEqualityComparer<AssemblyName>
	{
		public static readonly AssemblyNameOnlyComparer Default = new AssemblyNameOnlyComparer();

		public int Compare(AssemblyName x, AssemblyName y)
		{
			if (ReferenceEquals(x, y))
				return 0;
			if (ReferenceEquals(x, null))
				return -1;
			if (ReferenceEquals(y, null))
				return 1;

			return x.Name.CompareTo(y.Name);
		}

		public bool Equals(AssemblyName x, AssemblyName y)
		{
			return Compare(x, y) == 0;
		}

		public int GetHashCode(AssemblyName obj)
		{
			if (obj == null)
				return 0;

			return obj.Name.GetHashCode();
		}
	}
}
