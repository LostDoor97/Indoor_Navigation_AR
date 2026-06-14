using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndoorNavigation.Tests
{
    /// <summary>
    /// Unit tests for navigation components
    /// Run from editor with Test Framework
    /// </summary>
    public class NavigationTests : MonoBehaviour
    {
        private Navigation.NavigationGraph m_TestGraph;
        private Pathfinding.AStarPathfinder m_Pathfinder;

        /// <summary>
        /// Test 1: Load navigation graph from JSON
        /// </summary>
        public void Test_LoadNavigationGraph()
        {
            Debug.Log("[Test] Starting: Load Navigation Graph");

            var dbGo = new GameObject("TestDatabase");
            var database = dbGo.AddComponent<Database.NavigationDatabase>();
            var graph = database.LoadGraphFromJSON("navigation_graph.json");

            if (graph != null)
            {
                Debug.Log($"? PASS: Graph loaded successfully ({graph.Nodes.Count} nodes)");
                m_TestGraph = graph;
            }
            else
            {
                Debug.LogError("? FAIL: Failed to load graph");
            }
        }

        /// <summary>
        /// Test 2: Validate graph integrity
        /// </summary>
        public void Test_ValidateGraphIntegrity()
        {
            Debug.Log("[Test] Starting: Validate Graph Integrity");

            if (m_TestGraph == null)
            {
                Debug.LogError("? FAIL: No graph loaded");
                return;
            }

            if (m_TestGraph.ValidateIntegrity())
            {
                Debug.Log("? PASS: Graph integrity validated");
            }
            else
            {
                Debug.LogError("? FAIL: Graph integrity check failed");
            }
        }

        /// <summary>
        /// Test 3: A* pathfinding basic functionality
        /// </summary>
        public void Test_AStarPathfinding()
        {
            Debug.Log("[Test] Starting: A* Pathfinding");

            if (m_TestGraph == null)
            {
                Debug.LogError("? FAIL: No graph loaded");
                return;
            }

            m_Pathfinder = new Pathfinding.AStarPathfinder(m_TestGraph);

            if (m_TestGraph.Nodes.Count < 2)
            {
                Debug.LogError("? FAIL: Graph has less than 2 nodes");
                return;
            }

            var startNode = m_TestGraph.Nodes[0];
            var endNode = m_TestGraph.Nodes[m_TestGraph.Nodes.Count - 1];

            var path = m_Pathfinder.FindPath(startNode.Id, endNode.Id);

            if (path != null && path.Count >= 2)
            {
                Debug.Log($"? PASS: Path found ({path.Count} waypoints)");
            }
            else
            {
                Debug.LogWarning("? WARN: No path found (may be expected for disconnected graph)");
            }
        }

        /// <summary>
        /// Test 4: Find points of interest
        /// </summary>
        public void Test_FindPointsOfInterest()
        {
            Debug.Log("[Test] Starting: Find Points of Interest");

            if (m_TestGraph == null)
            {
                Debug.LogError("? FAIL: No graph loaded");
                return;
            }

            var pois = m_TestGraph.GetAllPointsOfInterest();

            if (pois.Count > 0)
            {
                Debug.Log($"? PASS: Found {pois.Count} POIs");
                foreach (var poi in pois)
                {
                    Debug.Log($"  - {poi.Name} ({poi.Category})");
                }
            }
            else
            {
                Debug.LogWarning("? WARN: No POIs found in graph");
            }
        }

        /// <summary>
        /// Test 5: QR code data validation
        /// </summary>
        public void Test_QRCodeDataValidation()
        {
            Debug.Log("[Test] Starting: QR Code Data Validation");

            var validQRData = new QRCode.QRCodeData("QR_TEST", 0.9f, Vector3.zero);
            var invalidQRData = new QRCode.QRCodeData("", 0.5f, Vector3.zero);

            if (validQRData.IsValid)
            {
                Debug.Log("? PASS: Valid QR code recognized");
            }
            else
            {
                Debug.LogError("? FAIL: Valid QR code marked as invalid");
            }

            if (!invalidQRData.IsValid)
            {
                Debug.Log("? PASS: Invalid QR code rejected");
            }
            else
            {
                Debug.LogError("? FAIL: Invalid QR code marked as valid");
            }
        }

        /// <summary>
        /// Test 6: Navigation node connections
        /// </summary>
        public void Test_NavigationNodeConnections()
        {
            Debug.Log("[Test] Starting: Navigation Node Connections");

            var node1 = new Navigation.NavigationNode("node1", "Node 1", Vector3.zero);
            var node2 = new Navigation.NavigationNode("node2", "Node 2", Vector3.right * 5);

            node1.AddConnection("node2", 5.0f);

            if (node1.ConnectedNodeIds.Contains("node2") && node1.EdgeCosts[0] == 5.0f)
            {
                Debug.Log("? PASS: Connection added correctly");
            }
            else
            {
                Debug.LogError("? FAIL: Connection not added properly");
            }

            node1.RemoveConnection("node2");

            if (!node1.ConnectedNodeIds.Contains("node2"))
            {
                Debug.Log("? PASS: Connection removed correctly");
            }
            else
            {
                Debug.LogError("? FAIL: Connection not removed properly");
            }
        }

        /// <summary>
        /// Test 7: Localization manager calibration
        /// </summary>
        public void Test_LocalizationCalibration()
        {
            Debug.Log("[Test] Starting: Localization Calibration");

            var locMgr = gameObject.AddComponent<Localization.LocalizationManager>();
            var testNode = new Navigation.NavigationNode("test", "Test Node", Vector3.one);
            var testPos = Vector3.zero;

            locMgr.SetManualCalibration(testPos, testNode);

            if (locMgr.IsCalibrated && locMgr.GetCurrentNode() == testNode)
            {
                Debug.Log("? PASS: Calibration set correctly");
            }
            else
            {
                Debug.LogError("? FAIL: Calibration not set properly");
            }

            Destroy(locMgr);
        }

        /// <summary>
        /// Test 8: Path cost calculation
        /// </summary>
        public void Test_PathCostCalculation()
        {
            Debug.Log("[Test] Starting: Path Cost Calculation");

            if (m_Pathfinder == null || m_TestGraph == null)
            {
                Debug.LogError("? FAIL: Pathfinder or graph not initialized");
                return;
            }

            var path = new List<string>();
            if (m_TestGraph.Nodes.Count >= 2)
            {
                path.Add(m_TestGraph.Nodes[0].Id);
                path.Add(m_TestGraph.Nodes[1].Id);

                float cost = m_Pathfinder.GetPathCost(path);

                if (cost > 0)
                {
                    Debug.Log($"? PASS: Path cost calculated: {cost}m");
                }
                else
                {
                    Debug.LogWarning("? WARN: Path cost is zero");
                }
            }
        }

        /// <summary>
        /// Test 9: Graph metadata
        /// </summary>
        public void Test_GraphMetadata()
        {
            Debug.Log("[Test] Starting: Graph Metadata");

            if (m_TestGraph == null)
            {
                Debug.LogError("? FAIL: No graph loaded");
                return;
            }

            if (!string.IsNullOrEmpty(m_TestGraph.BuildingName))
            {
                Debug.Log($"? PASS: Building name: {m_TestGraph.BuildingName}");
            }
            else
            {
                Debug.LogWarning("? WARN: Building name is empty");
            }

            if (m_TestGraph.GraphMetadata != null)
            {
                Debug.Log("? PASS: Graph metadata present");
            }
            else
            {
                Debug.LogWarning("? WARN: No graph metadata");
            }
        }

        /// <summary>
        /// Test 10: Node marker ID lookup
        /// </summary>
        public void Test_MarkerIDLookup()
        {
            Debug.Log("[Test] Starting: Marker ID Lookup");

            if (m_TestGraph == null)
            {
                Debug.LogError("? FAIL: No graph loaded");
                return;
            }

            bool foundMarker = false;
            foreach (var node in m_TestGraph.Nodes)
            {
                if (!string.IsNullOrEmpty(node.MarkerId))
                {
                    var found = m_TestGraph.FindNodeByMarkerId(node.MarkerId);
                    if (found != null && found.Id == node.Id)
                    {
                        foundMarker = true;
                        Debug.Log($"? PASS: Marker '{node.MarkerId}' maps to node '{node.Name}'");
                        break;
                    }
                }
            }

            if (!foundMarker)
            {
                Debug.LogWarning("? WARN: No markers found in graph");
            }
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        public void RunAllTests()
        {
            Debug.Log("\n" + new string('=', 50));
            Debug.Log("RUNNING NAVIGATION SYSTEM TESTS");
            Debug.Log(new string('=', 50) + "\n");

            Test_LoadNavigationGraph();
            Test_ValidateGraphIntegrity();
            Test_AStarPathfinding();
            Test_FindPointsOfInterest();
            Test_QRCodeDataValidation();
            Test_NavigationNodeConnections();
            Test_LocalizationCalibration();
            Test_PathCostCalculation();
            Test_GraphMetadata();
            Test_MarkerIDLookup();

            Debug.Log("\n" + new string('=', 50));
            Debug.Log("TEST SUITE COMPLETE");
            Debug.Log(new string('=', 50) + "\n");
        }
    }
}
