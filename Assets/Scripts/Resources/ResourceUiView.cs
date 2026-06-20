using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameResources
{
    [AddComponentMenu("Room Horror/Resources/Resource UI View")]
    public class ResourceUiView : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField] private ResourceType _resourceType = ResourceType.None;
        [SerializeField] private ResourceUiDefinitionSet _definitionSet;
        [SerializeField] private string _displayNameOverride;
        [SerializeField] private bool _updateAmountFromManager = true;
        [SerializeField] private int _fallbackAmount;

        [Header("UI")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _displayNameText;
        [SerializeField] private TMP_Text _amountText;

        [Header("Display Name Size")]
        [SerializeField] private bool _autoResizeDisplayNameWidth = true;
        [SerializeField] private float _displayNameHorizontalPadding = 16f;
        [SerializeField] private float _displayNameMinWidth = 0f;
        [SerializeField] private float _displayNameMaxWidth = 300f;
        [SerializeField] private RectTransform _displayNameWidthTarget;
        [SerializeField] private LayoutElement _displayNameLayoutElement;

        private CollectedResourcesManager _resourcesManager;

        public ResourceType ResourceType => _resourceType;

        private void OnEnable()
        {
            TryBindResourcesManager();
            Refresh();
        }

        private void Start()
        {
            TryBindResourcesManager();
            RefreshAmount();
        }

        private void OnDisable()
        {
            UnbindResourcesManager();
        }

        public void SetResourceType(ResourceType resourceType)
        {
            if (_resourceType == resourceType)
            {
                return;
            }

            _resourceType = resourceType;
            Refresh();
        }

        public void Refresh()
        {
            ApplyData();
            RefreshAmount();
        }

        public void SetDefinitionSet(ResourceUiDefinitionSet definitionSet)
        {
            if (_definitionSet == definitionSet)
            {
                return;
            }

            _definitionSet = definitionSet;
            ApplyData();
        }

        public void SetDisplayNameOverride(string displayNameOverride)
        {
            _displayNameOverride = displayNameOverride;
            ApplyData();
        }

        public void RefreshAmount()
        {
            int amount = _fallbackAmount;
            if (_updateAmountFromManager && CollectedResourcesManager.HasInstance)
            {
                amount = CollectedResourcesManager.Instance.GetAmount(_resourceType);
            }

            SetAmount(amount);
        }

        public void SetAmount(int amount)
        {
            _fallbackAmount = amount;
            if (_amountText != null)
            {
                _amountText.text = amount.ToString();
            }
        }

        [ContextMenu("Apply Data")]
        public void ApplyData()
        {
            Sprite sprite = null;
            string displayName = GetDisplayName();

            if (_definitionSet != null)
            {
                sprite = _definitionSet.GetSprite(_resourceType);
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = sprite;
                _iconImage.enabled = sprite != null;
            }

            if (_displayNameText != null)
            {
                _displayNameText.text = displayName;
                ResizeDisplayNameWidth();
            }
        }

        private string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(_displayNameOverride))
            {
                return _displayNameOverride;
            }

            if (_definitionSet != null)
            {
                return _definitionSet.GetDisplayName(_resourceType);
            }

            return _resourceType == ResourceType.None ? string.Empty : _resourceType.ToString();
        }

        [ContextMenu("Resize Display Name Width")]
        public void ResizeDisplayNameWidth()
        {
            if (!_autoResizeDisplayNameWidth || _displayNameText == null)
            {
                return;
            }

            _displayNameText.ForceMeshUpdate();
            float preferredWidth = _displayNameText.GetPreferredValues(_displayNameText.text).x + _displayNameHorizontalPadding;
            float targetWidth = Mathf.Clamp(preferredWidth, _displayNameMinWidth, _displayNameMaxWidth);

            if (_displayNameLayoutElement != null)
            {
                _displayNameLayoutElement.preferredWidth = targetWidth;
                return;
            }

            RectTransform widthTarget = _displayNameWidthTarget != null
                ? _displayNameWidthTarget
                : _displayNameText.rectTransform;

            if (widthTarget != null)
            {
                widthTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            }
        }

        private void TryBindResourcesManager()
        {
            if (!_updateAmountFromManager || _resourcesManager != null || !CollectedResourcesManager.HasInstance)
            {
                return;
            }

            _resourcesManager = CollectedResourcesManager.Instance;
            _resourcesManager.ResourceAmountChanged += OnResourceAmountChanged;
        }

        private void UnbindResourcesManager()
        {
            if (_resourcesManager == null)
            {
                return;
            }

            _resourcesManager.ResourceAmountChanged -= OnResourceAmountChanged;
            _resourcesManager = null;
        }

        private void OnResourceAmountChanged(ResourceType resourceType, int newAmount, int delta)
        {
            if (resourceType == _resourceType)
            {
                SetAmount(newAmount);
            }
        }

        private void OnValidate()
        {
            _displayNameMinWidth = Mathf.Max(0f, _displayNameMinWidth);
            _displayNameMaxWidth = Mathf.Max(_displayNameMinWidth, _displayNameMaxWidth);
            _displayNameHorizontalPadding = Mathf.Max(0f, _displayNameHorizontalPadding);

            if (_displayNameText != null)
            {
                if (_displayNameWidthTarget == null)
                {
                    _displayNameWidthTarget = _displayNameText.rectTransform;
                }

                if (_displayNameLayoutElement == null)
                {
                    _displayNameLayoutElement = _displayNameText.GetComponent<LayoutElement>();
                }
            }

            ApplyData();

            if (_amountText != null)
            {
                _amountText.text = _fallbackAmount.ToString();
            }
        }
    }
}
