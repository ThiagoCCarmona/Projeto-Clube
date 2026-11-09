using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Clube.Domain.Entities;

namespace Clube.Infrastructure.Security;

public interface ITokenService
{
    string GerarToken(Usuario usuario);
    ClaimsPrincipal? ValidarToken(string token);
}

public class TokenService : ITokenService
{
    private readonly string _secretKey;

    public TokenService(string secretKey)
    {
        _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
    }

    public string GerarToken(Usuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey.PadRight(32, '!'));

        var claims = new List<Claim>
        {
            new Claim("sub", usuario.Id.ToString()),
            new Claim("nome", usuario.Nome),
            new Claim("cpf", usuario.CPF),
            new Claim("email", usuario.Email),
            new Claim("role", usuario.Perfil.ToString()),
            new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
        };

        if (usuario.ClubeId.HasValue)
        {
            claims.Add(new Claim("clubeId", usuario.ClubeId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey.PadRight(32, '!'));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
