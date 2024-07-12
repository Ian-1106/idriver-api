using System.Reflection;

namespace I3S_API.Lib
{
    public class AppConfig
    {
        public static IConfigurationRoot Config => LazyConfig.Value;

#if DEBUG
        private static readonly Lazy<IConfigurationRoot> LazyConfig = new Lazy<IConfigurationRoot>(() => new ConfigurationBuilder()
           .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
           .AddJsonFile("appsettings.json")
           .AddJsonFile("appsettings.Development.json")
           .Build());
#endif

#if RELEASE
        private static readonly Lazy<IConfigurationRoot> LazyConfig = new Lazy<IConfigurationRoot>(() => new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("appsettings.json")
            .Build());
#endif
    }
}
