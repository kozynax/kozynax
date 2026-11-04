using Avalonia;
using Avalonia.Themes.Fluent;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>Code-only Avalonia application (no XAML compile step needed).</summary>
    public sealed class App : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }
    }
}
