using Microsoft.JSInterop;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public interface IAppDataService
{
    public event Action? OnChange;

    public GeolocationPosition GetGeolocationPosition();

    public GeolocationPositionError GetGeolocationPositionError();

    public void OnPositionRecieved(GeolocationPosition position);

    public void OnPositionError(GeolocationPositionError positionError);
}
