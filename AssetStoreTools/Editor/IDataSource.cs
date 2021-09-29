using System.Collections.Generic;

namespace AssetStoreTools
{
    internal interface IDataSource<T>
    {
        IList<T> GetVisibleRows();
    }

}