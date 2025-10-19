using Microsoft.EntityFrameworkCore;
using MonolithicService.Configuration;
using MonolithicService.Data;
using MonolithicService.Models;
using MonolithicService.Models.DTOs;
using BCrypt.Net;

namespace MonolithicService.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public AuthService(ApplicationDbContext context, IJwtService jwtService, JwtSettings jwtSettings)
        {
            _context = context;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings;
        }

        public async Task<TokenDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Generate tokens
            var tokens = _jwtService.GenerateTokens(user);
            
            // Update user with refresh token
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = tokens.RefreshTokenExpiry;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Update the token DTO with the saved user ID
            tokens.User.Id = user.Id;

            return tokens;
        }

        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Generate tokens
            var tokens = _jwtService.GenerateTokens(user);

            // Update user with new refresh token
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = tokens.RefreshTokenExpiry;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return tokens;
        }

        public async Task<TokenDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            // Find user by refresh token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Generate new tokens
            var tokens = _jwtService.GenerateTokens(user);

            // Update user with new refresh token
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = tokens.RefreshTokenExpiry;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return tokens;
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null)
            {
                return false;
            }

            // Clear refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}