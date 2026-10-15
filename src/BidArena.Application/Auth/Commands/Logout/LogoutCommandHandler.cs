using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _tokenService;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository, IJwtTokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashToken(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        // Idempotentno: nepoznat ili već poništen token nije greška.
        if (stored is null || stored.RevokedAt is not null)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
    }
}