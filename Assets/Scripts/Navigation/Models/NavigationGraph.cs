using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndoorNavigation.Navigation
{
    /// <summary>
    /// JSON-serializable container for the entire navigation graph
    /// </summary>
    [System.Serializable]
    public class NavigationGraph
    {
        /// <summary>
        /// Version of the graph format for compatibility checking
        /// </summary>
        public string Version = "1.0";

        /// <summary>
        /// Name/description of the building or area
        /// </summary>
        public string BuildingName;

        /// <summary>
        /// All nodes in the graph
        /// </summary>
        public List<NavigationNode> Nodes;

        /// <summary>
        /// Metadata about the graph (created date, author, etc.)
        /// </summary>
        public Dictionary<string, string> GraphMetadata;

        public NavigationGraph()
        {
            Nodes = new List<NavigationNode>();
            GraphMetadata = new Dictionary<string, string>();
        }

        public NavigationGraph(string buildingName)
        {
            BuildingName = buildingName;
            Nodes = new List<NavigationNode>();
            GraphMetadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Find a node by its ID
        /// </summary>
        public NavigationNode FindNodeById(string nodeId)
        {
            return Nodes.Find(n => n.Id == nodeId);
        }

        /// <summary>
        /// Find all POIs by category
        /// </summary>
        public List<NavigationNode> FindPointsOfInterestByCategory(string category)
        {
            return Nodes.FindAll(n => n.IsPointOfInterest && n.Category == category);
        }

        /// <summary>
        /// Find a node by its marker ID
        /// </summary>
        public NavigationNode FindNodeByMarkerId(string markerId)
        {
            return Nodes.Find(n => n.MarkerId == markerId);
        }

        /// <summary>
        /// Get all points of interest
        /// </summary>
        public List<NavigationNode> GetAllPointsOfInterest()
        {
            return Nodes.FindAll(n => n.IsPointOfInterest);
        }

        /// <summary>
        /// Add a node to the graph
        /// </summary>
        public void AddNode(NavigationNode node)
        {
            if (!Nodes.Exists(n => n.Id == node.Id))
            {
                Nodes.Add(node);
            }
        }

        /// <summary>
        /// Remove a node from the graph
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            Nodes.RemoveAll(n => n.Id == nodeId);
            // Remove all connections to this node
            foreach (var node in Nodes)
            {
                node.RemoveConnection(nodeId);
            }
        }

        /// <summary>
        /// Validate the integrity of the graph
        /// </summary>
        public bool ValidateIntegrity()
        {
            HashSet<string> nodeIds = new HashSet<string>();

            // Check for duplicate IDs
            foreach (var node in Nodes)
            {
                if (nodeIds.Contains(node.Id) || string.IsNullOrEmpty(node.Id))
                {
                    Debug.LogError($"[Navigation Graph] Duplicate or empty node ID: {node.Id}");
                    return false;
                }
                nodeIds.Add(node.Id);
            }

            // Check for references to non-existent nodes
            foreach (var node in Nodes)
            {
                foreach (var connectedId in node.ConnectedNodeIds)
                {
                    if (!nodeIds.Contains(connectedId))
                    {
                        Debug.LogError($"[Navigation Graph] Node {node.Id} references non-existent node {connectedId}");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
