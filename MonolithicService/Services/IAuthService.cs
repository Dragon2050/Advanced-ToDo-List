using MonolithicService.Models.DTOs;

namespace MonolithicService.Services
{
    public interface IAuthService
    {
        Task<TokenDto> RegisterAsync(RegisterDto registerDto);
        Task<TokenDto> LoginAsync(LoginDto loginDto);
        Task<TokenDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string refreshToken);
    }
}