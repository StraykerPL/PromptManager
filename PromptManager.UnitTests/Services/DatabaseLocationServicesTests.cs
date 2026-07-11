using PromptManager.Models;
using PromptManager.Services;
using PromptManager.UI.Services;

namespace PromptManager.UnitTests.Services
{
    public sealed class DatabaseLocationServicesTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"PromptManager-location-{Guid.NewGuid():N}");

        public DatabaseLocationServicesTests() => Directory.CreateDirectory(root);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CustomDirectoryAlwaysWins(bool debug)
        {
            var custom = NewDirectory("custom");
            var result = new DatabaseLocationResolver(NewDirectory("system"), NewDirectory("exe"), debug).Resolve(custom);
            Assert.Equal(custom, result.Directory);
            Assert.Equal(DatabaseLocationKind.Custom, result.Kind);
        }

        [Fact]
        public void DebugDefaultUsesInjectedExecutableDirectory()
        {
            var executable = NewDirectory("exe");
            var result = new DatabaseLocationResolver(NewDirectory("system"), executable, true).Resolve(null);
            Assert.Equal(executable, result.Directory);
            Assert.Equal(DatabaseLocationKind.DebugDefault, result.Kind);
        }

        [Fact]
        public void ReleaseDefaultUsesInjectedSystemDirectory()
        {
            var system = NewDirectory("system");
            var result = new DatabaseLocationResolver(system, NewDirectory("exe"), false).Resolve(null);
            Assert.Equal(system, result.Directory);
            Assert.Equal(DatabaseLocationKind.SystemDefault, result.Kind);
        }

        [Fact]
        public void SettingsRoundTripAndRestoreRemovesOverride()
        {
            var system = NewDirectory("settings");
            var custom = NewDirectory("custom");
            var store = new DatabaseLocationSettingsStore(system);
            store.Save(custom);
            Assert.Equal(custom, store.Load(out var warning));
            Assert.Null(warning);

            store.Save(null);
            Assert.Null(store.Load(out warning));
            Assert.DoesNotContain("databaseDirectory", File.ReadAllText(Path.Combine(system, "settings.json")));
            Assert.True(Directory.Exists(custom));
        }

        [Theory]
        [InlineData("{ broken")]
        [InlineData("{\"databaseDirectory\":\"relative/path\"}")]
        [InlineData("{\"databaseDirectory\":\"   \"}")]
        public void InvalidSettingsFallBack(string contents)
        {
            var system = NewDirectory("settings");
            File.WriteAllText(Path.Combine(system, "settings.json"), contents);
            var result = new DatabaseLocationSettingsStore(system).Load(out var warning);
            Assert.Null(result);
            if (!contents.Contains("   ")) Assert.NotNull(warning);
        }

        [Fact]
        public void SelectingCurrentDirectoryIsDetectedAsNoOp()
        {
            var directory = NewDirectory("current");
            var service = new DatabaseLocationChangeService(new DatabaseLocationSettingsStore(NewDirectory("settings")));
            Assert.True(service.IsSameLocation(directory, directory + Path.DirectorySeparatorChar));
        }

        [Theory]
        [InlineData(NewDatabaseAction.CopyCurrent, 1)]
        [InlineData(NewDatabaseAction.StartEmpty, 0)]
        public void NewDestinationSupportsCopyAndEmpty(NewDatabaseAction action, int expectedPrompts)
        {
            var source = NewDirectory("source-" + action);
            var destination = NewDirectory("destination-" + action);
            using var repository = CreateRepositoryWithPrompt(source);
            var service = new DatabaseLocationChangeService(new DatabaseLocationSettingsStore(NewDirectory("settings-" + action)));
            service.Apply(destination, repository, action, ExistingDatabaseAction.UseExisting);
            using var opened = new PromptRepository(destination);
            Assert.Equal(expectedPrompts, opened.GetPrompts().Count);
        }

        [Fact]
        public void ExistingDestinationCanBeUsedWithoutReplacement()
        {
            var source = NewDirectory("source-use");
            var destination = NewDirectory("destination-use");
            using var current = CreateRepositoryWithPrompt(source, "Current");
            using (CreateRepositoryWithPrompt(destination, "Existing")) { }
            var service = new DatabaseLocationChangeService(new DatabaseLocationSettingsStore(NewDirectory("settings-use")));
            service.Apply(destination, current, NewDatabaseAction.StartEmpty, ExistingDatabaseAction.UseExisting);
            using var opened = new PromptRepository(destination);
            Assert.Equal("Existing", Assert.Single(opened.GetPrompts()).Name);
        }

        [Fact]
        public void ExistingDestinationReplacementCreatesBackup()
        {
            var source = NewDirectory("source-replace");
            var destination = NewDirectory("destination-replace");
            using var current = CreateRepositoryWithPrompt(source, "Current");
            using (CreateRepositoryWithPrompt(destination, "Existing")) { }
            var service = new DatabaseLocationChangeService(new DatabaseLocationSettingsStore(NewDirectory("settings-replace")));
            var result = service.Apply(destination, current, NewDatabaseAction.StartEmpty, ExistingDatabaseAction.ReplaceCurrent);
            Assert.True(File.Exists(result.BackupPath));
            using var opened = new PromptRepository(destination);
            Assert.Equal("Current", Assert.Single(opened.GetPrompts()).Name);
        }

        private PromptRepository CreateRepositoryWithPrompt(string directory, string name = "Prompt")
        {
            var repository = new PromptRepository(directory);
            repository.SavePrompt(new PromptItem { Name = name, Content = "Text" });
            return repository;
        }

        private string NewDirectory(string name)
        {
            var path = Path.Combine(root, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose() => Directory.Delete(root, true);
    }
}
