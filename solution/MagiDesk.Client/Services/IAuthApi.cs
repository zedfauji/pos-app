using Refit;
using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Client.Services.Dtos;
using System.Threading.Tasks;
using System.Threading;

namespace MagiDesk.Client.Services;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<IApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);

    [Post("/api/auth/validate")]
    Task<IApiResponse<object>> ValidateCredentialsAsync([Body] ValidateCredentialsRequest request, CancellationToken ct = default);
}
