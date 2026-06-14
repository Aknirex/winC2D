using winC2D.Core.Models;

namespace winC2D.Core.Services;

public interface IMigrationPreflightService
{
    Task<MigrationPreflightResult> ValidateAsync(MigrationRequest request, CancellationToken cancellationToken = default);
}
