using System.Linq.Expressions;
using LiteDB;
using Moq;
using PromptManager.Models;
using PromptManager.Services;

namespace PromptManager.UnitTests.Services
{
    public sealed class PromptRepositoryTests
    {
        [Fact]
        public void GetPrompts_NormalizesTagsAndOrdersByName()
        {
            // Arrange
            var promptCollection = new Mock<ILiteCollection<PromptItem>>();
            promptCollection.Setup(collection => collection.FindAll()).Returns([
                new PromptItem { Id = 2, Name = "Beta", Description = null!, Content = null!, Tags = [" two ", "One", "one"] },
                new PromptItem { Id = 1, Name = "Alpha", Tags = [" alpha "] }
            ]);
            var repository = CreateRepository(promptCollection);

            // Act
            var result = repository.GetPrompts();

            // Assert
            Assert.Equal([1, 2], result.Select(prompt => prompt.Id));
            Assert.Equal(["One", "two"], result[1].Tags);
            Assert.Equal(string.Empty, result[1].Description);
            Assert.Equal(string.Empty, result[1].Content);
        }

        [Fact]
        public void GetFolders_NormalizesDescriptionsAndOrdersByName()
        {
            // Arrange
            var folderCollection = new Mock<ILiteCollection<PromptFolder>>();
            folderCollection.Setup(collection => collection.FindAll()).Returns([
                new PromptFolder { Id = 2, Name = "Beta", Description = null! },
                new PromptFolder { Id = 1, Name = "Alpha", Description = "A" }
            ]);
            var repository = CreateRepository(folders: folderCollection);

            // Act
            var result = repository.GetFolders();

            // Assert
            Assert.Equal([1, 2], result.Select(folder => folder.Id));
            Assert.Equal(string.Empty, result[1].Description);
        }

        [Fact]
        public void GetAvailableTags_NormalizesDistinctTagsAndOrdersByName()
        {
            // Arrange
            var tagCollection = new Mock<ILiteCollection<PromptTagOption>>();
            tagCollection.Setup(collection => collection.FindAll()).Returns([
                new PromptTagOption { Name = " beta " },
                new PromptTagOption { Name = "Alpha" },
                new PromptTagOption { Name = "alpha" },
                new PromptTagOption { Name = " " }
            ]);
            var repository = CreateRepository(tagOptions: tagCollection);

            // Act
            var result = repository.GetAvailableTags();

            // Assert
            Assert.Equal(["Alpha", "beta"], result);
        }

        [Fact]
        public void GetAvailableModels_NormalizesDistinctModelsAndOrdersByName()
        {
            // Arrange
            var modelCollection = new Mock<ILiteCollection<PromptModelOption>>();
            modelCollection.Setup(collection => collection.FindAll()).Returns([
                new PromptModelOption { Name = " gpt-5 " },
                new PromptModelOption { Name = "Claude Sonnet" },
                new PromptModelOption { Name = "GPT-5" },
                new PromptModelOption { Name = " " }
            ]);
            var repository = CreateRepository(modelOptions: modelCollection);

            // Act
            var result = repository.GetAvailableModels();

            // Assert
            Assert.Equal(["Claude Sonnet", "gpt-5"], result);
        }

        [Fact]
        public void SaveAvailableTags_ReplacesStoredTagsWithNormalizedTags()
        {
            // Arrange
            var insertedTags = new List<string>();
            var tagCollection = new Mock<ILiteCollection<PromptTagOption>>();
            tagCollection
                .Setup(collection => collection.Insert(It.IsAny<PromptTagOption>()))
                .Callback<PromptTagOption>(tag => insertedTags.Add(tag.Name))
                .Returns(new BsonValue(1));
            var repository = CreateRepository(tagOptions: tagCollection);

            // Act
            repository.SaveAvailableTags([" beta ", "Alpha", "alpha", " "]);

            // Assert
            tagCollection.Verify(collection => collection.DeleteAll(), Times.Once);
            Assert.Equal(["Alpha", "beta"], insertedTags);
        }

        [Fact]
        public void SaveAvailableModels_ReplacesStoredModelsWithNormalizedModels()
        {
            // Arrange
            var insertedModels = new List<string>();
            var modelCollection = new Mock<ILiteCollection<PromptModelOption>>();
            modelCollection
                .Setup(collection => collection.Insert(It.IsAny<PromptModelOption>()))
                .Callback<PromptModelOption>(model => insertedModels.Add(model.Name))
                .Returns(new BsonValue(1));
            var repository = CreateRepository(modelOptions: modelCollection);

            // Act
            repository.SaveAvailableModels([" gpt-5 ", "Claude Sonnet", "GPT-5", " "]);

            // Assert
            modelCollection.Verify(collection => collection.DeleteAll(), Times.Once);
            Assert.Equal(["Claude Sonnet", "gpt-5"], insertedModels);
        }

        [Fact]
        public void SavePrompt_WhenNew_NormalizesAndInsertsPrompt()
        {
            // Arrange
            PromptItem? insertedPrompt = null;
            var prompt = new PromptItem
            {
                Name = null!,
                Description = null!,
                Content = null!,
                Quality = 20,
                AiModel = " gpt-5 ",
                Tags = [" beta ", "Alpha", "alpha"]
            };
            var promptCollection = new Mock<ILiteCollection<PromptItem>>();
            promptCollection
                .Setup(collection => collection.Insert(It.IsAny<PromptItem>()))
                .Callback<PromptItem>(value => insertedPrompt = value)
                .Returns(new BsonValue(42));
            var repository = CreateRepository(promptCollection);

            // Act
            repository.SavePrompt(prompt);

            // Assert
            Assert.Same(prompt, insertedPrompt);
            Assert.Equal(42, prompt.Id);
            Assert.Equal(string.Empty, prompt.Name);
            Assert.Equal(string.Empty, prompt.Description);
            Assert.Equal(string.Empty, prompt.Content);
            Assert.Equal(10, prompt.Quality);
            Assert.Equal("gpt-5", prompt.AiModel);
            Assert.Equal(["Alpha", "beta"], prompt.Tags);
            Assert.True(prompt.CreatedAt <= prompt.UpdatedAt);
            promptCollection.Verify(collection => collection.Update(It.IsAny<PromptItem>()), Times.Never);
        }

