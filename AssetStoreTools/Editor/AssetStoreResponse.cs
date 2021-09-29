using System.Net;

namespace AssetStoreTools
{
    internal struct AssetStoreResponse
    {
        public bool failed
        {
            get
            {
                return !this.ok;
            }
        }

        public int HttpStatusCode;

        public string HttpErrorMessage;

        public WebHeaderCollection HttpHeaders;

        public string data;

        public byte[] binData;

        public bool ok;
    }

}