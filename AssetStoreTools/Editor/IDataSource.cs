using System.Collections.Generic;

internal interface IDataSource<T>
{
	IList<T> GetVisibleRows();
}
