namespace Trossitec.Azure.Function.Configuration;

public class StorageConfiguration
{
    public required string SourceStorageConnection { get; set; }
    public required string DestinationStorageConnection { get; set; }
    public required string TableStorageConnection { get; set; }
}

