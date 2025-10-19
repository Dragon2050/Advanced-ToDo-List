using MonolithicService.Models;
using MonolithicService.Models.DTOs;

namespace MonolithicService.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        bool ValidateToken(string token);
        string? GetUserIdFromToken(string token);
        TokenDto GenerateTokens(User user);
    }
}