using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

public record GeneratedRefreshToken(string RawToken, string TokenHash, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(User user);
    GeneratedRefreshToken GenerateRefreshToken();
    string HashToken(string rawToken);
}