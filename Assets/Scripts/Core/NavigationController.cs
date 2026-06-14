using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndoorNavigation.Core
{
    /// <summary>
    /// Main controller that orchestrates all navigation components
    /// </summary>
    public class NavigationController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private QRCode.QRCodeScanner m_QRScanner;
        [SerializeField] private Localization.LocalizationManager m_LocalizationManager;
        [SerializeField] private ARRendering.PathRenderer m_PathRenderer;
        [SerializeField] private UI.NavigationUIManager m_UIManager;
        [SerializeField] private Database.NavigationDatabase m_Database;

        [Header("Configuration")]
        [SerializeField] private string m_NavigationGraphFileName = "navigation_graph.json";
        [SerializeField] private float m_RecalibrationCheckInterval = 1f;
        [SerializeField] private float m_WaypointArrivalDistance = 1f;

        private Navigation.NavigationGraph m_NavigationGraph;
        private Pathfinding.AStarPathfinder m_Pathfinder;
        private Navigation.NavigationNode m_CurrentDestination;
        private List<string> m_CurrentPath;
        private bool m_IsNavigating;
        private float m_LastRecalibrationCheckTime;

        private void Start()
        {
            InitializeSystem();
        }

        private void Update()
        {
            if (!m_IsNavigating)
                return;

            // Periodically check if recalibration is needed
            if (Time.time - m_LastRecalibrationCheckTime >= m_RecalibrationCheckInterval)
            {
                if (m_LocalizationManager.NeedsRecalibration())
                {
                    m_UIManager.UpdateNavigationDisplay(status: "Recalibration needed. Scan QR code...");
                }
                else
                {
                    UpdateNavigationProgress();
                }
                m_LastRecalibrationCheckTime = Time.time;
            }
        }

        /// <summary>
        /// Initialize all components and load navigation graph
        /// </summary>
        private void InitializeSystem()
        {
            Debug.Log("[Navigation Controller] Initializing indoor navigation system...");

            // Ensure all required components are present
            if (m_Database == null)
                m_Database = FindObjectOfType<Database.NavigationDatabase>();
            if (m_QRScanner == null)
                m_QRScanner = FindObjectOfType<QRCode.QRCodeScanner>();
            if (m_LocalizationManager == null)
                m_LocalizationManager = FindObjectOfType<Localization.LocalizationManager>();
            if (m_PathRenderer == null)
                m_PathRenderer = FindObjectOfType<ARRendering.PathRenderer>();
            if (m_UIManager == null)
                m_UIManager = FindObjectOfType<UI.NavigationUIManager>();

            // Load navigation graph
            if (m_Database != null)
            {
                m_NavigationGraph = m_Database.LoadGraphFromJSON(m_NavigationGraphFileName);

                if (m_NavigationGraph == null)
                {
                    Debug.LogError("[Navigation Controller] Failed to load navigation graph");
                    return;
                }

                // Initialize pathfinder
                m_Pathfinder = new Pathfinding.AStarPathfinder(m_NavigationGraph);

                // Set graph for path renderer
                if (m_PathRenderer != null)
                {
                    m_PathRenderer.SetNavigationGraph(m_NavigationGraph);
                }

                // Initialize UI with POIs
                var pois = m_NavigationGraph.GetAllPointsOfInterest();
                if (m_UIManager != null)
                {
                    m_UIManager.InitializeDestinationList(pois);
                }
            }

            // Hook up event listeners
            if (m_QRScanner != null)
            {
                m_QRScanner.OnQRCodeDetected += HandleQRCodeDetected;
            }

            if (m_UIManager != null)
            {
                m_UIManager.OnDestinationSelected += HandleDestinationSelected;
                m_UIManager.OnNavigationStarted += HandleNavigationStarted;
                m_UIManager.OnNavigationCancelled += HandleNavigationCancelled;
            }

            if (m_LocalizationManager != null)
            {
                m_LocalizationManager.OnRecalibrated += HandleRecalibrated;
                m_LocalizationManager.OnCalibrationStatusChanged += HandleCalibrationStatusChanged;
            }

            // Start QR scanning
            if (m_QRScanner != null)
            {
                m_QRScanner.StartScanning();
                Debug.Log("[Navigation Controller] QR scanning started");
            }

            Debug.Log("[Navigation Controller] Initialization complete");
        }

        /// <summary>
        /// Handle QR code detection
        /// </summary>
        private void HandleQRCodeDetected(QRCode.QRCodeData qrData)
        {
            if (!qrData.IsValid || m_NavigationGraph == null)
                return;

            Debug.Log($"[Navigation Controller] QR Code detected: {qrData.Content}");

            // Find corresponding node
            var detectedNode = m_NavigationGraph.FindNodeByMarkerId(qrData.Content);

            if (detectedNode == null)
            {
                Debug.LogWarning($"[Navigation Controller] No navigation node found for marker: {qrData.Content}");
                return;
            }

            // Recalibrate position
            if (m_LocalizationManager != null)
            {
                m_LocalizationManager.RecalibratePosition(detectedNode, qrData.Position);
            }

            // If navigating, recalculate path
            if (m_IsNavigating && m_CurrentDestination != null)
            {
                CalculateAndRenderPath(detectedNode, m_CurrentDestination);
            }
        }

        /// <summary>
        /// Handle destination selection from UI
        /// </summary>
        private void HandleDestinationSelected(Navigation.NavigationNode destination)
        {
            m_CurrentDestination = destination;
            Debug.Log($"[Navigation Controller] Destination selected: {destination.Name}");
        }

        /// <summary>
        /// Handle navigation start
        /// </summary>
        private void HandleNavigationStarted(Navigation.NavigationNode destination)
        {
            m_CurrentDestination = destination;
            m_IsNavigating = true;
            m_LastRecalibrationCheckTime = Time.time;

            // Check if user is calibrated
            if (!m_LocalizationManager.IsCalibrated)
            {
                m_UIManager.UpdateNavigationDisplay(status: "Please scan a QR code to calibrate position...");
                return;
            }

            // Calculate path from current position to destination
            var currentNode = m_LocalizationManager.GetCurrentNode();
            if (currentNode != null)
            {
                CalculateAndRenderPath(currentNode, destination);
            }

            Debug.Log($"[Navigation Controller] Navigation started to {destination.Name}");
        }

        /// <summary>
        /// Handle navigation cancellation
        /// </summary>
        private void HandleNavigationCancelled()
        {
            m_IsNavigating = false;
            m_CurrentDestination = null;
            m_CurrentPath = null;

            if (m_PathRenderer != null)
            {
                m_PathRenderer.ClearPath();
            }

            Debug.Log("[Navigation Controller] Navigation cancelled");
        }

        /// <summary>
        /// Handle recalibration event
        /// </summary>
        private void HandleRecalibrated(Navigation.NavigationNode newNode, Vector3 newPosition)
        {
            Debug.Log($"[Navigation Controller] Recalibrated at {newNode.Name}");

            // If navigating, recalculate path
            if (m_IsNavigating && m_CurrentDestination != null)
            {
                CalculateAndRenderPath(newNode, m_CurrentDestination);
            }
        }

        /// <summary>
        /// Handle calibration status change
        /// </summary>
        private void HandleCalibrationStatusChanged(bool isCalibrated)
        {
            if (m_UIManager != null)
            {
                m_UIManager.UpdateCalibrationStatus(isCalibrated);
            }
        }

        /// <summary>
        /// Calculate path using A* and render it
        /// </summary>
        private void CalculateAndRenderPath(Navigation.NavigationNode startNode, Navigation.NavigationNode goalNode)
        {
            if (m_Pathfinder == null)
                return;

            m_CurrentPath = m_Pathfinder.FindPath(startNode.Id, goalNode.Id);

            if (m_CurrentPath == null || m_CurrentPath.Count == 0)
            {
                Debug.LogError($"[Navigation Controller] No path found from {startNode.Name} to {goalNode.Name}");
                m_UIManager.UpdateNavigationDisplay(status: "No route available");
                return;
            }

            // Render the path
            if (m_PathRenderer != null)
            {
                m_PathRenderer.RenderPath(m_CurrentPath);
            }

            float pathCost = m_Pathfinder.GetPathCost(m_CurrentPath);
            m_UIManager.UpdateNavigationDisplay(
                destination: goalNode.Name,
                distance: pathCost,
                status: $"Route calculated: {m_CurrentPath.Count} waypoints"
            );

            Debug.Log($"[Navigation Controller] Path calculated with {m_CurrentPath.Count} waypoints, distance: {pathCost}m");
        }

        /// <summary>
        /// Update navigation progress (check waypoint arrival, etc.)
        /// </summary>
        private void UpdateNavigationProgress()
        {
            if (m_CurrentPath == null || m_CurrentPath.Count == 0)
                return;

            var currentPos = m_LocalizationManager.GetCurrentPosition();
            var nextWaypoint = m_PathRenderer.GetNextWaypoint();

            if (nextWaypoint.HasValue)
            {
                float distanceToWaypoint = Vector3.Distance(currentPos, nextWaypoint.Value);

                // Update UI
                m_UIManager.UpdateNavigationDisplay(
                    destination: m_CurrentDestination.Name,
                    distance: distanceToWaypoint,
                    status: $"Follow the arrows ({distanceToWaypoint:F1}m)"
                );

                // Check if reached waypoint
                if (distanceToWaypoint < m_WaypointArrivalDistance)
                {
                    m_PathRenderer.AdvanceToNextWaypoint();

                    // Check if reached destination
                    if (m_PathRenderer.GetCurrentWaypointIndex() >= m_PathRenderer.GetWaypointCount() - 1)
                    {
                        HandleDestinationReached();
                    }
                    else
                    {
                        Debug.Log("[Navigation Controller] Waypoint reached, advancing...");
                    }
                }
            }
        }

        /// <summary>
        /// Handle arrival at destination
        /// </summary>
        private void HandleDestinationReached()
        {
            m_IsNavigating = false;
            m_UIManager.UpdateNavigationDisplay(status: "Destination reached!");
            if (m_PathRenderer != null)
            {
                m_PathRenderer.ClearPath();
            }
            Debug.Log($"[Navigation Controller] Destination reached: {m_CurrentDestination.Name}");
        }

        /// <summary>
        /// Get current navigation state
        /// </summary>
        public bool IsNavigating => m_IsNavigating;

        /// <summary>
        /// Get navigation graph
        /// </summary>
        public Navigation.NavigationGraph GetNavigationGraph()
        {
            return m_NavigationGraph;
        }

        /// <summary>
        /// Get current path
        /// </summary>
        public List<string> GetCurrentPath()
        {
            return m_CurrentPath;
        }
    }
}
