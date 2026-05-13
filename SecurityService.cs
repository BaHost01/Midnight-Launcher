using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Midnight_Launcher;

public class SecurityService
{
    private static readonly string TokensPath = "Tokens.json";
    private static readonly string SecretKey = "MidnightLauncherSecretKeyForSecurity_CStudioss"; // In production, this should be more secure

    public static void GenerateTokens(string version)
    {
        try
        {
            var hwid = GetHWID();
            var windowsProc = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            var claims = new List<Claim>
            {
                new Claim("hwid", hwid),
                new Claim("windowsProc", windowsProc),
                new Claim("version", version),
                new Claim("timestamp", DateTime.UtcNow.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "MidnightLauncher",
                audience: "CStudioss",
                claims: claims,
                expires: DateTime.Now.AddYears(1),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            
            var payload = new { encrypted_data = tokenString };
            File.WriteAllText(TokensPath, JsonConvert.SerializeObject(payload, Formatting.Indented));
        }
        catch { }
    }

    private static string GetHWID()
    {
        // Simple cross-platform HWID logic
        return Environment.MachineName + "-" + Environment.UserName + "-" + Environment.ProcessorCount;
    }
}
