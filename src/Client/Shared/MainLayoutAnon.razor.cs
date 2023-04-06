using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public partial class MainLayoutAnon
{
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
    [Parameter]
    public EventCallback OnDarkModeToggle { get; set; }
    [Parameter]
    public EventCallback<bool> OnRightToLeftToggle { get; set; }

    [Inject]
    protected ILoggerFactory LoggerFactory { get; set; } = default!;

    private ILogger Logger { get; set; } = default!;

    private bool _rightToLeft = default!;
    public async Task ToggleDarkMode()
    {
        await OnDarkModeToggle.InvokeAsync();
    }

    private async Task RightToLeftToggle()
    {
        bool isRtl = true;

        _rightToLeft = isRtl;

        await OnRightToLeftToggle.InvokeAsync(isRtl);
    }

    private string _currentUrl = default!;
    private Uri _uri = default!;
    private string _cssClasses = default!;

    protected override async Task OnInitializedAsync()
    {
        Logger = LoggerFactory.CreateLogger($"RaffleConsoleWriteLine - {nameof(MainLayoutAnon)}");

        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name}");
                }
            }
        }

        if (AppDataService != default)
        {
            AppDataService.ShowValuesAppDto();

            if (AppDataService.City == default)
            {
                await AppDataService.UpdateLocationAsync();
            }
        }
    }

    protected override void OnParametersSet()
    {
        _currentUrl = Navigation.Uri;

        _uri = new Uri(_currentUrl);

        if (_uri is { } && _uri.Segments.Count() > 1)
        {

            _cssClasses = _uri.Segments.ToList().ElementAt(1).ToString().ToLower();

            _cssClasses = _cssClasses.Replace("/", "");

        }
        else
        {
            _cssClasses = "home";
        }
    }
}