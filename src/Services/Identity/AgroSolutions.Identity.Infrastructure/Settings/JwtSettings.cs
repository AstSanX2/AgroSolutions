namespace AgroSolutions.Identity.Infrastructure.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AgroSolutions";
    public string Audience { get; set; } = "AgroSolutions";
    public int ExpirationInHours { get; set; } = 24;
}
