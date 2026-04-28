using PromptManager.Models;

namespace PromptManager.Services
{
    public sealed class PromptTreeService
    {
        public IReadOnlyList<PromptTreeNode> BuildTree(
            IEnumerable<PromptFolder> folders,
            IEnumerable<PromptItem> prompts,
            IEnumerable<int> expandedFolderIds,
            bool showAllPrompts,
            string? search)
        {
            var folderList = folders.ToList();
            var promptList = prompts.ToList();
            var expandedIds = expandedFolderIds.ToHashSet();
            var nodes = new List<PromptTreeNode>();
            var normalizedSearch = search?.Trim();

            if (showAllPrompts || !string.IsNullOrWhiteSpace(normalizedSearch))
            {
                nodes.AddRange(FilterPrompts(promptList, normalizedSearch).Select(prompt => new PromptTreeNode
                {
                    Key = $"prompt:{prompt.Id}",
                    Prompt = prompt,
                    Depth = 0
                }));

                return nodes;
            }

            foreach (var folder in folderList.Where(folder => folder.ParentFolderId is null).OrderBy(folder => folder.Name ?? string.Empty))
            {
                nodes.AddRange(BuildFolderNodes(folder, folderList, promptList, expandedIds, 0));
            }

            nodes.AddRange(promptList
                .Where(prompt => prompt.FolderId is null)
                .OrderBy(prompt => prompt.Name ?? string.Empty)
                .Select(prompt => new PromptTreeNode
                {
                    Key = $"prompt:{prompt.Id}",
                    Prompt = prompt,
                    Depth = 0
                }));

            return nodes;
        }

        public IReadOnlyList<PromptTreeNode> BuildChildNodes(
            PromptFolder folder,
            IEnumerable<PromptFolder> folders,
            IEnumerable<PromptItem> prompts,
            IEnumerable<int> expandedFolderIds,
            int depth) =>
            BuildChildNodesIterator(folder, folders.ToList(), prompts.ToList(), expandedFolderIds.ToHashSet(), depth).ToList();

        public IReadOnlyList<PromptItem> FilterPrompts(IEnumerable<PromptItem> source, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return source.OrderBy(prompt => prompt.Name ?? string.Empty).ToList();
            }

            return source
                .Where(prompt =>
                    (prompt.Name ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (prompt.Description ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (prompt.Content ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (prompt.AiModel ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (prompt.Tags ?? []).Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(prompt => prompt.Name ?? string.Empty)
                .ToList();
        }

        public string BuildFolderPath(PromptFolder folder, IEnumerable<PromptFolder> folders)
        {
            var folderList = folders.ToList();
            var names = new Stack<string>();
            var current = folder;

            while (true)
            {
                names.Push(string.IsNullOrWhiteSpace(current.Name) ? "Untitled folder" : current.Name);
                if (current.ParentFolderId is not int parentId)
                {
                    break;
                }

                var parent = folderList.FirstOrDefault(candidate => candidate.Id == parentId);
                if (parent is null)
                {
                    break;
                }

                current = parent;
            }

            return string.Join(" / ", names);
        }

        public bool IsDescendantFolder(int candidateFolderId, int parentFolderId, IEnumerable<PromptFolder> folders)
        {
            var folderList = folders.ToList();
            var current = folderList.FirstOrDefault(folder => folder.Id == candidateFolderId);

            while (current?.ParentFolderId is int nextParentId)
            {
                if (nextParentId == parentFolderId)
                {
                    return true;
                }

                current = folderList.FirstOrDefault(folder => folder.Id == nextParentId);
            }

            return false;
        }

        private IEnumerable<PromptTreeNode> BuildFolderNodes(
            PromptFolder folder,
            IReadOnlyList<PromptFolder> folders,
            IReadOnlyList<PromptItem> prompts,
            ISet<int> expandedFolderIds,
            int depth)
        {
            var expanded = expandedFolderIds.Contains(folder.Id);
            yield return new PromptTreeNode
            {
                Key = $"folder:{folder.Id}",
                Folder = folder,
                Depth = depth,
                IsExpanded = expanded
            };

            if (!expanded)
            {
                yield break;
            }

            foreach (var child in BuildChildNodesIterator(folder, folders, prompts, expandedFolderIds, depth + 1))
            {
                yield return child;
            }
        }

        private IEnumerable<PromptTreeNode> BuildChildNodesIterator(
            PromptFolder folder,
            IReadOnlyList<PromptFolder> folders,
            IReadOnlyList<PromptItem> prompts,
            ISet<int> expandedFolderIds,
            int depth)
        {
            foreach (var child in folders.Where(candidate => candidate.ParentFolderId == folder.Id).OrderBy(candidate => candidate.Name ?? string.Empty))
            {
                var expanded = expandedFolderIds.Contains(child.Id);
                yield return new PromptTreeNode
                {
                    Key = $"folder:{child.Id}",
                    Folder = child,
                    Depth = depth,
                    IsExpanded = expanded
                };

                if (expanded)
                {
                    foreach (var descendant in BuildChildNodesIterator(child, folders, prompts, expandedFolderIds, depth + 1))
                    {
                        yield return descendant;
                    }
                }
            }

            foreach (var prompt in prompts.Where(prompt => prompt.FolderId == folder.Id).OrderBy(prompt => prompt.Name ?? string.Empty))
            {
                yield return new PromptTreeNode
                {
                    Key = $"prompt:{prompt.Id}",
                    Prompt = prompt,
                    Depth = depth
                };
            }
        }
    }
}
