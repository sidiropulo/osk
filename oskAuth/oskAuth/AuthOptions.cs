using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace oskAuth;

public static class AuthOptions
{
    public const string Issuer = "oskAuth";
    public const string Audience = "oskAuth-client";
    private const string SecretKey = "mysupersecret_secretsecretsecretkey!123";

    public static SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
    }
}