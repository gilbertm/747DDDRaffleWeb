using Microsoft.JSInterop;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public interface IAppDataService
{
    public event Action? OnChange;

    public GeolocationPosition GetGeolocationPosition();

    public GeolocationPositionError GetGeolocationPositionError();

    public void OnPositionRecieved(GeolocationPosition position);

    public void OnPositionError(GeolocationPositionError positionError);
}
