using PromptManager.UI.Services;

namespace PromptManager.UnitTests.Services
{
    public sealed class AppDataPathProviderTests
    {
        [Fact]
        public void Windows_UsesApplicationDataDirectory()
        {
            var appData = Path.Combine(Path.GetTempPath(), "app-data");
            var provider = CreateProvider(DesktopPlatform.Windows, appData, "/home/test", null);

            var result = provider.AppDataDirectory;

            Assert.Equal(Path.Combine(appData, "PromptManager"), result);
        }

        [Fact]
        public void Linux_UsesAbsoluteXdgDataHome()
        {
            var xdgDataHome = Path.Combine(Path.GetTempPath(), "xdg-data");
            var provider = CreateProvider(DesktopPlatform.Linux, "/app-data", "/home/test", xdgDataHome);

            var result = provider.AppDataDirectory;

            Assert.Equal(Path.Combine(xdgDataHome, "PromptManager"), result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("relative/path")]
        public void Linux_FallsBackToLocalShareWhenXdgDataHomeIsUnavailable(string? xdgDataHome)
        {
            var home = Path.Combine(Path.GetTempPath(), "home");
            var provider = CreateProvider(DesktopPlatform.Linux, "/app-data", home, xdgDataHome);

            var result = provider.AppDataDirectory;

            Assert.Equal(Path.Combine(home, ".local", "share", "PromptManager"), result);
        }

        [Theory]
        [InlineData(DesktopPlatform.Windows)]
        [InlineData(DesktopPlatform.Linux)]
        public void MissingBaseDirectory_ThrowsUsefulError(DesktopPlatform platform)
        {
            var provider = CreateProvider(platform, string.Empty, string.Empty, null);

            var exception = Assert.Throws<InvalidOperationException>(() => provider.AppDataDirectory);

            Assert.Contains("absolute application-data directory", exception.Message);
        }

        private static AvaloniaAppDataPathProvider CreateProvider(
            DesktopPlatform platform,
            string appData,
            string home,
            string? xdgDataHome)
        {
            return new AvaloniaAppDataPathProvider(
                platform,
                folder => folder == Environment.SpecialFolder.ApplicationData ? appData : home,
                name => name == "XDG_DATA_HOME" ? xdgDataHome : null);
        }
    }
}
