using System.Collections.Generic;
using GameResources;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Renders an independent, live top-down view of the scene into a UI RawImage.
/// It intentionally has no dependency on the procedural level generator.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class RealtimeMinimapController : MonoBehaviour
{
    private enum MarkerKind
    {
        Character,
        Player,
        Resource,
        DungeonMaster
    }

    private sealed class MarkerView
    {
        public Transform Target;
        public RectTransform Rect;
        public Image Image;
        public MarkerKind Kind;
    }

    [Header("Output")]
    [SerializeField] private RawImage _output;
    [SerializeField, Range(64, 1024)] private int _textureResolution = 256;
    [SerializeField, Range(1f, 60f)] private float _refreshRate = 12f;
    [SerializeField] private Color _backgroundColor = new Color(0.055f, 0.06f, 0.07f, 1f);

    [Header("View")]
    [SerializeField] private Transform _explicitFollowTarget;
    [SerializeField, Min(1f)] private float _orthographicSize = 25f;
    [SerializeField, Min(2f)] private float _cameraHeight = 60f;
    [SerializeField, Min(0f)] private float _followSharpness = 12f;
    [SerializeField] private LayerMask _cullingMask = ~((1 << 5) | (1 << 6) | (1 << 10));

    [Header("Markers")]
    [SerializeField] private bool _showCharacters = true;
    [SerializeField] private bool _showResources = true;
    [SerializeField] private bool _showDungeonMaster = true;
    [SerializeField] private bool _clampMarkersToBorder = true;
    [SerializeField, Min(0.1f)] private float _markerDiscoveryInterval = 1f;
    [SerializeField, Min(2f)] private float _markerSize = 9f;
    [SerializeField] private Color _playerColor = new Color(0.25f, 1f, 0.35f, 1f);
    [SerializeField] private Color _characterColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color _resourceColor = new Color(1f, 0.78f, 0.15f, 1f);
    [SerializeField] private Color _dungeonMasterColor = new Color(1f, 0.2f, 0.25f, 1f);

    private readonly Dictionary<Transform, MarkerView> _markers = new Dictionary<Transform, MarkerView>();
    private readonly HashSet<Transform> _discoveredTargets = new HashSet<Transform>();
    private readonly List<Transform> _staleTargets = new List<Transform>();

    private RectTransform _viewport;
    private RectTransform _markerContainer;
    private RectMask2D _rectMask;
    private bool _ownsRectMask;

    private GameObject _cameraObject;
    private Camera _minimapCamera;
    private RenderTexture _renderTexture;
    private RenderPipeline.StandardRequest _renderRequest;
    private bool _continuousRenderFallback;

    private Transform _followTarget;
    private Vector3 _mapCenter;
    private bool _hasMapCenter;
    private float _nextRenderTime;
    private float _nextMarkerDiscoveryTime;
    private bool _initialized;

    public Camera MinimapCamera => _minimapCamera;
    public RenderTexture OutputTexture => _renderTexture;
    public Transform FollowTarget => _followTarget;

    private void Reset()
    {
        _output = GetComponent<RawImage>();
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        _nextRenderTime = 0f;
        _nextMarkerDiscoveryTime = 0f;
    }

    private void LateUpdate()
    {
        if (!_initialized)
        {
            return;
        }

        ResolveFollowTarget();
        UpdateCameraPosition();

        float now = Time.unscaledTime;
        if (now >= _nextMarkerDiscoveryTime)
        {
            DiscoverMarkers();
            _nextMarkerDiscoveryTime = now + _markerDiscoveryInterval;
        }

        UpdateMarkerPositions();

        if (!_continuousRenderFallback && now >= _nextRenderTime)
        {
            RenderNow();
            _nextRenderTime = now + 1f / _refreshRate;
        }
    }

    private void OnDisable()
    {
        ReleaseRuntimeResources();
    }

    private void OnDestroy()
    {
        ReleaseRuntimeResources();
    }

    private void OnValidate()
    {
        _textureResolution = Mathf.Clamp(_textureResolution, 64, 1024);
        _refreshRate = Mathf.Clamp(_refreshRate, 1f, 60f);
        _orthographicSize = Mathf.Max(1f, _orthographicSize);
        _cameraHeight = Mathf.Max(2f, _cameraHeight);
        _followSharpness = Mathf.Max(0f, _followSharpness);
        _markerDiscoveryInterval = Mathf.Max(0.1f, _markerDiscoveryInterval);
        _markerSize = Mathf.Max(2f, _markerSize);

        if (_minimapCamera != null)
        {
            ApplyCameraSettings();
        }
    }

    public void SetFollowTarget(Transform target)
    {
        _explicitFollowTarget = target;
        _followTarget = target;
        _hasMapCenter = false;
    }

    public void ClearExplicitFollowTarget()
    {
        _explicitFollowTarget = null;
        _followTarget = null;
        _hasMapCenter = false;
    }

    public void RenderNow()
    {
        if (!_initialized || _minimapCamera == null || _renderTexture == null)
        {
            return;
        }

        _renderRequest.destination = _renderTexture;

        if (RenderPipeline.SupportsRenderRequest(_minimapCamera, _renderRequest))
        {
            RenderPipeline.SubmitRenderRequest(_minimapCamera, _renderRequest);
            return;
        }

        // A non-SRP fallback keeps the feature functional, but renders every frame.
        _continuousRenderFallback = true;
        _minimapCamera.enabled = true;
    }

    private void EnsureInitialized()
    {
        if (_initialized || !isActiveAndEnabled)
        {
            return;
        }

        if (_output == null)
        {
            _output = GetComponent<RawImage>();
        }

        _viewport = _output.rectTransform;
        _output.raycastTarget = false;

        _rectMask = GetComponent<RectMask2D>();
        if (_rectMask == null)
        {
            _rectMask = gameObject.AddComponent<RectMask2D>();
            _ownsRectMask = true;
        }

        CreateMarkerContainer();
        CreateRenderTexture();
        CreateMinimapCamera();

        _output.texture = _renderTexture;
        _initialized = true;
    }

    private void CreateMarkerContainer()
    {
        GameObject markerObject = new GameObject("Realtime Minimap Markers", typeof(RectTransform));
        _markerContainer = markerObject.GetComponent<RectTransform>();
        _markerContainer.SetParent(_viewport, false);
        _markerContainer.anchorMin = Vector2.zero;
        _markerContainer.anchorMax = Vector2.one;
        _markerContainer.offsetMin = Vector2.zero;
        _markerContainer.offsetMax = Vector2.zero;
        _markerContainer.pivot = new Vector2(0.5f, 0.5f);
        _markerContainer.SetAsLastSibling();
    }

    private void CreateRenderTexture()
    {
        _renderTexture = new RenderTexture(_textureResolution, _textureResolution, 16, RenderTextureFormat.ARGB32)
        {
            name = "Realtime Minimap Texture",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        _renderTexture.Create();
        _renderRequest = new RenderPipeline.StandardRequest
        {
            destination = _renderTexture,
            face = CubemapFace.Unknown,
            mipLevel = 0,
            slice = 0
        };
    }

    private void CreateMinimapCamera()
    {
        _cameraObject = new GameObject("Realtime Minimap Camera")
        {
            hideFlags = HideFlags.DontSave
        };

        _minimapCamera = _cameraObject.AddComponent<Camera>();
        _minimapCamera.targetTexture = _renderTexture;
        _minimapCamera.enabled = false;

        UniversalAdditionalCameraData cameraData = _minimapCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;
        cameraData.requiresColorTexture = false;
        cameraData.requiresDepthTexture = false;
        cameraData.antialiasing = AntialiasingMode.None;

        ApplyCameraSettings();
    }

    private void ApplyCameraSettings()
    {
        _minimapCamera.orthographic = true;
        _minimapCamera.orthographicSize = _orthographicSize;
        _minimapCamera.aspect = 1f;
        _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        _minimapCamera.backgroundColor = _backgroundColor;
        _minimapCamera.cullingMask = _cullingMask;
        _minimapCamera.allowHDR = false;
        _minimapCamera.allowMSAA = false;
        _minimapCamera.useOcclusionCulling = true;
        _minimapCamera.nearClipPlane = 0.1f;
        _minimapCamera.farClipPlane = _cameraHeight * 2f;
    }

    private void ResolveFollowTarget()
    {
        if (_explicitFollowTarget != null && _explicitFollowTarget.gameObject.activeInHierarchy)
        {
            _followTarget = _explicitFollowTarget;
            return;
        }

        if (CharactersCoordinator.HasInstance)
        {
            CharacterProvider player = CharactersCoordinator.Instance.CurrentPlayer;
            if (player != null && player.gameObject.activeInHierarchy)
            {
                _followTarget = player.transform;
                return;
            }
        }

        Camera mainCamera = Camera.main;
        _followTarget = mainCamera != null && mainCamera != _minimapCamera
            ? mainCamera.transform
            : null;
    }

    private void UpdateCameraPosition()
    {
        Vector3 desiredCenter = _followTarget != null ? _followTarget.position : Vector3.zero;

        if (!_hasMapCenter || _followSharpness <= 0f)
        {
            _mapCenter = desiredCenter;
            _hasMapCenter = true;
        }
        else
        {
            float blend = 1f - Mathf.Exp(-_followSharpness * Time.unscaledDeltaTime);
            _mapCenter = Vector3.Lerp(_mapCenter, desiredCenter, blend);
        }

        _cameraObject.transform.SetPositionAndRotation(
            new Vector3(_mapCenter.x, _mapCenter.y + _cameraHeight, _mapCenter.z),
            Quaternion.Euler(90f, 0f, 0f));
    }

    private void DiscoverMarkers()
    {
        _discoveredTargets.Clear();

        if (_showCharacters && CharactersCoordinator.HasInstance)
        {
            CharactersCoordinator coordinator = CharactersCoordinator.Instance;
            List<CharacterProvider> characters = coordinator.AllCharacters;

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterProvider character = characters[i];
                if (character == null || !character.gameObject.activeInHierarchy)
                {
                    continue;
                }

                MarkerKind kind = character == coordinator.CurrentPlayer
                    ? MarkerKind.Player
                    : MarkerKind.Character;

                RegisterMarker(character.transform, kind);
            }
        }

        if (_showResources)
        {
            ResourceNode[] resources = FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < resources.Length; i++)
            {
                ResourceNode resource = resources[i];
                if (resource == null || resource.IsDepleted)
                {
                    continue;
                }

                RegisterMarker(resource.transform, MarkerKind.Resource);
            }
        }

        if (_showDungeonMaster)
        {
            DungeonMaster[] dungeonMasters = FindObjectsByType<DungeonMaster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < dungeonMasters.Length; i++)
            {
                DungeonMaster dungeonMaster = dungeonMasters[i];
                if (dungeonMaster != null)
                {
                    RegisterMarker(dungeonMaster.transform, MarkerKind.DungeonMaster);
                }
            }
        }

        _staleTargets.Clear();
        foreach (KeyValuePair<Transform, MarkerView> pair in _markers)
        {
            if (pair.Key == null || !_discoveredTargets.Contains(pair.Key))
            {
                _staleTargets.Add(pair.Key);
            }
        }

        for (int i = 0; i < _staleTargets.Count; i++)
        {
            RemoveMarker(_staleTargets[i]);
        }
    }

    private void RegisterMarker(Transform target, MarkerKind kind)
    {
        _discoveredTargets.Add(target);

        if (_markers.TryGetValue(target, out MarkerView marker))
        {
            ApplyMarkerStyle(marker, kind);
            return;
        }

        GameObject markerObject = new GameObject($"Marker - {target.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markerObject.transform.SetParent(_markerContainer, false);

        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);

        Image markerImage = markerObject.GetComponent<Image>();
        markerImage.raycastTarget = false;

        marker = new MarkerView
        {
            Target = target,
            Rect = markerRect,
            Image = markerImage,
            Kind = kind
        };

        _markers.Add(target, marker);
        ApplyMarkerStyle(marker, kind);
    }

    private void ApplyMarkerStyle(MarkerView marker, MarkerKind kind)
    {
        marker.Kind = kind;
        marker.Image.color = GetMarkerColor(kind);

        float sizeMultiplier = kind == MarkerKind.Player || kind == MarkerKind.DungeonMaster ? 1.35f : 1f;
        marker.Rect.sizeDelta = Vector2.one * (_markerSize * sizeMultiplier);
    }

    private Color GetMarkerColor(MarkerKind kind)
    {
        switch (kind)
        {
            case MarkerKind.Player:
                return _playerColor;
            case MarkerKind.Resource:
                return _resourceColor;
            case MarkerKind.DungeonMaster:
                return _dungeonMasterColor;
            default:
                return _characterColor;
        }
    }

    private void UpdateMarkerPositions()
    {
        if (_markerContainer == null)
        {
            return;
        }

        Rect rect = _markerContainer.rect;
        float halfWorldHeight = _minimapCamera.orthographicSize;
        float halfWorldWidth = halfWorldHeight * _minimapCamera.aspect;

        foreach (KeyValuePair<Transform, MarkerView> pair in _markers)
        {
            MarkerView marker = pair.Value;
            if (marker.Target == null)
            {
                marker.Image.enabled = false;
                continue;
            }

            Vector3 offset = marker.Target.position - _mapCenter;
            Vector2 normalized = new Vector2(offset.x / halfWorldWidth, offset.z / halfWorldHeight);
            bool inside = Mathf.Abs(normalized.x) <= 1f && Mathf.Abs(normalized.y) <= 1f;

            if (!inside && !_clampMarkersToBorder)
            {
                marker.Image.enabled = false;
                continue;
            }

            marker.Image.enabled = true;
            float horizontalLimit = Mathf.Max(0f, rect.width * 0.5f - marker.Rect.rect.width * 0.5f);
            float verticalLimit = Mathf.Max(0f, rect.height * 0.5f - marker.Rect.rect.height * 0.5f);

            marker.Rect.anchoredPosition = new Vector2(
                Mathf.Clamp(normalized.x * rect.width * 0.5f, -horizontalLimit, horizontalLimit),
                Mathf.Clamp(normalized.y * rect.height * 0.5f, -verticalLimit, verticalLimit));
        }
    }

    private void RemoveMarker(Transform target)
    {
        if (!_markers.TryGetValue(target, out MarkerView marker))
        {
            return;
        }

        _markers.Remove(target);
        if (marker.Image != null)
        {
            Destroy(marker.Image.gameObject);
        }
    }

    private void ReleaseRuntimeResources()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        _continuousRenderFallback = false;
        _markers.Clear();
        _discoveredTargets.Clear();
        _staleTargets.Clear();

        if (_markerContainer != null)
        {
            Destroy(_markerContainer.gameObject);
            _markerContainer = null;
        }

        if (_output != null && _output.texture == _renderTexture)
        {
            _output.texture = null;
        }

        if (_cameraObject != null)
        {
            Destroy(_cameraObject);
            _cameraObject = null;
            _minimapCamera = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        _renderRequest = null;

        if (_ownsRectMask && _rectMask != null)
        {
            Destroy(_rectMask);
        }

        _rectMask = null;
        _ownsRectMask = false;
    }
}
