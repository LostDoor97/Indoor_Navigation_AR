using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndoorNavigation.ARRendering
{
    /// <summary>
    /// Renders navigation path as AR arrows/lines on the ground
    /// </summary>
    public class PathRenderer : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private Material m_PathMaterial;
        [SerializeField] private Material m_ArrowMaterial;
        [SerializeField] private float m_PathWidth = 0.1f;
        [SerializeField] private float m_PathHeight = 0.01f; // Offset from ground

        [Header("Arrow Settings")]
        [SerializeField] private float m_ArrowSpacing = 1f; // Distance between arrows
        [SerializeField] private float m_ArrowSize = 0.3f;
        [SerializeField] private Color m_PathColor = Color.cyan;
        [SerializeField] private Color m_NextSegmentColor = Color.yellow;

        private LineRenderer m_LineRenderer;
        private List<GameObject> m_ArrowObjects;
        private List<string> m_CurrentPath;
        private Navigation.NavigationGraph m_Graph;
        private int m_CurrentWaypointIndex;

        private void Awake()
        {
            m_ArrowObjects = new List<GameObject>();
            m_CurrentPath = new List<string>();

            // Create line renderer
            m_LineRenderer = gameObject.AddComponent<LineRenderer>();
            m_LineRenderer.material = m_PathMaterial != null ? m_PathMaterial : new Material(Shader.Find("Sprites/Default"));
            m_LineRenderer.startWidth = m_PathWidth;
            m_LineRenderer.endWidth = m_PathWidth;
            m_LineRenderer.startColor = m_PathColor;
            m_LineRenderer.endColor = m_PathColor;
            m_LineRenderer.sortingOrder = -1;
        }

        /// <summary>
        /// Set the navigation graph for path rendering
        /// </summary>
        public void SetNavigationGraph(Navigation.NavigationGraph graph)
        {
            m_Graph = graph;
        }

        /// <summary>
        /// Render a path given a list of node IDs
        /// </summary>
        public void RenderPath(List<string> path)
        {
            if (path == null || path.Count < 2)
            {
                ClearPath();
                return;
            }

            m_CurrentPath = new List<string>(path);
            m_CurrentWaypointIndex = 0;

            UpdatePathVisualization();
        }

        /// <summary>
        /// Update the visual representation of the current path
        /// </summary>
        private void UpdatePathVisualization()
        {
            if (m_Graph == null)
            {
                Debug.LogWarning("[Path Renderer] Navigation graph not set");
                return;
            }

            // Collect all waypoint positions
            var positions = new List<Vector3>();
            foreach (string nodeId in m_CurrentPath)
            {
                var node = m_Graph.FindNodeById(nodeId);
                if (node != null)
                {
                    // Offset Y to render above ground
                    positions.Add(node.Position + Vector3.up * m_PathHeight);
                }
            }

            // Update line renderer
            m_LineRenderer.positionCount = positions.Count;
            for (int i = 0; i < positions.Count; i++)
            {
                m_LineRenderer.SetPosition(i, positions[i]);
            }

            // Create direction arrows
            CreateDirectionalArrows(positions);

            Debug.Log($"[Path Renderer] Rendered path with {positions.Count} waypoints");
        }

        /// <summary>
        /// Create arrow meshes along the path
        /// </summary>
        private void CreateDirectionalArrows(List<Vector3> positions)
        {
            // Clear existing arrows
            foreach (var arrow in m_ArrowObjects)
            {
                Destroy(arrow);
            }
            m_ArrowObjects.Clear();

            // Create arrows at regular intervals
            for (int i = 0; i < positions.Count - 1; i++)
            {
                Vector3 current = positions[i];
                Vector3 next = positions[i + 1];
                Vector3 direction = (next - current).normalized;
                float distance = Vector3.Distance(current, next);

                // Place arrows along this segment
                int arrowCount = Mathf.Max(1, Mathf.FloorToInt(distance / m_ArrowSpacing));
                for (int j = 0; j < arrowCount; j++)
                {
                    float t = (j + 1) / (arrowCount + 1f);
                    Vector3 arrowPos = Vector3.Lerp(current, next, t);
                    CreateArrowAtPosition(arrowPos, direction, i == m_CurrentWaypointIndex);
                }
            }
        }

        /// <summary>
        /// Create a single directional arrow
        /// </summary>
        private void CreateArrowAtPosition(Vector3 position, Vector3 direction, bool isNextSegment)
        {
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(transform);
            arrowGo.transform.position = position;

            // Rotate to face direction
            arrowGo.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Create arrow mesh (simple pyramid)
            var meshFilter = arrowGo.AddComponent<MeshFilter>();
            var meshRenderer = arrowGo.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateArrowMesh();
            meshRenderer.material = m_ArrowMaterial != null ? m_ArrowMaterial : new Material(Shader.Find("Standard"));
            meshRenderer.material.color = isNextSegment ? m_NextSegmentColor : m_PathColor;

            // Scale
            arrowGo.transform.localScale = Vector3.one * m_ArrowSize;

            m_ArrowObjects.Add(arrowGo);
        }

        /// <summary>
        /// Create a simple arrow mesh
        /// </summary>
        private Mesh CreateArrowMesh()
        {
            var mesh = new Mesh();

            // Define arrow vertices (simple pyramid shape)
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0, 0, 0.5f),      // tip
                new Vector3(-0.25f, 0, -0.5f), // base left
                new Vector3(0.25f, 0, -0.5f),  // base right
                new Vector3(0, 0.25f, -0.5f),  // base top
                new Vector3(0, -0.25f, -0.5f)  // base bottom
            };

            int[] triangles = new int[]
            {
                0, 1, 3,  // left side
                0, 3, 2,  // right side
                0, 2, 4,  // bottom side
                0, 4, 1,  // back
                1, 2, 3,  // base
                2, 4, 3
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Update next waypoint in path
        /// </summary>
        public void AdvanceToNextWaypoint()
        {
            if (m_CurrentWaypointIndex < m_CurrentPath.Count - 2)
            {
                m_CurrentWaypointIndex++;
                UpdatePathVisualization();
                Debug.Log($"[Path Renderer] Advanced to waypoint {m_CurrentWaypointIndex}");
            }
        }

        /// <summary>
        /// Get the next waypoint position
        /// </summary>
        public Vector3? GetNextWaypoint()
        {
            if (m_CurrentWaypointIndex + 1 < m_CurrentPath.Count && m_Graph != null)
            {
                var nextNode = m_Graph.FindNodeById(m_CurrentPath[m_CurrentWaypointIndex + 1]);
                if (nextNode != null)
                {
                    return nextNode.Position;
                }
            }
            return null;
        }

        /// <summary>
        /// Clear the rendered path
        /// </summary>
        public void ClearPath()
        {
            m_LineRenderer.positionCount = 0;

            foreach (var arrow in m_ArrowObjects)
            {
                Destroy(arrow);
            }
            m_ArrowObjects.Clear();
            m_CurrentPath.Clear();
            m_CurrentWaypointIndex = 0;
        }

        /// <summary>
        /// Get current waypoint index
        /// </summary>
        public int GetCurrentWaypointIndex()
        {
            return m_CurrentWaypointIndex;
        }

        /// <summary>
        /// Get total waypoint count
        /// </summary>
        public int GetWaypointCount()
        {
            return m_CurrentPath.Count;
        }
    }
}
