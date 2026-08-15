using Avalonia.Controls;
using Avalonia.Media;
using DynamicUI24.Shared.Presentation;

namespace DynamicUI24.Avalonia.Presentation;

public sealed class SemanticIcon : PathIcon
{
    public void SetIcon(IIconRegistry registry, IconKey key)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Data = Geometry.Parse(registry.Resolve(key).SvgPathData);
    }
}
