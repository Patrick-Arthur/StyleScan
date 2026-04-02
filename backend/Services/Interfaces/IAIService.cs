using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace StyleScan.Backend.Services.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateAvatarModelUrlAsync(IFormFile photo, string gender, string bodyType, string skinTone);
        Task<string> GenerateLookRecommendationAsync(string avatarId, string occasion, string style, string season, string[] colorPreferences, decimal? budget);
        // Outros métodos de IA conforme necessário
    }
}