        [Fact]
        public void SavePrompt_WhenExisting_UpdatesPrompt()
        {
            // Arrange
            var createdAt = new DateTime(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);
            var prompt = new PromptItem
            {
                Id = 7,
                Name = "Name",
                CreatedAt = createdAt
            };
            var promptCollection = new Mock<ILiteCollection<PromptItem>>();
            var repository = CreateRepository(promptCollection);

            // Act
            repository.SavePrompt(prompt);

            // Assert
            Assert.Equal(createdAt, prompt.CreatedAt);
            Assert.True(prompt.UpdatedAt >= createdAt);
            promptCollection.Verify(collection => collection.Update(prompt), Times.Once);
            promptCollection.Verify(collection => collection.Insert(It.IsAny<PromptItem>()), Times.Never);
        }

        [Fact]
        public void SaveFolder_WhenNew_NormalizesAndInsertsFolder()
        {
            // Arrange
            var folder = new PromptFolder
            {
                Name = null!,
                Description = null!
            };
            var folderCollection = new Mock<ILiteCollection<PromptFolder>>();
            folderCollection
                .Setup(collection => collection.Insert(It.IsAny<PromptFolder>()))
                .Returns(new BsonValue(11));
            var repository = CreateRepository(folders: folderCollection);

            // Act
            repository.SaveFolder(folder);

            // Assert
            Assert.Equal(11, folder.Id);
            Assert.Equal(string.Empty, folder.Name);
            Assert.Equal(string.Empty, folder.Description);
            folderCollection.Verify(collection => collection.Update(It.IsAny<PromptFolder>()), Times.Never);
        }

        [Fact]
        public void SaveFolder_WhenExisting_UpdatesFolder()
        {
            // Arrange
            var createdAt = new DateTime(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);
            var folder = new PromptFolder
            {
                Id = 9,
                Name = "Folder",
                CreatedAt = createdAt
            };
            var folderCollection = new Mock<ILiteCollection<PromptFolder>>();
            var repository = CreateRepository(folders: folderCollection);

            // Act
            repository.SaveFolder(folder);

            // Assert
            Assert.Equal(createdAt, folder.CreatedAt);
            Assert.True(folder.UpdatedAt >= createdAt);
            folderCollection.Verify(collection => collection.Update(folder), Times.Once);
            folderCollection.Verify(collection => collection.Insert(It.IsAny<PromptFolder>()), Times.Never);
        }

        [Fact]
        public void DeletePrompt_DeletesPromptById()
        {
            // Arrange
            var promptCollection = new Mock<ILiteCollection<PromptItem>>();
            var repository = CreateRepository(promptCollection);

            // Act
            repository.DeletePrompt(7);

            // Assert
            promptCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 7)), Times.Once);
        }

        [Fact]
        public void DeleteFolder_DeletesFolderDescendantsAndTheirPrompts()
        {
            // Arrange
            var folders = new[]
            {
                new PromptFolder { Id = 1, Name = "Root" },
                new PromptFolder { Id = 2, Name = "Child", ParentFolderId = 1 },
                new PromptFolder { Id = 3, Name = "Other" }
            };
            var prompts = new[]
            {
                new PromptItem { Id = 10, FolderId = 1 },
                new PromptItem { Id = 11, FolderId = 2 },
                new PromptItem { Id = 12, FolderId = 3 },
                new PromptItem { Id = 13 }
            };

            var folderCollection = new Mock<ILiteCollection<PromptFolder>>();
            folderCollection.Setup(collection => collection.FindAll()).Returns(folders);

            var promptCollection = new Mock<ILiteCollection<PromptItem>>();
            promptCollection
                .Setup(collection => collection.Find(It.IsAny<Expression<Func<PromptItem, bool>>>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((Expression<Func<PromptItem, bool>> predicate, int skip, int limit) => prompts.Where(predicate.Compile()));

            var repository = CreateRepository(promptCollection, folderCollection);

            // Act
            repository.DeleteFolder(1);

            // Assert
            promptCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 10)), Times.Once);
            promptCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 11)), Times.Once);
            promptCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 12)), Times.Never);
            folderCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 1)), Times.Once);
            folderCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 2)), Times.Once);
            folderCollection.Verify(collection => collection.Delete(It.Is<BsonValue>(value => value.AsInt32 == 3)), Times.Never);
        }

        private static PromptRepository CreateRepository(
            Mock<ILiteCollection<PromptItem>>? prompts = null,
            Mock<ILiteCollection<PromptFolder>>? folders = null,
            Mock<ILiteCollection<PromptTagOption>>? tagOptions = null,
            Mock<ILiteCollection<PromptModelOption>>? modelOptions = null) =>
            new(
                prompts?.Object ?? new Mock<ILiteCollection<PromptItem>>().Object,
                folders?.Object ?? new Mock<ILiteCollection<PromptFolder>>().Object,
                tagOptions?.Object ?? new Mock<ILiteCollection<PromptTagOption>>().Object,
                modelOptions?.Object ?? new Mock<ILiteCollection<PromptModelOption>>().Object);
    }
}
