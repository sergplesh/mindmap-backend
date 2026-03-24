using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Services
{
    public sealed class NodeLearningState
    {
        public bool IsVisible { get; init; }
        public bool IsUnlocked { get; init; }
        public bool CanReadContent { get; init; }
        public int Level { get; init; }
    }

    public sealed class MapLearningAccessSnapshot
    {
        public string UserRole { get; init; } = "observer";
        public IReadOnlyDictionary<int, NodeLearningState> NodeStates { get; init; } = new Dictionary<int, NodeLearningState>();
        public ISet<int> PassedNodeIds { get; init; } = new HashSet<int>();
    }

    public static class MapLearningAccessResolver
    {
        public static async Task<MapLearningAccessSnapshot> BuildAsync(
            ApplicationDbContext context,
            int mapId,
            int userId,
            Map? loadedMap = null)
        {
            var map = loadedMap ?? await context.Maps
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Questions)
                .FirstOrDefaultAsync(m => m.Id == mapId);

            if (map == null)
            {
                return new MapLearningAccessSnapshot();
            }

            var hierarchyEdges = await context.Edges
                .Where(e => e.IsHierarchy && e.SourceNode.MapId == mapId && e.TargetNode.MapId == mapId)
                .Select(e => new
                {
                    e.SourceNodeId,
                    e.TargetNodeId
                })
                .ToListAsync();

            var userRole = await ResolveUserRoleAsync(context, map, userId);
            var unlockAll = userRole == "owner" || userRole == "observer";
            var nodeIds = map.Nodes.Select(n => n.Id).ToList();

            var passedNodeIds = unlockAll
                ? new HashSet<int>()
                : (await context.AnswerResults
                    .Where(ar => ar.UserId == userId && ar.IsPassed && nodeIds.Contains(ar.NodeId))
                    .Select(ar => ar.NodeId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();

            var childrenByNodeId = new Dictionary<int, List<int>>();
            var parentByNodeId = new Dictionary<int, int>();

            foreach (var edge in hierarchyEdges)
            {
                if (!childrenByNodeId.TryGetValue(edge.SourceNodeId, out var children))
                {
                    children = new List<int>();
                    childrenByNodeId[edge.SourceNodeId] = children;
                }

                children.Add(edge.TargetNodeId);
                parentByNodeId[edge.TargetNodeId] = edge.SourceNodeId;
            }

            var rootIds = map.Nodes
                .Where(n => !parentByNodeId.ContainsKey(n.Id))
                .Select(n => n.Id)
                .ToList();

            if (rootIds.Count == 0 && map.Nodes.Count > 0)
            {
                rootIds.Add(map.Nodes.First().Id);
            }

            var levelByNodeId = new Dictionary<int, int>();
            var levelQueue = new Queue<(int NodeId, int Level)>();

            foreach (var rootId in rootIds)
            {
                if (levelByNodeId.ContainsKey(rootId))
                {
                    continue;
                }

                levelByNodeId[rootId] = 0;
                levelQueue.Enqueue((rootId, 0));

                while (levelQueue.Count > 0)
                {
                    var (currentNodeId, currentLevel) = levelQueue.Dequeue();
                    if (!childrenByNodeId.TryGetValue(currentNodeId, out var childIds))
                    {
                        continue;
                    }

                    foreach (var childId in childIds)
                    {
                        if (levelByNodeId.ContainsKey(childId))
                        {
                            continue;
                        }

                        levelByNodeId[childId] = currentLevel + 1;
                        levelQueue.Enqueue((childId, currentLevel + 1));
                    }
                }
            }

            var nodeById = map.Nodes.ToDictionary(n => n.Id);
            var nodeStates = new Dictionary<int, NodeLearningState>();
            var visited = new HashSet<int>();

            void Traverse(int nodeId, bool isVisible)
            {
                if (!visited.Add(nodeId) || !nodeById.TryGetValue(nodeId, out var node))
                {
                    return;
                }

                var hasBlockingQuiz = !unlockAll
                    && node.Questions.Any()
                    && levelByNodeId.TryGetValue(nodeId, out var nodeLevel)
                    && nodeLevel > 0
                    && !passedNodeIds.Contains(node.Id);

                var isUnlocked = unlockAll || (isVisible && !hasBlockingQuiz);

                nodeStates[nodeId] = new NodeLearningState
                {
                    IsVisible = unlockAll || isVisible,
                    IsUnlocked = isUnlocked,
                    CanReadContent = unlockAll || isUnlocked,
                    Level = levelByNodeId.TryGetValue(nodeId, out var currentLevel) ? currentLevel : 0
                };

                if (!childrenByNodeId.TryGetValue(nodeId, out var childIds))
                {
                    return;
                }

                var canRevealChildren = unlockAll || isUnlocked;
                foreach (var childId in childIds)
                {
                    Traverse(childId, canRevealChildren);
                }
            }

            foreach (var rootId in rootIds)
            {
                Traverse(rootId, true);
            }

            foreach (var nodeId in nodeById.Keys)
            {
                if (!nodeStates.ContainsKey(nodeId))
                {
                    Traverse(nodeId, true);
                }
            }

            return new MapLearningAccessSnapshot
            {
                UserRole = userRole,
                NodeStates = nodeStates,
                PassedNodeIds = passedNodeIds
            };
        }

        private static async Task<string> ResolveUserRoleAsync(ApplicationDbContext context, Map map, int userId)
        {
            if (map.OwnerId == userId)
            {
                return "owner";
            }

            if (map.Accesses.Any())
            {
                var loadedAccess = map.Accesses.FirstOrDefault(a => a.UserId == userId);
                if (loadedAccess != null)
                {
                    return loadedAccess.Role;
                }
            }

            var accessRole = await context.Accesses
                .Where(a => a.MapId == map.Id && a.UserId == userId)
                .Select(a => a.Role)
                .FirstOrDefaultAsync();

            return accessRole ?? "observer";
        }
    }
}
