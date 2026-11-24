namespace HubLink.Application.Configuration;

public class HubLinkOptions
{
    public const string SectionName = "HubLink";

    public string BaseAddress { get; set; } = "http://127.0.0.1:5080";
    public string BranchCode { get; set; } = "BR-001";
    public int TimeoutSeconds { get; set; } = 5;
    public bool UseTestEndpoint { get; set; } = false;
    public string? TestMode { get; set; } = "normal";
}
