using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using StyleScan.Backend.Data;
using StyleScan.Backend.Services.Interfaces;
using StyleScan.Backend.Services.Implementations;
using StyleScan.Backend.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using Microsoft.AspNetCore.Http;
using StyleScan.Backend.Models.Domain;

var builder = WebApplication.CreateBuilder(args);
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

builder.Services.AddDbContext<StyleScanDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings.GetValue<string>("SecretKey")
    ?? throw new InvalidOperationException("Jwt:SecretKey nao configurado.");
var issuer = jwtSettings.GetValue<string>("Issuer") ?? string.Empty;
var audience = jwtSettings.GetValue<string>("Audience") ?? string.Empty;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAvatarService, AvatarService>();
builder.Services.AddScoped<ILooksService, LooksService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<JwtTokenGenerator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StyleScan API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();
app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StyleScanDbContext>();
    dbContext.Database.ExecuteSqlRaw(
        """
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'Users'
                  AND column_name = 'DateOfBirth'
                  AND data_type <> 'date'
            ) THEN
                ALTER TABLE "Users"
                ALTER COLUMN "DateOfBirth" TYPE date
                USING "DateOfBirth"::date;
            END IF;
        END
        $$;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Avatars"
        ADD COLUMN IF NOT EXISTS "PhotoUrl" character varying(500);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Avatars"
        ADD COLUMN IF NOT EXISTS "PhotoUrls" text[] NOT NULL DEFAULT ARRAY[]::text[];
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Avatars"
        ADD COLUMN IF NOT EXISTS "GeneratedAvatarImageUrl" character varying(500);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "AccountPlan" character varying(32) NOT NULL DEFAULT 'free';
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "PublicProfileSlug" character varying(160);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        UPDATE "Users"
        SET "AccountPlan" = 'free'
        WHERE "AccountPlan" IS NULL OR "AccountPlan" = '';
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "SubscriptionStatus" character varying(32) NOT NULL DEFAULT 'inactive';
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "SubscriptionProvider" character varying(64);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "SubscriptionReference" character varying(120);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "PendingAccountPlan" character varying(32);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "SubscriptionStartedAt" timestamp with time zone;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "SubscriptionCurrentPeriodEndsAt" timestamp with time zone;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "LastPaymentId" character varying(120);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "LastPaymentStatus" character varying(32);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "LastPaymentStatusDetail" character varying(120);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "LastPaymentUpdatedAt" timestamp with time zone;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Users"
        ADD COLUMN IF NOT EXISTS "LastWebhookReceivedAt" timestamp with time zone;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "HeroImageUrl" character varying(500);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "Note" character varying(220);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "OccasionTags" text[] NOT NULL DEFAULT ARRAY[]::text[];
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "HeroPreviewMode" character varying(32);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "IsPublished" boolean NOT NULL DEFAULT FALSE;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "PublishedAt" timestamp with time zone;
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        ALTER TABLE "Looks"
        ADD COLUMN IF NOT EXISTS "ShareSlug" character varying(160);
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "UserPreference" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "PreferenceKey" character varying(120) NOT NULL,
            "PreferenceValue" character varying(255) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_UserPreference" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_UserPreference_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserPreference_UserId_PreferenceKey_PreferenceValue"
        ON "UserPreference" ("UserId", "PreferenceKey", "PreferenceValue");
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "UserUsageRecords" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "MetricType" character varying(64) NOT NULL,
            "PeriodKey" character varying(32) NOT NULL,
            "Used" integer NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_UserUsageRecords" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_UserUsageRecords_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserUsageRecords_UserId_MetricType_PeriodKey"
        ON "UserUsageRecords" ("UserId", "MetricType", "PeriodKey");
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "TryOnPreviewHistories" (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "AvatarId" uuid NOT NULL,
            "Style" character varying(120) NOT NULL,
            "Occasion" character varying(120) NOT NULL,
            "BoardId" character varying(64),
            "Mode" character varying(32) NOT NULL,
            "ImageUrl" character varying(500) NOT NULL,
            "UsedAi" boolean NOT NULL,
            "ProductNames" text[] NOT NULL DEFAULT ARRAY[]::text[],
            "ProductCategories" text[] NOT NULL DEFAULT ARRAY[]::text[],
            "CreatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_TryOnPreviewHistories" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_TryOnPreviewHistories_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_TryOnPreviewHistories_Avatars_AvatarId" FOREIGN KEY ("AvatarId") REFERENCES "Avatars" ("Id") ON DELETE CASCADE
        );
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        CREATE INDEX IF NOT EXISTS "IX_TryOnPreviewHistories_UserId_AvatarId_CreatedAt"
        ON "TryOnPreviewHistories" ("UserId", "AvatarId", "CreatedAt");
        """);

    dbContext.Database.ExecuteSqlRaw(
        """
        UPDATE "Avatars"
        SET "PhotoUrls" = ARRAY["PhotoUrl"]
        WHERE ("PhotoUrl" IS NOT NULL AND "PhotoUrl" <> '')
          AND (cardinality("PhotoUrls") = 0);
        """);

    await EnsureCatalogSeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => "API is healthy!").WithTags("Health");
