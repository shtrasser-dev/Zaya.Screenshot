using System.Resources;

namespace Zaya.Screenshot.Properties;

internal static class Resources
{
    private static readonly ResourceManager _rm =
        new("Zaya.Screenshot.Properties.Resources", typeof(Resources).Assembly);

    public static ResourceManager ResourceManager => _rm;
}
