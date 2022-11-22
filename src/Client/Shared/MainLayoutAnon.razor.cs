using Microsoft.AspNetCore.Components;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public partial class MainLayoutAnon
{
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
    [Parameter]
    public EventCallback OnDarkModeToggle { get; set; }
    [Parameter]
    public EventCallback<bool> OnRightToLeftToggle { get; set; }

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

}