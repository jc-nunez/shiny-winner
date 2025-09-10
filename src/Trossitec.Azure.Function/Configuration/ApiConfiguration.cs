namespace Trossitec.Azure.Function.Configuration;

public class ApiConfiguration
{
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

