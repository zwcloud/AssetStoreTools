using System;
using System.Net;

namespace AssetStoreTools
{
    internal class AssetStoreWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            return (HttpWebRequest)base.GetWebRequest(address);
        }
    }

}