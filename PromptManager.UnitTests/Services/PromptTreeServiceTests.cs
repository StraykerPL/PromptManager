using PromptManager.Models;
using PromptManager.Services;

namespace PromptManager.UnitTests.Services
{
    public sealed class PromptTreeServiceTests
    {
        private readonly PromptTreeService service = new();

        [Fact]
        public void FilterPrompts_WithSearch_MatchesNameDescriptionContentTagsAndAiModel()
        {
            // Arrange
            var prompts = new[]
            {
                new PromptItem { Id = 1, Name = "Alpha", Description = "First", Content = "Body", Tags = ["one"] },
                new PromptItem { Id = 2, Name = "Beta", Description = "Target description", Content = "Body", Tags = ["two"] },
                new PromptItem { Id = 3, Name = "Gamma", Description = "Third", Content = "Target content", Tags = ["three"] },
                new PromptItem { Id = 4, Name = "Delta", Description = "Fourth", Content = "Body", Tags = ["target-tag"] },
                new PromptItem { Id = 5, Name = "Epsilon", Description = "Fifth", Content = "Body", AiModel = "Target model" }
            };

            // Act
            var result = service.FilterPrompts(prompts, "target");

            // Assert
            Assert.Equal([2, 4, 5, 3], result.Select(prompt => prompt.Id));
        }

        [Fact]
        public void BuildTree_WhenCollapsed_ShowsRootFoldersAndRootPromptsOnly()
        {
            // Arrange
            var folders = new[]
            {
                new PromptFolder { Id = 2, Name = "Beta" },
                new PromptFolder { Id = 1, Name = "Alpha" },
                new PromptFolder { Id = 3, Name = "Child", ParentFolderId = 1 }
            };
            var prompts = new[]
            {
                new PromptItem { Id = 2, Name = "Root prompt" },
                new PromptItem { Id = 1, Name = "Child prompt", FolderId = 1 }
            };

            // Act
            var result = service.BuildTree(folders, prompts, [], showAllPrompts: false, search: null);

            // Assert
            Assert.Equal(["folder:1", "folder:2", "prompt:2"], result.Select(node => node.Key));
        }

        [Fact]
        public void BuildTree_WhenFolderExpanded_IncludesChildrenAndPrompts()
        {
            // Arrange
            var folders = new[]
            {
                new PromptFolder { Id = 1, Name = "Root" },
                new PromptFolder { Id = 2, Name = "Child", ParentFolderId = 1 }
            };
            var prompts = new[]
            {
                new PromptItem { Id = 1, Name = "Inside", FolderId = 1 }
            };

            // Act
            var result = service.BuildTree(folders, prompts, [1], showAllPrompts: false, search: null);

            // Assert
            Assert.Equal(["folder:1", "folder:2", "prompt:1"], result.Select(node => node.Key));
            Assert.True(result[0].IsExpanded);
            Assert.Equal(1, result[1].Depth);
            Assert.Equal(1, result[2].Depth);
        }

        [Fact]
        public void BuildTree_WithSearch_ShowsMatchingPromptsFlat()
        {
            // Arrange
            var folders = new[]
            {
                new PromptFolder { Id = 1, Name = "Root" }
            };
            var prompts = new[]
            {
                new PromptItem { Id = 1, Name = "Alpha", FolderId = 1 },
                new PromptItem { Id = 2, Name = "Beta" }
            };

            // Act
            var result = service.BuildTree(folders, prompts, [1], showAllPrompts: false, search: "beta");

            // Assert
            var node = Assert.Single(result);
            Assert.Equal("prompt:2", node.Key);
            Assert.Equal(0, node.Depth);
        }

        [Fact]
        public void BuildFolderPath_ReturnsParentPath()
        {
            // Arrange
            var root = new PromptFolder { Id = 1, Name = "Root" };
            var child = new PromptFolder { Id = 2, Name = "Child", ParentFolderId = 1 };

            // Act
            var result = service.BuildFolderPath(child, [root, child]);

            // Assert
            Assert.Equal("Root / Child", result);
        }

        [Fact]
        public void BuildFolderPath_WithBlankName_UsesUntitledFolder()
        {
            // Arrange
            var folder = new PromptFolder { Id = 1, Name = " " };

            // Act
            var result = service.BuildFolderPath(folder, [folder]);

            // Assert
            Assert.Equal("Untitled folder", result);
        }

        [Fact]
        public void IsDescendantFolder_WhenCandidateIsChild_ReturnsTrue()
        {
            // Arrange
            var folders = new[]
            {
                new PromptFolder { Id = 1, Name = "Root" },
                new PromptFolder { Id = 2, Name = "Child", ParentFolderId = 1 },
                new PromptFolder { Id = 3, Name = "Grandchild", ParentFolderId = 2 }
            };

            // Act
            var result = service.IsDescendantFolder(3, 1, folders);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDescendantFolder_WhenCandidateIsNotChild_ReturnsFalse()
        {
            // Arrange
            var folders = new[]
            {
                new PromptFolder { Id = 1, Name = "Root" },
                new PromptFolder { Id = 2, Name = "Other" }
            };

            // Act
            var result = service.IsDescendantFolder(2, 1, folders);

            // Assert
            Assert.False(result);
        }
    }
}
