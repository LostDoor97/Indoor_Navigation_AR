using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndoorNavigation.Utilities
{
    /// <summary>
    /// Visualization tool for navigation graphs in the Scene view
    /// Shows nodes as spheres and connections as lines
    /// </summary>
    public class NavigationGraphVisualizer : MonoBehaviour
    {
        [SerializeField] private Navigation.NavigationGraph m_Graph;
        [SerializeField] private bool m_VisualizeNodes = true;
        [SerializeField] private bool m_VisualizeConnections = true;
        [SerializeField] private float m_NodeRadius = 0.5f;
        [SerializeField] private Color m_RegularNodeColor = Color.blue;
        [SerializeField] private Color m_POINodeColor = Color.green;
        [SerializeField] private Color m_ConnectionColor = Color.white;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (m_Graph == null)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;

            // Draw nodes
            if (m_VisualizeNodes)
            {
                foreach (var node in m_Graph.Nodes)
                {
                    Gizmos.color = node.IsPointOfInterest ? m_POINodeColor : m_RegularNodeColor;
                    Gizmos.DrawSphere(node.Position, m_NodeRadius);
                }
            }

            // Draw connections
            if (m_VisualizeConnections)
            {
                Gizmos.color = m_ConnectionColor;
                foreach (var node in m_Graph.Nodes)
                {
                    foreach (var connectedId in node.ConnectedNodeIds)
                    {
                        var connectedNode = m_Graph.FindNodeById(connectedId);
                        if (connectedNode != null)
                        {
                            Gizmos.DrawLine(node.Position, connectedNode.Position);
                        }
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// Load and visualize a graph from JSON
        /// </summary>
        public void LoadGraphForVisualization(string jsonFileName)
        {
            var dbGo = new GameObject("TempNavigationDatabase");
            var database = dbGo.AddComponent<Database.NavigationDatabase>();
            m_Graph = database.LoadGraphFromJSON(jsonFileName);
            DestroyImmediate(dbGo);
        }
#endif

        /// <summary>
        /// Export graph to OBJ file for 3D visualization
        /// </summary>
        public void ExportGraphToOBJ(string outputPath)
        {
            if (m_Graph == null)
            {
                Debug.LogError("[Graph Visualizer] No graph loaded");
                return;
            }

            var obj = new System.Text.StringBuilder();

            // Vertices (nodes)
            foreach (var node in m_Graph.Nodes)
            {
                obj.AppendLine($"v {node.Position.x} {node.Position.y} {node.Position.z}");
            }

            // Lines (connections)
            for (int i = 0; i < m_Graph.Nodes.Count; i++)
            {
                var node = m_Graph.Nodes[i];
                foreach (var connectedId in node.ConnectedNodeIds)
                {
                    int connectedIndex = m_Graph.Nodes.FindIndex(n => n.Id == connectedId);
                    if (connectedIndex >= 0)
                    {
                        obj.AppendLine($"l {i + 1} {connectedIndex + 1}");
                    }
                }
            }

            System.IO.File.WriteAllText(outputPath, obj.ToString());
            Debug.Log($"[Graph Visualizer] Exported graph to {outputPath}");
        }

        /// <summary>
        /// Create physical game objects for all nodes (for testing)
        /// </summary>
        public void CreatePhysicalNodes(Material material = null)
        {
            if (m_Graph == null)
            {
                Debug.LogError("[Graph Visualizer] No graph loaded");
                return;
            }

            var parent = new GameObject("NavigationNodes");
            parent.transform.SetParent(transform);

            foreach (var node in m_Graph.Nodes)
            {
                var nodeGo = new GameObject(node.Name);
                nodeGo.transform.SetParent(parent.transform);
                nodeGo.transform.position = node.Position;

                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(nodeGo.transform);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = Vector3.one * m_NodeRadius * 2;

                if (material != null)
                {
                    sphere.GetComponent<Renderer>().material = material;
                }
                else
                {
                    var color = node.IsPointOfInterest ? m_POINodeColor : m_RegularNodeColor;
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = color;
                    sphere.GetComponent<Renderer>().material = mat;
                }

                // Add label
                var label = nodeGo.AddComponent<TextMesh>();
                label.text = node.Name;
                label.fontSize = 100;
            }

            Debug.Log($"[Graph Visualizer] Created physical nodes for {m_Graph.Nodes.Count} nodes");
        }

        /// <summary>
        /// Get graph statistics
        /// </summary>
        public void PrintGraphStatistics()
        {
            if (m_Graph == null)
            {
                Debug.LogError("[Graph Visualizer] No graph loaded");
                return;
            }

            Debug.Log("=== Navigation Graph Statistics ===");
            Debug.Log($"Building: {m_Graph.BuildingName}");
            Debug.Log($"Total Nodes: {m_Graph.Nodes.Count}");
            Debug.Log($"Points of Interest: {m_Graph.GetAllPointsOfInterest().Count}");

            int totalConnections = 0;
            float minDistance = float.MaxValue;
            float maxDistance = 0;
            float avgDistance = 0;

            foreach (var node in m_Graph.Nodes)
            {
                totalConnections += node.ConnectedNodeIds.Count;

                foreach (var cost in node.EdgeCosts)
                {
                    minDistance = Mathf.Min(minDistance, cost);
                    maxDistance = Mathf.Max(maxDistance, cost);
                    avgDistance += cost;
                }
            }

            if (totalConnections > 0)
                avgDistance /= totalConnections;

            Debug.Log($"Total Connections: {totalConnections}");
            Debug.Log($"Min Edge Cost: {minDistance}m");
            Debug.Log($"Max Edge Cost: {maxDistance}m");
            Debug.Log($"Avg Edge Cost: {avgDistance:F2}m");

            // Category breakdown
            var categories = new Dictionary<string, int>();
            foreach (var poi in m_Graph.GetAllPointsOfInterest())
            {
                if (!categories.ContainsKey(poi.Category))
                    categories[poi.Category] = 0;
                categories[poi.Category]++;
            }

            Debug.Log("POI Categories:");
            foreach (var kvp in categories)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            Debug.Log("====================================");
        }

        /// <summary>
        /// Validate graph and report issues
        /// </summary>
        public void ValidateAndReportGraph()
        {
            if (m_Graph == null)
            {
                Debug.LogError("[Graph Visualizer] No graph loaded");
                return;
            }

            Debug.Log("[Graph Visualizer] Starting validation...");

            int warnings = 0;
            int errors = 0;

            // Check for duplicate IDs
            var seenIds = new HashSet<string>();
            foreach (var node in m_Graph.Nodes)
            {
                if (seenIds.Contains(node.Id))
                {
                    Debug.LogError($"[Graph Visualizer] Duplicate node ID: {node.Id}");
                    errors++;
                }
                seenIds.Add(node.Id);
            }

            // Check for orphaned nodes
            foreach (var node in m_Graph.Nodes)
            {
                if (node.ConnectedNodeIds.Count == 0 && !node.IsPointOfInterest)
                {
                    Debug.LogWarning($"[Graph Visualizer] Orphaned node (no connections): {node.Name}");
                    warnings++;
                }

                // Check for invalid connections
                foreach (var connectedId in node.ConnectedNodeIds)
                {
                    if (m_Graph.FindNodeById(connectedId) == null)
                    {
                        Debug.LogError($"[Graph Visualizer] Invalid connection from {node.Name} to {connectedId}");
                        errors++;
                    }
                }
            }

            // Check for unreachable POIs
            var pathfinder = new Pathfinding.AStarPathfinder(m_Graph);
            var pois = m_Graph.GetAllPointsOfInterest();

            if (pois.Count > 0)
            {
                var startPoi = pois[0];
                foreach (var targetPoi in pois)
                {
                    if (startPoi.Id == targetPoi.Id)
                        continue;

                    var path = pathfinder.FindPath(startPoi.Id, targetPoi.Id);
                    if (path == null || path.Count == 0)
                    {
                        Debug.LogError($"[Graph Visualizer] Unreachable POI: {targetPoi.Name} from {startPoi.Name}");
                        errors++;
                    }
                }
            }

            Debug.Log($"[Graph Visualizer] Validation complete: {errors} errors, {warnings} warnings");
        }
    }
}
