using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StyleScan.Backend.Data;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.Avatar;
using StyleScan.Backend.Services.Interfaces;
using StyleScan.Backend.Services.Support;

namespace StyleScan.Backend.Services.Implementations
{
    public class AvatarService : IAvatarService
    {
        private readonly StyleScanDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AvatarService(
            StyleScanDbContext context,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<AvatarResponse> CreateAvatarAsync(Guid userId, CreateAvatarRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId)
                ?? throw new InvalidOperationException("Usuario nao encontrado para criar avatar.");
            var userAvatarCount = await _context.Avatars.CountAsync(avatar => avatar.UserId == userId);
            var plan = AccountPlanCatalog.Resolve(user.AccountPlan);
            if (userAvatarCount >= plan.Avatars)
            {
                throw new InvalidOperationException($"Seu plano {plan.Id} permite {plan.Avatars} avatar(es) ativo(s).");
            }

            var savedPhotoUrls = await SaveAvatarPhotosAsync(request.Photos);

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name.Trim(),
                ModelUrl = $"https://models.readyplayer.me/{Guid.NewGuid()}.glb",
                PhotoUrl = savedPhotoUrls.FirstOrDefault(),
                PhotoUrls = savedPhotoUrls,
                Gender = request.Gender.Trim(),
                BodyType = request.BodyType.Trim(),
                SkinTone = request.SkinTone.Trim(),
                Height = request.Height,
                Weight = request.Weight,
                Chest = request.Chest,
                Waist = request.Waist,
                Hips = request.Hips,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await ApplyInferredAvatarAttributesAsync(avatar);

            _context.Avatars.Add(avatar);
            await IncrementUsageAsync(userId, UsageMetricType.AvatarSlot, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return MapAvatarResponse(avatar);
        }

        public async Task<List<AvatarResponse>> GetUserAvatarsAsync(Guid userId)
        {
            return await _context.Avatars
                .Where(avatar => avatar.UserId == userId)
                .OrderByDescending(avatar => avatar.UpdatedAt)
                .Select(avatar => new AvatarResponse
                {
                    Id = avatar.Id,
                    UserId = avatar.UserId,
                    Name = avatar.Name,
                    ModelUrl = avatar.ModelUrl,
                    PhotoUrl = avatar.PhotoUrl,
                    PhotoUrls = avatar.PhotoUrls,
                    GeneratedAvatarImageUrl = avatar.GeneratedAvatarImageUrl,
                    Gender = avatar.Gender,
                    BodyType = avatar.BodyType,
                    SkinTone = avatar.SkinTone,
                    Height = avatar.Height,
                    Weight = avatar.Weight,
                    Chest = avatar.Chest,
                    Waist = avatar.Waist,
                    Hips = avatar.Hips,
                    CreatedAt = avatar.CreatedAt,
                    UpdatedAt = avatar.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<AvatarResponse?> GetAvatarByIdAsync(Guid avatarId)
        {
            var avatar = await _context.Avatars.FindAsync(avatarId);
            return avatar == null ? null : MapAvatarResponse(avatar);
        }

        public async Task<AvatarResponse?> UpdateAvatarAsync(Guid avatarId, UpdateAvatarRequest request)
        {
            var avatar = await _context.Avatars.FindAsync(avatarId);
            if (avatar == null)
            {
                return null;
            }

            avatar.Name = request.Name?.Trim() ?? avatar.Name;
            avatar.Gender = request.Gender?.Trim() ?? avatar.Gender;
            avatar.BodyType = request.BodyType?.Trim() ?? avatar.BodyType;
            avatar.SkinTone = request.SkinTone?.Trim() ?? avatar.SkinTone;

            if (request.Measurements != null)
            {
                avatar.Height = request.Measurements.Height;
                avatar.Weight = request.Measurements.Weight;
                avatar.Chest = request.Measurements.Chest;
                avatar.Waist = request.Measurements.Waist;
                avatar.Hips = request.Measurements.Hips;
            }

            avatar.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapAvatarResponse(avatar);
        }

        public async Task<AvatarResponse?> UpdateAvatarPhotosAsync(Guid avatarId, IReadOnlyCollection<IFormFile> photos)
        {
            var avatar = await _context.Avatars.FindAsync(avatarId);
            if (avatar == null)
            {
                return null;
            }

            var savedPhotoUrls = await SaveAvatarPhotosAsync(photos);
            avatar.PhotoUrls = savedPhotoUrls;
            avatar.PhotoUrl = savedPhotoUrls.FirstOrDefault();
            avatar.GeneratedAvatarImageUrl = null;
            await ApplyInferredAvatarAttributesAsync(avatar);
            avatar.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapAvatarResponse(avatar);
        }

        public async Task<AvatarResponse?> GenerateTwoDimensionalAvatarAsync(Guid avatarId)
        {
            var avatar = await _context.Avatars.FindAsync(avatarId);
            if (avatar == null)
            {
                return null;
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(existingUser => existingUser.Id == avatar.UserId)
                ?? throw new InvalidOperationException("Usuario nao encontrado para gerar avatar.");
            var plan = AccountPlanCatalog.Resolve(user.AccountPlan);
            await EnsureUsageLimitAsync(avatar.UserId, UsageMetricType.RealisticRender, plan.RealisticRendersPerMonth, "Seu plano atual atingiu o limite mensal de geracoes realistas.");

            avatar.GeneratedAvatarImageUrl = await TryGenerateWithOpenAiAsync(avatar)
                ?? await CreateGeneratedAvatarAssetAsync(avatar);
            avatar.UpdatedAt = DateTime.UtcNow;
            await IncrementUsageAsync(avatar.UserId, UsageMetricType.RealisticRender, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return MapAvatarResponse(avatar);
        }

        public async Task DeleteAvatarAsync(Guid avatarId)
        {
            var avatar = await _context.Avatars.FindAsync(avatarId);
            if (avatar == null)
            {
                return;
            }

            _context.Avatars.Remove(avatar);
            await _context.SaveChangesAsync();
        }

        private static AvatarResponse MapAvatarResponse(Avatar avatar)
        {
            return new AvatarResponse
            {
                Id = avatar.Id,
                UserId = avatar.UserId,
                Name = avatar.Name,
                ModelUrl = avatar.ModelUrl,
                PhotoUrl = avatar.PhotoUrl,
                PhotoUrls = avatar.PhotoUrls,
                GeneratedAvatarImageUrl = avatar.GeneratedAvatarImageUrl,
                Gender = avatar.Gender,
                BodyType = avatar.BodyType,
                SkinTone = avatar.SkinTone,
                Height = avatar.Height,
                Weight = avatar.Weight,
                Chest = avatar.Chest,
                Waist = avatar.Waist,
                Hips = avatar.Hips,
                CreatedAt = avatar.CreatedAt,
                UpdatedAt = avatar.UpdatedAt
            };
        }

        private async Task<List<string>> SaveAvatarPhotosAsync(IReadOnlyCollection<IFormFile>? photos)
        {
            if (photos == null || photos.Count == 0)
            {
                return new List<string>();
            }

            var avatarDirectory = EnsureAvatarUploadDirectory();
            var savedUrls = new List<string>();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            foreach (var photo in photos.Where(file => file != null && file.Length > 0))
            {
                var extension = Path.GetExtension(photo.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension.ToLowerInvariant()))
                {
                    throw new InvalidOperationException("Formato de imagem nao suportado.");
                }

                var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var filePath = Path.Combine(avatarDirectory, fileName);

                await using var stream = File.Create(filePath);
                await photo.CopyToAsync(stream);

                savedUrls.Add($"/uploads/avatars/{fileName}");
            }

            return savedUrls;
        }

        private async Task ApplyInferredAvatarAttributesAsync(Avatar avatar)
        {
            var inferred = await InferAvatarAttributesAsync(avatar);
            if (inferred == null)
            {
                return;
            }

            avatar.BodyType = inferred.BodyType;
            avatar.SkinTone = inferred.SkinTone;
        }

        private async Task<AvatarAttributeInference?> InferAvatarAttributesAsync(Avatar avatar)
        {
            var apiKey = _configuration["ExternalApis:OpenAI:ApiKey"]?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("your-openai-api-key", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var references = await LoadAvatarReferenceImagesAsync(avatar);
            if (references.Count == 0)
            {
                return null;
            }

            var baseUrl = _configuration["ExternalApis:OpenAI:BaseUrl"]?.Trim().TrimEnd('/') ?? "https://api.openai.com/v1";
            var model = _configuration["ExternalApis:OpenAI:AnalysisModel"]?.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gpt-4.1-mini";
            }

            var content = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "input_text",
                    ["text"] = BuildAvatarAnalysisPrompt()
                }
            };

            foreach (var reference in references)
            {
                content.Add(new JsonObject
                {
                    ["type"] = "input_image",
                    ["image_url"] = reference,
                    ["detail"] = "low"
                });
            }

            var payload = new JsonObject
            {
                ["model"] = model,
                ["input"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = content
                    }
                },
                ["text"] = new JsonObject
                {
                    ["format"] = new JsonObject
                    {
                        ["type"] = "json_schema",
                        ["name"] = "avatar_attributes",
                        ["strict"] = true,
                        ["schema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject
                            {
                                ["bodyType"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray("slim", "average", "athletic", "curvy")
                                },
                                ["skinTone"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray("light", "medium", "dark")
                                }
                            },
                            ["required"] = new JsonArray("bodyType", "skinTone")
                        }
                    }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/responses")
                {
                    Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
                };

                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(responseContent);
                var outputText = root?["output_text"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(outputText))
                {
                    return null;
                }

                var inferred = JsonNode.Parse(outputText);
                var bodyType = inferred?["bodyType"]?.GetValue<string>();
                var skinTone = inferred?["skinTone"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(bodyType) || string.IsNullOrWhiteSpace(skinTone))
                {
                    return null;
                }

                return new AvatarAttributeInference
                {
                    BodyType = bodyType,
                    SkinTone = skinTone
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> TryGenerateWithOpenAiAsync(Avatar avatar)
        {
            var apiKey = _configuration["ExternalApis:OpenAI:ApiKey"]?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("your-openai-api-key", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var references = await LoadAvatarReferenceImagesAsync(avatar);
            if (references.Count == 0)
            {
                return null;
            }

            var baseUrl = _configuration["ExternalApis:OpenAI:BaseUrl"]?.Trim().TrimEnd('/') ?? "https://api.openai.com/v1";
            var model = _configuration["ExternalApis:OpenAI:Model"]?.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gpt-image-1";
            }

            var payload = new JsonObject
            {
                ["model"] = model,
                ["prompt"] = BuildOpenAiPrompt(avatar),
                ["size"] = "1024x1536",
                ["quality"] = "medium",
                ["output_format"] = "png",
                ["background"] = "transparent",
                ["images"] = new JsonArray(references.Select(reference =>
                    (JsonNode)new JsonObject
                    {
                        ["image_url"] = reference
                    }).ToArray())
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/images/edits")
                {
                    Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
                };

                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(responseContent);
                var imageBase64 = root?["data"]?
                    .AsArray()
                    .FirstOrDefault()?["b64_json"]?
                    .GetValue<string>();

                if (string.IsNullOrWhiteSpace(imageBase64))
                {
                    return null;
                }

                return await SaveGeneratedPngAsync(avatar.Id, imageBase64);
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<string>> LoadAvatarReferenceImagesAsync(Avatar avatar)
        {
            var sources = avatar.PhotoUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).ToList() ?? new List<string>();
            if (sources.Count == 0 && !string.IsNullOrWhiteSpace(avatar.PhotoUrl))
            {
                sources.Add(avatar.PhotoUrl);
            }

            var references = new List<string>();
            foreach (var source in sources.Take(4))
            {
                if (string.IsNullOrWhiteSpace(source) || source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = source.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(GetWebRootPath(), relativePath);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var mimeType = extension switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    continue;
                }

                var bytes = await File.ReadAllBytesAsync(filePath);
                references.Add($"data:{mimeType};base64,{Convert.ToBase64String(bytes)}");
            }

            return references;
        }

        private static string BuildOpenAiPrompt(Avatar avatar)
        {
            var gender = string.IsNullOrWhiteSpace(avatar.Gender) ? "not specified" : avatar.Gender;
            var bodyType = string.IsNullOrWhiteSpace(avatar.BodyType) ? "balanced" : avatar.BodyType;
            var skinTone = string.IsNullOrWhiteSpace(avatar.SkinTone) ? "medium" : avatar.SkinTone;
            var hasWeight = avatar.Weight > 0;
            var hasMeasurements = avatar.Height > 0 || avatar.Weight > 0 || avatar.Chest > 0 || avatar.Waist > 0 || avatar.Hips > 0;

            return $"""
Create a highly faithful full-body 2D fashion avatar using the reference photos as the primary source of truth.

Identity fidelity rules:
- Preserve the same person from the photos with maximum fidelity.
- Match facial structure, jawline, cheek volume, nose shape, lips, eyes, eyebrows, ears, forehead, hairline, hairstyle, hair texture, hair volume, and hair color as closely as possible.
- Match skin tone, undertone, body proportions, shoulder width, torso length, waist position, hip width, arm thickness, leg length, neck shape, and overall silhouette closely to the references.
- Respect the body's actual volume and distribution shown in the photos, especially abdomen projection, waist softness, chest shape, upper arms, lower back, glutes, thighs, and calves.
- If there are multiple photos, use them to improve consistency and identity fidelity, not to average the person into a generic model.
- Use the front, close-up, back, and side references together to preserve shape from every angle.
- Preserve any visible asymmetry or distinctive traits that help the face and body remain recognizable.
- Preserve visible tattoos, tattoo placement, relative tattoo scale, beard shape, eyebrow shape, and hairline faithfully.
- Do not beautify, idealize, age up, age down, slim down, bulk up, feminize, masculinize, smooth out, or otherwise stylize the person away from the references.
- Do not generate a generic fashion model. The result must feel recognizably like the same individual from the photos.

Composition rules:
- Create a single full-body avatar facing forward in a neutral standing pose.
- The full head and full feet must be visible inside frame with comfortable top and bottom margins.
- Keep the avatar centered and fully contained in the image.
- Use a very light or transparent plain background only.
- No props, no furniture, no scenery, no text, no watermark, no collage, no split view.

Visual style rules:
- Premium editorial fashion app look.
- Semi-realistic 2D illustration or polished digital portrait, not 3D, not photorealistic, not cartoonish, and not emoji-like.
- Clean soft studio lighting.
- Natural anatomy only, with realistic hands, arms, legs, and feet.
- No duplicated limbs, no cropped head, no cropped feet, no deformed hands, no distorted face.
- Keep the face readable and proportionate.
- Avoid oversized head, tiny feet, toy-like proportions, rounded mascot body shapes, or simplified character design.
- Avoid creating an artificially athletic torso if the reference body is softer or more natural.
- Avoid flattening the stomach, narrowing the waist, widening the shoulders, or reducing the natural body volume seen in the photos.

Wardrobe rules:
- Dress the person in simple fitted neutral clothing suitable for a try-on base layer.
- Prefer a clean fitted tank or sleeveless base top and fitted shorts or shorts-like base bottom in neutral tones.
- Avoid bold patterns, coats, accessories, hats, bags, or dramatic styling.
- The clothing must not hide the body silhouette.
- Keep the base outfit close to the body but not compression-tight.

Avatar profile:
- Name: {avatar.Name}
- Gender expression: {gender}
- Body type: {bodyType}
- Skin tone: {skinTone}
- Height in cm: {avatar.Height.ToString(CultureInfo.InvariantCulture)}
- Weight in kg: {(hasWeight ? avatar.Weight.ToString(CultureInfo.InvariantCulture) : "not provided")}
- Chest in cm: {avatar.Chest.ToString(CultureInfo.InvariantCulture)}
- Waist in cm: {avatar.Waist.ToString(CultureInfo.InvariantCulture)}
- Hips in cm: {avatar.Hips.ToString(CultureInfo.InvariantCulture)}

Measurement guidance:
- Use these measurements as a corrective anchor so the avatar proportions stay coherent with the real person from the photos.
- The photos remain the main source of truth for shape, tattoos, face, and posture.
- If the numeric measurements and the photos differ slightly, prefer the photos for identity and the measurements for overall scale/proportion.

Critical fit guidance:
- Preserve a natural male body with mild abdominal projection if present.
- Preserve the actual waistline and side profile from the references.
- Preserve back width and softness from the rear view.
- Preserve arm thickness and leg volume from the full-body references.
- Do not turn this person into a lean fashion mannequin.

Output requirements:
- Single full-body 2D avatar image.
- Full body visible from head to toe.
- Prioritize fidelity over creativity.
- The result must look like the same person from the references, not a generic model.
- Transparent or very light plain background preferred.
- The output should feel premium, recognizable, balanced, and suitable as a try-on base in a fashion app.
- Use the provided measurements assertively to improve proportion accuracy.{(hasMeasurements ? string.Empty : " If measurements are missing, rely entirely on the photos.")}
""";
        }

        private static string BuildAvatarAnalysisPrompt()
        {
            return """
Analyze the reference photos and return JSON only.

Goal:
- Infer a respectful, moderate body-type label and a skin-tone label for a fashion avatar setup.
- Be conservative and kind.
- Do not exaggerate body size.
- Prefer "average" unless there is a clear reason to choose "slim", "athletic", or "curvy".
- Use "curvy" only when it is clearly visible, and never as an extreme interpretation.
- Infer skin tone from visible skin in a balanced way under normal lighting.

Allowed values:
- bodyType: slim, average, athletic, curvy
- skinTone: light, medium, dark

Return strict JSON with:
{
  "bodyType": "average",
  "skinTone": "medium"
}
""";
        }

        private async Task<string> SaveGeneratedPngAsync(Guid avatarId, string imageBase64)
        {
            var generatedDirectory = Path.Combine(EnsureAvatarUploadDirectory(), "generated");
            Directory.CreateDirectory(generatedDirectory);

            var fileName = $"avatar-2d-ai-{avatarId:N}.png";
            var filePath = Path.Combine(generatedDirectory, fileName);
            var bytes = Convert.FromBase64String(imageBase64);
            await File.WriteAllBytesAsync(filePath, bytes);

            return $"/uploads/avatars/generated/{fileName}";
        }

        private async Task<string> CreateGeneratedAvatarAssetAsync(Avatar avatar)
        {
            var generatedDirectory = Path.Combine(EnsureAvatarUploadDirectory(), "generated");
            Directory.CreateDirectory(generatedDirectory);

            var fileName = $"avatar-2d-{avatar.Id:N}.svg";
            var filePath = Path.Combine(generatedDirectory, fileName);

            var skin = ResolveSkinColor(avatar.SkinTone);
            var accent = ResolveAccentColor(avatar.Gender, avatar.BodyType);
            var outline = ResolveOutlineColor(accent);
            var cover = avatar.PhotoUrls.FirstOrDefault() ?? avatar.PhotoUrl;
            var safeName = SecurityElement.Escape(avatar.Name) ?? "Avatar";
            var safeBodyType = SecurityElement.Escape(avatar.BodyType) ?? "Personalizado";
            var coverImage = string.IsNullOrWhiteSpace(cover)
                ? string.Empty
                : $"<image href=\"..{cover}\" x=\"86\" y=\"64\" width=\"268\" height=\"248\" preserveAspectRatio=\"xMidYMid slice\" clip-path=\"url(#portraitMask)\" opacity=\"0.82\" />";

            var svg = $"""
<svg width="440" height="640" viewBox="0 0 440 640" fill="none" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bg" x1="40" y1="24" x2="400" y2="612" gradientUnits="userSpaceOnUse">
      <stop stop-color="#F7FBFC"/>
      <stop offset="1" stop-color="#E7F1F4"/>
    </linearGradient>
    <linearGradient id="skin" x1="220" y1="160" x2="220" y2="492" gradientUnits="userSpaceOnUse">
      <stop stop-color="{skin}"/>
      <stop offset="1" stop-color="#D9B08A"/>
    </linearGradient>
    <linearGradient id="accent" x1="90" y1="110" x2="344" y2="522" gradientUnits="userSpaceOnUse">
      <stop stop-color="{accent}" stop-opacity="0.96"/>
      <stop offset="1" stop-color="{outline}" stop-opacity="0.92"/>
    </linearGradient>
    <clipPath id="portraitMask">
      <rect x="86" y="64" width="268" height="248" rx="42" />
    </clipPath>
  </defs>
  <rect x="18" y="18" width="404" height="604" rx="36" fill="url(#bg)"/>
  <rect x="38" y="38" width="364" height="564" rx="30" fill="#FFFFFF" fill-opacity="0.72" stroke="#D7E5EA"/>
  <rect x="86" y="64" width="268" height="248" rx="42" fill="#E8EFF2"/>
  {coverImage}
  <circle cx="220" cy="390" r="118" fill="url(#accent)" opacity="0.18"/>
  <ellipse cx="220" cy="392" rx="104" ry="124" fill="url(#skin)"/>
  <circle cx="220" cy="232" r="54" fill="url(#skin)"/>
  <rect x="195" y="274" width="50" height="30" rx="15" fill="url(#skin)"/>
  <path d="M145 324C145 300.804 163.804 282 187 282H253C276.196 282 295 300.804 295 324V366C295 389.196 276.196 408 253 408H187C163.804 408 145 389.196 145 366V324Z" fill="#F6FBFC" fill-opacity="0.54"/>
  <path d="M164 414C164 399.641 175.641 388 190 388H250C264.359 388 276 399.641 276 414V520C276 538.778 260.778 554 242 554H198C179.222 554 164 538.778 164 520V414Z" fill="url(#accent)" fill-opacity="0.78"/>
  <rect x="126" y="430" width="52" height="120" rx="26" fill="url(#skin)"/>
  <rect x="262" y="430" width="52" height="120" rx="26" fill="url(#skin)"/>
  <rect x="176" y="548" width="34" height="18" rx="9" fill="{outline}" fill-opacity="0.78"/>
  <rect x="230" y="548" width="34" height="18" rx="9" fill="{outline}" fill-opacity="0.78"/>
  <text x="54" y="572" fill="#203540" font-family="Segoe UI, sans-serif" font-size="28" font-weight="700">{safeName}</text>
  <text x="54" y="602" fill="#5C717B" font-family="Segoe UI, sans-serif" font-size="18">Avatar 2D • {safeBodyType}</text>
</svg>
""";

            await File.WriteAllTextAsync(filePath, svg);
            return $"/uploads/avatars/generated/{fileName}";
        }

        private string EnsureAvatarUploadDirectory()
        {
            var avatarDirectory = Path.Combine(GetWebRootPath(), "uploads", "avatars");
            Directory.CreateDirectory(avatarDirectory);
            return avatarDirectory;
        }

        private string GetWebRootPath()
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            return webRootPath;
        }

        private static string ResolveSkinColor(string skinTone)
        {
            var tone = (skinTone ?? string.Empty).ToLowerInvariant();
            if (tone.Contains("dark") || tone.Contains("esc")) return "#7B5239";
            if (tone.Contains("medium") || tone.Contains("med") || tone.Contains("mor")) return "#C79269";
            return "#F1C9A6";
        }

        private static string ResolveAccentColor(string gender, string bodyType)
        {
            var combined = $"{gender} {bodyType}".ToLowerInvariant();
            if (combined.Contains("athletic") || combined.Contains("atlet")) return "#2F5F68";
            if (combined.Contains("curvy") || combined.Contains("ampulheta")) return "#9B5E49";
            if (combined.Contains("male") || combined.Contains("masc")) return "#2B4452";
            return "#6E8776";
        }

        private static string ResolveOutlineColor(string accent)
        {
            return accent switch
            {
                "#2F5F68" => "#17353D",
                "#9B5E49" => "#6D4032",
                "#2B4452" => "#172833",
                _ => "#44584A"
            };
        }

        private sealed class AvatarAttributeInference
        {
            public string BodyType { get; init; } = "average";
            public string SkinTone { get; init; } = "medium";
        }

        private async Task EnsureUsageLimitAsync(Guid userId, string metricType, int limit, string message)
        {
            var now = DateTime.UtcNow;
            var periodKey = AccountPlanCatalog.GetPeriodKeyForMetric(metricType, now);
            var currentUsage = await _context.UserUsageRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.UserId == userId && record.MetricType == metricType && record.PeriodKey == periodKey);

            if ((currentUsage?.Used ?? 0) >= limit)
            {
                throw new InvalidOperationException(message);
            }
        }

        private async Task IncrementUsageAsync(Guid userId, string metricType, DateTime now)
        {
            var periodKey = AccountPlanCatalog.GetPeriodKeyForMetric(metricType, now);
            var record = await _context.UserUsageRecords
                .FirstOrDefaultAsync(existingRecord => existingRecord.UserId == userId && existingRecord.MetricType == metricType && existingRecord.PeriodKey == periodKey);

            if (record == null)
            {
                record = new UserUsageRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MetricType = metricType,
                    PeriodKey = periodKey,
                    Used = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.UserUsageRecords.Add(record);
            }

            record.Used += 1;
            record.UpdatedAt = now;
        }
    }
}
