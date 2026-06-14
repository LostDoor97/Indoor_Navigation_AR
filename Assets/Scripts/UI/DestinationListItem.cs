using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IndoorNavigation.UI
{
    /// <summary>
    /// Individual destination list item UI component
    /// </summary>
    public class DestinationListItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_NameText;
        [SerializeField] private TextMeshProUGUI m_CategoryText;
        [SerializeField] private Button m_SelectButton;
        [SerializeField] private Image m_IconImage;

        private Navigation.NavigationNode m_Data;
        private System.Action<Navigation.NavigationNode> m_OnClickCallback;

        private void OnEnable()
        {
            if (m_SelectButton != null)
                m_SelectButton.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (m_SelectButton != null)
                m_SelectButton.onClick.RemoveListener(OnButtonClicked);
        }

        /// <summary>
        /// Initialize this list item with POI data
        /// </summary>
        public void Initialize(Navigation.NavigationNode node, System.Action<Navigation.NavigationNode> onClickCallback)
        {
            m_Data = node;
            m_OnClickCallback = onClickCallback;

            if (m_NameText != null)
                m_NameText.text = node.Name;

            if (m_CategoryText != null)
                m_CategoryText.text = node.Category ?? "Unknown";
        }

        /// <summary>
        /// Handle button click
        /// </summary>
        private void OnButtonClicked()
        {
            m_OnClickCallback?.Invoke(m_Data);
        }

        /// <summary>
        /// Get associated navigation node
        /// </summary>
        public Navigation.NavigationNode GetData()
        {
            return m_Data;
        }
    }
}
