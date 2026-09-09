using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using oskAuth;
using oskAuth.Models;

var builder = WebApplication.CreateBuilder(args);
var signingKey = AuthOptions.GetSymmetricSecurityKey();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Set ConnectionStrings:Postgres.")));

builder.Services.AddScoped<PasswordHasher<AppUser>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtConfiguration.Configure);
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
}
else
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<AuthVersionMiddleware>();
app.UseAuthorization();

app.MapPost("/auth/register", async (RegisterRequest request, AppDbContext db,
    PasswordHasher<AppUser> hasher, CancellationToken ct) =>
{
    request = request with { Login = request.Login?.Trim().ToLowerInvariant() };
    var errors = request.Validate();
    if (errors.Length > 0)
    {
        return Results.BadRequest(new { error = errors });
    }

    var user = new AppUser
    {
        Login = request.Login!,
        DisplayName = request.DisplayName!.Trim(),
    };
    user.PasswordHash = hasher.HashPassword(user, request.Password!);
    db.Users.Add(user);
    try
    {
        await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
    {
        return Results.Conflict(new { error = "Такой логин уже используется" });
    }

    return Results.Created("/whoami", new UserResponse(user.Id, user.Login, user.DisplayName));
});

app.MapPost("/auth/login", async (LoginRequest request, AppDbContext db,
    PasswordHasher<AppUser> hasher, CancellationToken ct) =>
{
    var login = request.Login?.Trim().ToLowerInvariant();
    var password = request.Password;

    if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || login.Length > 100 || password.Length > 128)
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.SingleOrDefaultAsync(u => u.Login == login, ct);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
    if (verification == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    if (verification == PasswordVerificationResult.SuccessRehashNeeded)
    {
        user.PasswordHash = hasher.HashPassword(user, password);
        await db.SaveChangesAsync(ct);
    }

    var expires = DateTime.UtcNow.AddHours(1);
    var token = new JwtSecurityToken(AuthOptions.Issuer, AuthOptions.Audience,
        [
            new Claim("sub", user.Id.ToString()),
            new Claim("auth_version", user.AuthVersion.ToString())
        ],
        expires: expires,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
    return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), expiresAt = expires });
});

app.MapGet("/whoami", (HttpContext context) =>
{
    var user = (AppUser)context.Items[typeof(AppUser)]!;
    return new UserResponse(user.Id, user.Login, user.DisplayName);
}).RequireAuthorization();

app.MapPost("/auth/logout", async (HttpContext context, AppDbContext db, CancellationToken ct) =>
{
    var user = (AppUser)context.Items[typeof(AppUser)]!;
    await db.Users.Where(u => u.Id == user.Id && u.AuthVersion == user.AuthVersion)
        .ExecuteUpdateAsync(update => update.SetProperty(u => u.AuthVersion, u => u.AuthVersion + 1), ct);
    return Results.NoContent();
}).RequireAuthorization();

app.Run();