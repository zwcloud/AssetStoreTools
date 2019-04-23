using System;

namespace AssetStoreTools
{
    internal class JSONParseException : Exception
    {
        public JSONParseException(string msg) : base(msg)
        {
        }
    }

}