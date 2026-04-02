using Microsoft.EntityFrameworkCore;
using StyleScan.Backend.Data;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.Auth;
using StyleScan.Backend.Services.Interfaces;
using StyleScan.Backend.Utilities;

namespace StyleScan.Backend.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly StyleScanDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            StyleScanDbContext context,
            IConfiguration configuration,
            JwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _configuration = configuration;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Email == normalizedEmail);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Este email nao esta registrado.");
            }

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("A senha informada esta incorreta.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            SetRefreshToken(user);

            await _context.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token invalido ou expirado.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            SetRefreshToken(user);

            await _context.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var userExists = await _context.Users.AnyAsync(existingUser => existingUser.Email == normalizedEmail);
            if (userExists)
            {
                throw new InvalidOperationException("Ja existe um usuario cadastrado com este email.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = PasswordHasher.Hash(request.Password),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                DateOfBirth = request.DateOfBirth,
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            SetRefreshToken(user);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task LogoutAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException("Usuario invalido.");
            }

            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                return;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private AuthResponse BuildAuthResponse(User user)
        {
            var expirationMinutes = _configuration.GetSection("Jwt").GetValue<int?>("ExpirationMinutes") ?? 1440;

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = _jwtTokenGenerator.GenerateToken(user),
                RefreshToken = user.RefreshToken ?? string.Empty,
                ExpiresIn = expirationMinutes
            };
        }

        private static void SetRefreshToken(User user)
        {
            user.RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        }
    }
}
