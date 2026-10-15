using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashToken(request.RefreshToken);

        var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Nevažeći refresh token");

        if (stored.RevokedAt is not null)
        {
            // Token koji je već rotiran ponovo je upotrebljen: moguća krađa.
            // Poništavamo sve sesije tog korisnika.
            if (stored.ReplacedByTokenHash is not null)
                await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId, cancellationToken);

            throw new UnauthorizedAccessException("Refresh token više nije važeći");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token je istekao");

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Korisnik ne postoji");

        var access = _tokenService.GenerateAccessToken(user);
        var newRefresh = _tokenService.GenerateRefreshToken();

        // Stari token je praćen (tracked) u istom DbContext-u, pa AddAsync ispod
        // snima obe promene jednim SaveChanges pozivom (atomski).
        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenHash = newRefresh.TokenHash;

        await _refreshTokenRepository.AddAsync(new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefresh.TokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = newRefresh.ExpiresAt
        }, cancellationToken);

        return new AuthResponse(access.Token, access.ExpiresAt, newRefresh.RawToken, newRefresh.ExpiresAt);
    }
}