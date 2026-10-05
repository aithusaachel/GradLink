using Microsoft.AspNetCore.Components;

namespace GradLink.Client.Components;

public class RoleOption<TValue>
{
    public TValue Value { get; set; } = default!;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RenderFragment? Icon { get; set; }
    public string? IconBgColor { get; set; }
    public string? IconColor { get; set; }
}
