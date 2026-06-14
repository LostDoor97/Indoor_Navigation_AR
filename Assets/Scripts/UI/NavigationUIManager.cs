using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IndoorNavigation.UI
{
    /// <summary>
    /// Main navigation UI controller managing destination selection and status display
    /// </summary>
    public class NavigationUIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject m_MainPanel;
        [SerializeField] private GameObject m_DestinationListPanel;
        [SerializeField] private GameObject m_NavigationPanel;

        [Header("Destination Selection")]
        [SerializeField] private Transform m_DestinationListContent;
        [SerializeField] private GameObject m_DestinationItemPrefab;
        [SerializeField] private TMP_InputField m_SearchField;

        [Header("Navigation Display")]
        [SerializeField] private TextMeshProUGUI m_DestinationNameText;
        [SerializeField] private TextMeshProUGUI m_DistanceText;
        [SerializeField] private TextMeshProUGUI m_StatusText;
        [SerializeField] private Image m_CalibrationIndicator;

        [Header("Buttons")]
        [SerializeField] private Button m_StartNavigationButton;
        [SerializeField] private Button m_CancelNavigationButton;
        [SerializeField] private Button m_RecalibrateButton;

        private List<Navigation.NavigationNode> m_CurrentPOIs;
        private Navigation.NavigationNode m_SelectedDestination;
        private bool m_IsNavigating;

        // Events
        public delegate void DestinationSelectedDelegate(Navigation.NavigationNode destination);
        public event DestinationSelectedDelegate OnDestinationSelected;

        public delegate void NavigationStartedDelegate(Navigation.NavigationNode destination);
        public event NavigationStartedDelegate OnNavigationStarted;

        public delegate void NavigationCancelledDelegate();
        public event NavigationCancelledDelegate OnNavigationCancelled;

        private void OnEnable()
        {
            if (m_StartNavigationButton != null)
                m_StartNavigationButton.onClick.AddListener(OnStartNavigationClicked);
            if (m_CancelNavigationButton != null)
                m_CancelNavigationButton.onClick.AddListener(OnCancelNavigationClicked);
            if (m_RecalibrateButton != null)
                m_RecalibrateButton.onClick.AddListener(OnRecalibrateClicked);
            if (m_SearchField != null)
                m_SearchField.onValueChanged.AddListener(OnSearchValueChanged);
        }

        private void OnDisable()
        {
            if (m_StartNavigationButton != null)
                m_StartNavigationButton.onClick.RemoveListener(OnStartNavigationClicked);
            if (m_CancelNavigationButton != null)
                m_CancelNavigationButton.onClick.RemoveListener(OnCancelNavigationClicked);
            if (m_RecalibrateButton != null)
                m_RecalibrateButton.onClick.RemoveListener(OnRecalibrateClicked);
            if (m_SearchField != null)
                m_SearchField.onValueChanged.RemoveListener(OnSearchValueChanged);
        }

        /// <summary>
        /// Initialize UI with list of destinations
        /// </summary>
        public void InitializeDestinationList(List<Navigation.NavigationNode> pois)
        {
            m_CurrentPOIs = pois;
            RefreshDestinationList(pois);
            ShowMainPanel();
        }

        /// <summary>
        /// Refresh the destination list display
        /// </summary>
        private void RefreshDestinationList(List<Navigation.NavigationNode> pois)
        {
            // Clear existing items
            foreach (Transform child in m_DestinationListContent)
            {
                Destroy(child.gameObject);
            }

            // Create UI items for each POI
            foreach (var poi in pois)
            {
                var itemGo = Instantiate(m_DestinationItemPrefab, m_DestinationListContent);
                var itemUI = itemGo.GetComponent<DestinationListItem>();
                if (itemUI != null)
                {
                    itemUI.Initialize(poi, OnDestinationItemClicked);
                }
            }
        }

        /// <summary>
        /// Handle destination item click
        /// </summary>
        private void OnDestinationItemClicked(Navigation.NavigationNode destination)
        {
            m_SelectedDestination = destination;
            OnDestinationSelected?.Invoke(destination);
            ShowNavigationConfirmPanel();
        }

        /// <summary>
        /// Search field value changed
        /// </summary>
        private void OnSearchValueChanged(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                RefreshDestinationList(m_CurrentPOIs);
            }
            else
            {
                var filtered = m_CurrentPOIs.FindAll(p =>
                    p.Name.ToLower().Contains(searchText.ToLower()) ||
                    p.Category.ToLower().Contains(searchText.ToLower())
                );
                RefreshDestinationList(filtered);
            }
        }

        /// <summary>
        /// Start navigation button clicked
        /// </summary>
        private void OnStartNavigationClicked()
        {
            if (m_SelectedDestination != null)
            {
                m_IsNavigating = true;
                OnNavigationStarted?.Invoke(m_SelectedDestination);
                UpdateNavigationDisplay();
                ShowNavigationPanel();
                Debug.Log($"[Navigation UI] Started navigation to {m_SelectedDestination.Name}");
            }
        }

        /// <summary>
        /// Cancel navigation button clicked
        /// </summary>
        private void OnCancelNavigationClicked()
        {
            m_IsNavigating = false;
            OnNavigationCancelled?.Invoke();
            ShowMainPanel();
            Debug.Log("[Navigation UI] Navigation cancelled");
        }

        /// <summary>
        /// Recalibrate button clicked
        /// </summary>
        private void OnRecalibrateClicked()
        {
            if (m_StatusText != null)
            {
                m_StatusText.text = "Please scan a QR code to recalibrate...";
            }
            Debug.Log("[Navigation UI] Recalibration requested");
        }

        /// <summary>
        /// Update navigation status display
        /// </summary>
        public void UpdateNavigationDisplay(string destination = null, float distance = 0, string status = "Navigating...")
        {
            if (m_DestinationNameText != null && destination != null)
            {
                m_DestinationNameText.text = destination;
            }

            if (m_DistanceText != null && distance > 0)
            {
                m_DistanceText.text = $"Distance: {distance:F1}m";
            }

            if (m_StatusText != null)
            {
                m_StatusText.text = status;
            }
        }

        /// <summary>
        /// Update calibration indicator
        /// </summary>
        public void UpdateCalibrationStatus(bool isCalibrated)
        {
            if (m_CalibrationIndicator != null)
            {
                m_CalibrationIndicator.color = isCalibrated ? Color.green : Color.red;
            }
        }

        /// <summary>
        /// Show main panel
        /// </summary>
        private void ShowMainPanel()
        {
            if (m_MainPanel != null)
                m_MainPanel.SetActive(true);
            if (m_DestinationListPanel != null)
                m_DestinationListPanel.SetActive(true);
            if (m_NavigationPanel != null)
                m_NavigationPanel.SetActive(false);
        }

        /// <summary>
        /// Show navigation confirmation panel
        /// </summary>
        private void ShowNavigationConfirmPanel()
        {
            if (m_MainPanel != null)
                m_MainPanel.SetActive(true);
            if (m_DestinationListPanel != null)
                m_DestinationListPanel.SetActive(false);
        }

        /// <summary>
        /// Show active navigation panel
        /// </summary>
        private void ShowNavigationPanel()
        {
            if (m_MainPanel != null)
                m_MainPanel.SetActive(true);
            if (m_NavigationPanel != null)
                m_NavigationPanel.SetActive(true);
        }

        /// <summary>
        /// Check if currently navigating
        /// </summary>
        public bool IsNavigating => m_IsNavigating;

        /// <summary>
        /// Get selected destination
        /// </summary>
        public Navigation.NavigationNode GetSelectedDestination()
        {
            return m_SelectedDestination;
        }
    }
}
