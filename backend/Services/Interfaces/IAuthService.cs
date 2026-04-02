using System.Threading.Tasks;
using StyleScan.Backend.Models.DTOs.Auth;

namespace StyleScan.Backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string userId);
    }
}
