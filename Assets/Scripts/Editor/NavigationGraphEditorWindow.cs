using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace IndoorNavigation.Editor
{
    /// <summary>
    /// Editor window for creating and editing navigation graphs
    /// </summary>
    public class NavigationGraphEditorWindow : EditorWindow
    {
        private Navigation.NavigationGraph m_CurrentGraph;
        private Vector2 m_ScrollPosition;
        private string m_GraphFileName = "navigation_graph.json";
        private int m_SelectedNodeIndex = -1;

        // Node creation
        private string m_NewNodeId = "";
        private string m_NewNodeName = "";
        private Vector3 m_NewNodePosition = Vector3.zero;
        private string m_NewNodeMarkerId = "";
        private bool m_NewNodeIsPOI = false;
        private string m_NewNodeCategory = "";

        [MenuItem("Window/Indoor Navigation/Graph Editor")]
        public static void ShowWindow()
        {
            GetWindow<NavigationGraphEditorWindow>("Navigation Graph Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Navigation Graph Editor", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // File operations
            EditorGUILayout.LabelField("File Operations", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            m_GraphFileName = EditorGUILayout.TextField("Graph File Name", m_GraphFileName);

            if (GUILayout.Button("Load Graph", GUILayout.Width(100)))
            {
                LoadGraph();
            }
            if (GUILayout.Button("Save Graph", GUILayout.Width(100)))
            {
                SaveGraph();
            }
            if (GUILayout.Button("New Graph", GUILayout.Width(100)))
            {
                NewGraph();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (m_CurrentGraph == null)
            {
                EditorGUILayout.HelpBox("No graph loaded. Click 'Load Graph' or 'New Graph' to start.", MessageType.Info);
                return;
            }

            // Graph info
            EditorGUILayout.LabelField($"Building: {m_CurrentGraph.BuildingName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Nodes: {m_CurrentGraph.Nodes.Count}", EditorStyles.label);

            EditorGUILayout.Space();

            // Node creation section
            EditorGUILayout.LabelField("Create New Node", EditorStyles.boldLabel);

            m_NewNodeId = EditorGUILayout.TextField("Node ID", m_NewNodeId);
            m_NewNodeName = EditorGUILayout.TextField("Node Name", m_NewNodeName);
            m_NewNodePosition = EditorGUILayout.Vector3Field("Position", m_NewNodePosition);
            m_NewNodeMarkerId = EditorGUILayout.TextField("Marker ID (QR Code)", m_NewNodeMarkerId);
            m_NewNodeIsPOI = EditorGUILayout.Toggle("Is Point of Interest", m_NewNodeIsPOI);
            if (m_NewNodeIsPOI)
            {
                m_NewNodeCategory = EditorGUILayout.TextField("Category", m_NewNodeCategory);
            }

            if (GUILayout.Button("Add Node"))
            {
                AddNode();
            }

            EditorGUILayout.Space();

            // Node list
            EditorGUILayout.LabelField("Nodes", EditorStyles.boldLabel);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            for (int i = 0; i < m_CurrentGraph.Nodes.Count; i++)
            {
                var node = m_CurrentGraph.Nodes[i];
                DrawNodeGUI(node, i);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Validate graph
            if (GUILayout.Button("Validate Graph"))
            {
                if (m_CurrentGraph.ValidateIntegrity())
                {
                    EditorUtility.DisplayDialog("Validation", "Graph is valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation", "Graph has errors. Check console for details.", "OK");
                }
            }
        }

        private void DrawNodeGUI(Navigation.NavigationNode node, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool isSelected = m_SelectedNodeIndex == index;
            if (GUILayout.Button($"{node.Name} ({node.Id})", isSelected ? EditorStyles.boldLabel : EditorStyles.label))
            {
                m_SelectedNodeIndex = isSelected ? -1 : index;
            }

            if (isSelected)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.TextField("ID", node.Id);
                node.Name = EditorGUILayout.TextField("Name", node.Name);
                node.Position = EditorGUILayout.Vector3Field("Position", node.Position);
                node.MarkerId = EditorGUILayout.TextField("Marker ID", node.MarkerId);
                node.IsPointOfInterest = EditorGUILayout.Toggle("Is POI", node.IsPointOfInterest);
                if (node.IsPointOfInterest)
                {
                    node.Category = EditorGUILayout.TextField("Category", node.Category);
                }

                EditorGUILayout.LabelField($"Connections: {node.ConnectedNodeIds.Count}");

                if (GUILayout.Button("Delete Node"))
                {
                    m_CurrentGraph.RemoveNode(node.Id);
                    m_SelectedNodeIndex = -1;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNode()
        {
            if (string.IsNullOrEmpty(m_NewNodeId) || string.IsNullOrEmpty(m_NewNodeName))
            {
                EditorUtility.DisplayDialog("Error", "Node ID and Name are required.", "OK");
                return;
            }

            var newNode = new Navigation.NavigationNode(m_NewNodeId, m_NewNodeName, m_NewNodePosition);
            newNode.MarkerId = m_NewNodeMarkerId;
            newNode.IsPointOfInterest = m_NewNodeIsPOI;
            newNode.Category = m_NewNodeCategory;

            m_CurrentGraph.AddNode(newNode);

            // Reset fields
            m_NewNodeId = "";
            m_NewNodeName = "";
            m_NewNodePosition = Vector3.zero;
            m_NewNodeMarkerId = "";
            m_NewNodeIsPOI = false;
            m_NewNodeCategory = "";

            EditorUtility.DisplayDialog("Success", "Node added successfully.", "OK");
        }

        private void LoadGraph()
        {
            var dbGo = new GameObject("TempNavigationDatabase");
            var database = dbGo.AddComponent<Database.NavigationDatabase>();
            m_CurrentGraph = database.LoadGraphFromJSON(m_GraphFileName);
            DestroyImmediate(dbGo);

            if (m_CurrentGraph != null)
            {
                EditorUtility.DisplayDialog("Success", $"Loaded graph: {m_CurrentGraph.BuildingName}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to load graph. Check the file name and StreamingAssets folder.", "OK");
            }
        }

        private void SaveGraph()
        {
            if (m_CurrentGraph == null)
            {
                EditorUtility.DisplayDialog("Error", "No graph loaded.", "OK");
                return;
            }

            var dbGo = new GameObject("TempNavigationDatabase");
            var database = dbGo.AddComponent<Database.NavigationDatabase>();
            bool success = database.SaveGraphToJSON(m_CurrentGraph, m_GraphFileName);
            DestroyImmediate(dbGo);

            if (success)
            {
                EditorUtility.DisplayDialog("Success", "Graph saved successfully.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to save graph.", "OK");
            }
        }

        private void NewGraph()
        {
            m_CurrentGraph = new Navigation.NavigationGraph("New Building");
            EditorUtility.DisplayDialog("Success", "New graph created.", "OK");
        }
    }
}
#endif
