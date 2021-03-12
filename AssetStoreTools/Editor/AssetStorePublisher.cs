
internal class AssetStorePublisher
{
	public int PublisherID
	{
		get
		{
			return this.publisherId;
		}
	}

	public void Reset()
	{
		this.publisherId = -1;
		this.publisherName = string.Empty;
	}

	public AssetStorePublisher.Status mStatus;

	public int publisherId;

	public string publisherName;

	public enum Status
	{
		NotLoaded,
		Loading,
		New,
		Existing,
		Saving
	}
}