app.Run();

static async Task EnsureCatalogSeedAsync(StyleScanDbContext dbContext)
{
    if (await dbContext.Stores.AnyAsync() || await dbContext.Clothings.AnyAsync())
    {
        return;
    }

    var now = DateTime.UtcNow;

    var store = new Store
    {
        Id = Guid.NewGuid(),
        Name = "StyleScan Select",
        Description = "Curadoria inicial do MVP para geracao de looks.",
        LogoUrl = "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?auto=format&fit=crop&w=200&q=80",
        WebsiteUrl = "https://stylescan.local/store",
        ContactEmail = "curadoria@stylescan.local",
        CreatedAt = now,
        UpdatedAt = now
    };

    var clothings = new[]
    {
        CreateClothing(store.Id, "Essential White Tee", "top", 119m, "white", "Algodao premium para base casual.", "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?auto=format&fit=crop&w=900&q=80", "TEE-001", 4.8, now),
        CreateClothing(store.Id, "Classic Black Shirt", "top", 189m, "black", "Camisa versatil para office e dinner.", "https://images.unsplash.com/photo-1603252109303-2751441dd157?auto=format&fit=crop&w=900&q=80", "TOP-002", 4.7, now),
        CreateClothing(store.Id, "Relaxed Denim", "bottom", 219m, "blue", "Jeans de corte reto para looks casuais.", "https://images.unsplash.com/photo-1541099649105-f69ad21f3246?auto=format&fit=crop&w=900&q=80", "BOT-003", 4.6, now),
        CreateClothing(store.Id, "Tailored Trousers", "bottom", 259m, "beige", "Calca de alfaiataria com acabamento clean.", "https://images.unsplash.com/photo-1506629905607-d9dc772b3b53?auto=format&fit=crop&w=900&q=80", "BOT-004", 4.9, now),
        CreateClothing(store.Id, "City Sneakers", "shoes", 299m, "white", "Tenis urbano para compor bases modernas.", "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=900&q=80", "SHO-005", 4.8, now),
        CreateClothing(store.Id, "Leather Loafers", "shoes", 349m, "black", "Sapato sofisticado para contextos formais.", "https://images.unsplash.com/photo-1614252235316-8c857d38b5f4?auto=format&fit=crop&w=900&q=80", "SHO-006", 4.7, now),
        CreateClothing(store.Id, "Midnight Slip Dress", "dress", 329m, "black", "Vestido elegante para eventos noturnos.", "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?auto=format&fit=crop&w=900&q=80", "DRS-007", 4.8, now),
        CreateClothing(store.Id, "Statement Earrings", "accessory", 99m, "gold", "Acessorio de destaque para finalizar o look.", "https://images.unsplash.com/photo-1617038220319-276d3cfab638?auto=format&fit=crop&w=900&q=80", "ACC-008", 4.5, now)
    };

    dbContext.Stores.Add(store);
    dbContext.Clothings.AddRange(clothings);
    await dbContext.SaveChangesAsync();
}

static Clothing CreateClothing(
    Guid storeId,
    string name,
    string category,
    decimal price,
    string color,
    string description,
    string imageUrl,
    string sku,
    double rating,
    DateTime now)
{
    return new Clothing
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = description,
        Category = category,
        Price = price,
        Color = color,
        Material = "Blend",
        Sizes = new List<string> { "PP", "P", "M", "G" },
        ImageUrl = imageUrl,
        ProductUrl = $"https://stylescan.local/product/{sku.ToLowerInvariant()}",
        StoreId = storeId,
        Rating = rating,
        ReviewsCount = 32,
        InStock = true,
        Sku = sku,
        CreatedAt = now,
        UpdatedAt = now
    };
}
