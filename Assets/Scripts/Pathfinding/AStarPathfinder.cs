using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndoorNavigation.Pathfinding
{
    /// <summary>
    /// Represents a node in the A* search
    /// </summary>
    public class AStarNode : IComparable<AStarNode>
    {
        public string NodeId;
        public float GCost; // Cost from start
        public float HCost; // Heuristic cost to goal
        public float FCost => GCost + HCost; // Total cost
        public AStarNode Parent;

        public AStarNode(string nodeId)
        {
            NodeId = nodeId;
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }

        public int CompareTo(AStarNode other)
        {
            return FCost.CompareTo(other.FCost);
        }
    }

    /// <summary>
    /// Implements the A* pathfinding algorithm for navigation graph traversal
    /// </summary>
    public class AStarPathfinder
    {
        private readonly Navigation.NavigationGraph m_Graph;
        private Dictionary<string, AStarNode> m_SearchSpace;
        private List<AStarNode> m_OpenSet;
        private HashSet<string> m_ClosedSet;

        public AStarPathfinder(Navigation.NavigationGraph graph)
        {
            m_Graph = graph;
            m_SearchSpace = new Dictionary<string, AStarNode>();
            m_OpenSet = new List<AStarNode>();
            m_ClosedSet = new HashSet<string>();
        }

        /// <summary>
        /// Euclidean heuristic: straight-line distance to goal
        /// </summary>
        private float CalculateHeuristic(Vector3 current, Vector3 goal)
        {
            return Vector3.Distance(current, goal);
        }

        /// <summary>
        /// Find the shortest path from start to goal node
        /// Returns null if no path exists
        /// </summary>
        public List<string> FindPath(string startNodeId, string goalNodeId)
        {
            // Reset search data
            m_SearchSpace.Clear();
            m_OpenSet.Clear();
            m_ClosedSet.Clear();

            var startNode = m_Graph.FindNodeById(startNodeId);
            var goalNode = m_Graph.FindNodeById(goalNodeId);

            if (startNode == null || goalNode == null)
            {
                Debug.LogError($"[A* Pathfinder] Invalid start or goal node: {startNodeId} -> {goalNodeId}");
                return null;
            }

            // Initialize start node
            var startAStarNode = new AStarNode(startNodeId);
            startAStarNode.GCost = 0;
            startAStarNode.HCost = CalculateHeuristic(startNode.Position, goalNode.Position);
            m_SearchSpace[startNodeId] = startAStarNode;
            m_OpenSet.Add(startAStarNode);

            while (m_OpenSet.Count > 0)
            {
                // Get node with lowest F cost
                m_OpenSet.Sort();
                var current = m_OpenSet[0];
                m_OpenSet.RemoveAt(0);

                if (current.NodeId == goalNodeId)
                {
                    return ReconstructPath(current);
                }

                m_ClosedSet.Add(current.NodeId);

                var currentNode = m_Graph.FindNodeById(current.NodeId);

                // Explore neighbors
                for (int i = 0; i < currentNode.ConnectedNodeIds.Count; i++)
                {
                    string neighborId = currentNode.ConnectedNodeIds[i];
                    float edgeCost = currentNode.EdgeCosts[i];

                    if (m_ClosedSet.Contains(neighborId))
                        continue;

                    var neighborNode = m_Graph.FindNodeById(neighborId);
                    if (neighborNode == null)
                        continue;

                    float tentativeGCost = current.GCost + edgeCost;

                    if (!m_SearchSpace.ContainsKey(neighborId))
                    {
                        var neighborAStarNode = new AStarNode(neighborId);
                        neighborAStarNode.GCost = tentativeGCost;
                        neighborAStarNode.HCost = CalculateHeuristic(neighborNode.Position, goalNode.Position);
                        neighborAStarNode.Parent = current;
                        m_SearchSpace[neighborId] = neighborAStarNode;
                        m_OpenSet.Add(neighborAStarNode);
                    }
                    else
                    {
                        var neighbor = m_SearchSpace[neighborId];
                        if (tentativeGCost < neighbor.GCost)
                        {
                            neighbor.GCost = tentativeGCost;
                            neighbor.Parent = current;
                        }
                    }
                }
            }

            Debug.LogWarning($"[A* Pathfinder] No path found from {startNodeId} to {goalNodeId}");
            return null;
        }

        /// <summary>
        /// Reconstruct the path from start to goal
        /// </summary>
        private List<string> ReconstructPath(AStarNode node)
        {
            var path = new List<string>();
            var current = node;

            while (current != null)
            {
                path.Insert(0, current.NodeId);
                current = current.Parent;
            }

            return path;
        }

        /// <summary>
        /// Get the total cost of a path
        /// </summary>
        public float GetPathCost(List<string> path)
        {
            if (path == null || path.Count < 2)
                return 0;

            float totalCost = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var node = m_Graph.FindNodeById(path[i]);
                int neighborIndex = node.ConnectedNodeIds.IndexOf(path[i + 1]);
                if (neighborIndex >= 0)
                {
                    totalCost += node.EdgeCosts[neighborIndex];
                }
            }

            return totalCost;
        }
    }
}
