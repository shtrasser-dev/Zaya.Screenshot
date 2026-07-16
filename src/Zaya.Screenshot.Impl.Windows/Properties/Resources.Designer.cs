using System.Resources;

namespace Zaya.Screenshot.Impl.Windows.Properties;

internal static class Resources
{
    private static readonly ResourceManager _rm =
        new("Zaya.Screenshot.Impl.Windows.Properties.Resources", typeof(Resources).Assembly);

    public static ResourceManager ResourceManager => _rm;
}
