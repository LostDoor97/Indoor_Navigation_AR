using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Core = IndoorNavigation.Core;
using Database = IndoorNavigation.Database;
using Localization = IndoorNavigation.Localization;
using QRCode = IndoorNavigation.QRCode;
using UI = IndoorNavigation.UI;

/// <summary>
/// Helper component to quickly set up the navigation system on startup
/// Attach this to a GameObject in your scene and it will initialize everything
/// </summary>
public class NavigationSystemSetup : MonoBehaviour
    {
        [SerializeField] private bool m_AutoInitialize = true;
        [SerializeField] private bool m_ShowDebugInfo = true;

        private void Start()
        {
            if (m_AutoInitialize)
            {
                InitializeNavigationSystem();
            }
        }

        /// <summary>
        /// Initialize the complete navigation system
        /// </summary>
        public void InitializeNavigationSystem()
        {
            Debug.Log("[Navigation Setup] Starting system initialization...");

            // Ensure AR Foundation components are properly configured
            SetupARComponents();

            // Ensure all navigation components exist
            EnsureNavigationComponentsExist();

            // Verify navigation graph exists
            VerifyNavigationGraph();

            Debug.Log("[Navigation Setup] System initialization complete");
        }

        /// <summary>
        /// Setup AR Foundation components
        /// </summary>
        private void SetupARComponents()
        {
            var arSession = FindObjectOfType<ARSession>();
            if (arSession == null)
            {
                Debug.LogWarning("[Navigation Setup] ARSession not found in scene");
            }
            else
            {
                Debug.Log("[Navigation Setup] ARSession configured");
            }

            var arCameraManager = FindObjectOfType<ARCameraManager>();
            if (arCameraManager == null)
            {
                Debug.LogWarning("[Navigation Setup] ARCameraManager not found");
            }
            else
            {
                Debug.Log("[Navigation Setup] ARCameraManager configured");
            }
        }

        /// <summary>
        /// Ensure all navigation components are in the scene
        /// </summary>
        private void EnsureNavigationComponentsExist()
        {
            if (FindObjectOfType<Database.NavigationDatabase>() == null)
            {
                Debug.LogWarning("[Navigation Setup] Creating NavigationDatabase...");
                var dbGo = new GameObject("NavigationDatabase");
                dbGo.AddComponent<Database.NavigationDatabase>();
            }

            if (FindObjectOfType<Core.NavigationController>() == null)
            {
                Debug.LogWarning("[Navigation Setup] Creating NavigationController...");
                var controllerGo = new GameObject("NavigationController");
                controllerGo.AddComponent<Core.NavigationController>();
            }

            if (FindObjectOfType<Localization.LocalizationManager>() == null)
            {
                Debug.LogWarning("[Navigation Setup] Creating LocalizationManager...");
                var locGo = new GameObject("LocalizationManager");
                locGo.AddComponent<Localization.LocalizationManager>();
            }

            if (FindObjectOfType<QRCode.QRCodeScanner>() == null)
            {
                Debug.LogWarning("[Navigation Setup] Creating QRCodeScanner...");
                var scannerGo = new GameObject("QRCodeScanner");
                scannerGo.AddComponent<QRCode.QRCodeScanner>();
            }

            if (FindObjectOfType<UI.NavigationUIManager>() == null)
            {
                Debug.LogWarning("[Navigation Setup] Creating NavigationUIManager...");
                var uiGo = new GameObject("NavigationUIManager");
                uiGo.AddComponent<UI.NavigationUIManager>();
            }

            Debug.Log("[Navigation Setup] All components verified");
        }

        /// <summary>
        /// Verify navigation graph is properly set up
        /// </summary>
        private void VerifyNavigationGraph()
        {
            var database = FindObjectOfType<Database.NavigationDatabase>();
            if (database == null)
                return;

            var graph = database.LoadGraphFromJSON("navigation_graph.json");
            if (graph == null)
            {
                Debug.LogError("[Navigation Setup] Failed to load navigation_graph.json from StreamingAssets");
                return;
            }

            if (!graph.ValidateIntegrity())
            {
                Debug.LogError("[Navigation Setup] Navigation graph integrity check failed");
                return;
            }

            var pois = graph.GetAllPointsOfInterest();
            Debug.Log($"[Navigation Setup] Navigation graph loaded: {graph.Nodes.Count} nodes, {pois.Count} POIs");
        }

        /// <summary>
        /// Print debug information to console
        /// </summary>
        public void PrintDebugInfo()
        {
            if (!m_ShowDebugInfo)
                return;

            Debug.Log("=== Navigation System Debug Info ===");

            var controller = FindObjectOfType<Core.NavigationController>();
            if (controller != null)
            {
                var graph = controller.GetNavigationGraph();
                if (graph != null)
                {
                    Debug.Log($"Graph: {graph.BuildingName}");
                    Debug.Log($"Nodes: {graph.Nodes.Count}");
                    Debug.Log($"POIs: {graph.GetAllPointsOfInterest().Count}");
                }
            }

            var localization = FindObjectOfType<Localization.LocalizationManager>();
            if (localization != null)
            {
                Debug.Log($"Calibrated: {localization.IsCalibrated}");
                Debug.Log($"Position: {localization.GetCurrentPosition()}");
            }

            Debug.Log("====================================");
        }
    }
