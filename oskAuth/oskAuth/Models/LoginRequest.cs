using JetBrains.Annotations;

namespace oskAuth.Models;

[UsedImplicitly]
internal sealed record LoginRequest(string? Login, string? Password);