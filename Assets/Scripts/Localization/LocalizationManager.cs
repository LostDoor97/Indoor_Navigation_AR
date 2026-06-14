using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace IndoorNavigation.Localization
{
    /// <summary>
    /// Manages user position and orientation tracking with recalibration via QR codes
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        [SerializeField] private ARSession m_ARSession;
        [SerializeField] private ARAnchorManager m_AnchorManager;
        [SerializeField] private float m_RecalibrationDistance = 0.3f; // 30cm as per requirements

        private Vector3 m_CurrentPosition;
        private Quaternion m_CurrentOrientation;
        private Vector3 m_PositionOffset;
        private Navigation.NavigationNode m_CurrentNode;
        private bool m_IsCalibrated;
        private float m_LastRecalibrationTime;

        // Events
        public delegate void RecalibrationDelegate(Navigation.NavigationNode newNode, Vector3 newPosition);
        public event RecalibrationDelegate OnRecalibrated;

        public delegate void CalibrationStatusDelegate(bool isCalibrated);
        public event CalibrationStatusDelegate OnCalibrationStatusChanged;

        private void Start()
        {
            m_CurrentPosition = Vector3.zero;
            m_CurrentOrientation = Quaternion.identity;
            m_PositionOffset = Vector3.zero;
            m_IsCalibrated = false;
        }

        private void Update()
        {
            // Continuously update position from ARCore tracking
            if (m_ARSession != null && ARSession.state == ARSessionState.SessionTracking)
            {
                UpdateTracking();
            }
        }

        /// <summary>
        /// Update current position from AR camera
        /// </summary>
        private void UpdateTracking()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                m_CurrentPosition = mainCamera.transform.position + m_PositionOffset;
                m_CurrentOrientation = mainCamera.transform.rotation;
            }
        }

        /// <summary>
        /// Recalibrate position based on detected QR code
        /// </summary>
        public void RecalibratePosition(Navigation.NavigationNode detectedNode, Vector3 markerWorldPosition)
        {
            if (detectedNode == null)
            {
                Debug.LogWarning("[Localization Manager] Cannot recalibrate with null node");
                return;
            }

            // Calculate offset between marker position and node position
            m_PositionOffset = detectedNode.Position - markerWorldPosition;

            // Update current node
            m_CurrentNode = detectedNode;
            m_CurrentPosition = detectedNode.Position;
            m_IsCalibrated = true;
            m_LastRecalibrationTime = Time.time;

            // Fire event
            OnRecalibrated?.Invoke(m_CurrentNode, m_CurrentPosition);
            OnCalibrationStatusChanged?.Invoke(true);

            Debug.Log($"[Localization Manager] Recalibrated at node: {detectedNode.Name} ({detectedNode.Id})");
        }

        /// <summary>
        /// Check if user has drifted from current position and needs recalibration
        /// </summary>
        public bool NeedsRecalibration()
        {
            if (m_CurrentNode == null || !m_IsCalibrated)
                return true;

            float distanceFromNode = Vector3.Distance(m_CurrentPosition, m_CurrentNode.Position);
            return distanceFromNode > m_RecalibrationDistance;
        }

        /// <summary>
        /// Get the current estimated position
        /// </summary>
        public Vector3 GetCurrentPosition()
        {
            return m_CurrentPosition;
        }

        /// <summary>
        /// Get current navigation node
        /// </summary>
        public Navigation.NavigationNode GetCurrentNode()
        {
            return m_CurrentNode;
        }

        /// <summary>
        /// Get current orientation
        /// </summary>
        public Quaternion GetCurrentOrientation()
        {
            return m_CurrentOrientation;
        }

        /// <summary>
        /// Check if user is currently calibrated
        /// </summary>
        public bool IsCalibrated => m_IsCalibrated;

        /// <summary>
        /// Get time since last recalibration in seconds
        /// </summary>
        public float GetTimeSinceLastRecalibration()
        {
            return Time.time - m_LastRecalibrationTime;
        }

        /// <summary>
        /// Set a manual calibration point
        /// </summary>
        public void SetManualCalibration(Vector3 position, Navigation.NavigationNode node)
        {
            m_CurrentPosition = position;
            m_CurrentNode = node;
            m_IsCalibrated = true;
            m_LastRecalibrationTime = Time.time;
            OnCalibrationStatusChanged?.Invoke(true);
            Debug.Log($"[Localization Manager] Manual calibration set at {position}");
        }

        /// <summary>
        /// Reset calibration
        /// </summary>
        public void ResetCalibration()
        {
            m_IsCalibrated = false;
            m_CurrentNode = null;
            m_PositionOffset = Vector3.zero;
            OnCalibrationStatusChanged?.Invoke(false);
            Debug.Log("[Localization Manager] Calibration reset");
        }

        /// <summary>
        /// Create AR anchor at current position
        /// </summary>
        public ARAnchor CreateAnchorAtCurrentPosition()
        {
            if (m_AnchorManager == null)
            {
                Debug.LogWarning("[Localization Manager] ARAnchorManager not assigned");
                return null;
            }

            var pose = new Pose(m_CurrentPosition, m_CurrentOrientation);
            var anchorGO = new GameObject("AR Anchor");
            anchorGO.transform.position = pose.position;
            anchorGO.transform.rotation = pose.rotation;
            return anchorGO.AddComponent<ARAnchor>();
        }
    }
}
