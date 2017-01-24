using System;
using System.Net;

internal struct AssetStoreResponse
{
	public int HttpStatusCode;

	public string HttpErrorMessage;

	public WebHeaderCollection HttpHeaders;

	public string data;

	public byte[] binData;

	public bool ok;

	public bool failed
	{
		get
		{
			return !this.ok;
		}
	}
}
