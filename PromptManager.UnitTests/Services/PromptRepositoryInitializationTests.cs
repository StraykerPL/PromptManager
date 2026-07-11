using PromptManager.Services;

namespace PromptManager.UnitTests.Services
{
    public sealed class PromptRepositoryInitializationTests
    {
        [Fact]
        public void Constructor_CreatesDatabaseUnderAppDataDirectory()
        {
            // Arrange
            var directory = CreateTempDirectory();

            try
            {
                // Act
                using var repository = new PromptRepository(new TestAppDataPathProvider(directory));
                repository.GetPrompts();

                // Assert
                Assert.True(File.Exists(Path.Combine(directory, "prompts.db")));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void Constructor_WhenDatabaseIsCorrupt_CreatesBackupAndFreshDatabase()
        {
            // Arrange
            var directory = CreateTempDirectory();
            var databasePath = Path.Combine(directory, "prompts.db");
            File.WriteAllText(databasePath, "not a litedb database");

            try
            {
                // Act
                using var repository = new PromptRepository(new TestAppDataPathProvider(directory));
                repository.GetPrompts();

                // Assert
                Assert.True(File.Exists(databasePath));
                Assert.Single(Directory.GetFiles(directory, "prompts.corrupt-*.db"));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static string CreateTempDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"promptmanager-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private sealed class TestAppDataPathProvider(string directory) : IAppDataPathProvider
        {
            public string AppDataDirectory { get; } = directory;
        }
    }
}
