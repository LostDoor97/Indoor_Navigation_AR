using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndoorNavigation.Navigation
{
    /// <summary>
    /// Represents a node (intersection/waypoint) in the navigation graph
    /// </summary>
    [System.Serializable]
    public class NavigationNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public string Id;

        /// <summary>
        /// Display name for this node (e.g., "Main Entrance", "Room 101")
        /// </summary>
        public string Name;

        /// <summary>
        /// 3D world position of this node
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// QR code or image marker associated with this node
        /// </summary>
        public string MarkerId;

        /// <summary>
        /// Whether this node is a point of interest (destination)
        /// </summary>
        public bool IsPointOfInterest;

        /// <summary>
        /// Category of the POI (e.g., "Entrance", "Restroom", "Office")
        /// </summary>
        public string Category;

        /// <summary>
        /// Additional metadata (floor number, description, etc.)
        /// </summary>
        public Dictionary<string, string> Metadata;

        /// <summary>
        /// IDs of adjacent nodes connected by corridors
        /// </summary>
        public List<string> ConnectedNodeIds;

        /// <summary>
        /// Cost/distance to traverse to each connected node
        /// </summary>
        public List<float> EdgeCosts;

        public NavigationNode()
        {
            Metadata = new Dictionary<string, string>();
            ConnectedNodeIds = new List<string>();
            EdgeCosts = new List<float>();
        }

        public NavigationNode(string id, string name, Vector3 position)
        {
            Id = id;
            Name = name;
            Position = position;
            Metadata = new Dictionary<string, string>();
            ConnectedNodeIds = new List<string>();
            EdgeCosts = new List<float>();
        }

        /// <summary>
        /// Add a connection to another node
        /// </summary>
        public void AddConnection(string targetNodeId, float cost)
        {
            if (!ConnectedNodeIds.Contains(targetNodeId))
            {
                ConnectedNodeIds.Add(targetNodeId);
                EdgeCosts.Add(cost);
            }
        }

        /// <summary>
        /// Remove a connection to another node
        /// </summary>
        public void RemoveConnection(string targetNodeId)
        {
            int index = ConnectedNodeIds.IndexOf(targetNodeId);
            if (index >= 0)
            {
                ConnectedNodeIds.RemoveAt(index);
                EdgeCosts.RemoveAt(index);
            }
        }
    }
}
