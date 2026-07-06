namespace backend.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "RealEstateCrm";
    public string Audience { get; set; } = "RealEstateCrmClients";
    public string Secret { get; set; } = "dev-only-change-this-secret-to-at-least-32-chars";
    public int ExpiryMinutes { get; set; } = 720;
}
