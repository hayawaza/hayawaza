using System.Windows;
using System.Windows.Controls;

namespace ShortcutOverlay;

public class ShortcutItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ShortcutTemplate { get; set; }
    public DataTemplate? SeparatorTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is SeparatorItem ? SeparatorTemplate : ShortcutTemplate;
}
