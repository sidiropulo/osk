using JetBrains.Annotations;

namespace oskAuth.Models;

[UsedImplicitly]
internal sealed record UserResponse(Guid Id, string Login, string DisplayName);