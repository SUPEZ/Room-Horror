using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources
{
    [AddComponentMenu("Room Horror/Resources/Resources View Handler")]
    public class ResourcesViewHandler : MonoBehaviour
    {
        private enum ResourceSource
        {
            DefinitionSet = 0,
            ExplicitList = 1,
            AllResourceTypes = 2
        }

        [Header("Spawn")]
        [SerializeField] private ResourceUiView _viewPrefab;
        [SerializeField] private Transform _pivot;
        [SerializeField] private bool _buildOnEnable = true;
        [SerializeField] private bool _clearBeforeBuild = true;

        [Header("Data")]
        [SerializeField] private ResourceUiDefinitionSet _definitionSet;
        [SerializeField] private ResourceSource _resourceSource = ResourceSource.DefinitionSet;
        [SerializeField] private ResourceType[] _resourceTypes = Array.Empty<ResourceType>();

        private readonly List<ResourceUiView> _spawnedViews = new List<ResourceUiView>();

        public IReadOnlyList<ResourceUiView> SpawnedViews => _spawnedViews;

        private Transform SpawnParent => _pivot != null ? _pivot : transform;

        private void OnEnable()
        {
            if (_buildOnEnable)
            {
                Build();
            }
        }

        [ContextMenu("Build")]
        public void Build()
        {
            if (_viewPrefab == null)
            {
                Debug.LogWarning($"{name} : ResourcesViewHandler needs a view prefab.", this);
                return;
            }

            if (_clearBeforeBuild)
            {
                Clear();
            }

            List<ResourceType> resourceTypes = CollectResourceTypes();
            for (int i = 0; i < resourceTypes.Count; i++)
            {
                CreateView(resourceTypes[i]);
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            for (int i = _spawnedViews.Count - 1; i >= 0; i--)
            {
                ResourceUiView spawnedView = _spawnedViews[i];
                if (spawnedView == null)
                {
                    continue;
                }

                DestroySpawnedObject(spawnedView.gameObject);
            }

            _spawnedViews.Clear();
        }

        public ResourceUiView CreateView(ResourceType resourceType)
        {
            if (_viewPrefab == null || resourceType == ResourceType.None)
            {
                return null;
            }

            ResourceUiView view = Instantiate(_viewPrefab, SpawnParent, false);
            view.name = $"{_viewPrefab.name}_{resourceType}";
            view.SetDefinitionSet(_definitionSet);
            view.SetResourceType(resourceType);
            view.Refresh();
            _spawnedViews.Add(view);

            return view;
        }

        private List<ResourceType> CollectResourceTypes()
        {
            List<ResourceType> result = new List<ResourceType>();

            if (_resourceSource == ResourceSource.DefinitionSet)
            {
                AddFromDefinitionSet(result);
            }
            else if (_resourceSource == ResourceSource.ExplicitList)
            {
                AddFromArray(result, _resourceTypes);
            }
            else
            {
                AddAllResourceTypes(result);
            }

            return result;
        }

        private void AddFromDefinitionSet(List<ResourceType> result)
        {
            if (_definitionSet == null)
            {
                return;
            }

            IReadOnlyList<ResourceUiDefinitionSet.ResourceUiDefinition> definitions = _definitionSet.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                ResourceUiDefinitionSet.ResourceUiDefinition definition = definitions[i];
                if (definition != null)
                {
                    AddUnique(result, definition.ResourceType);
                }
            }
        }

        private void AddFromArray(List<ResourceType> result, ResourceType[] resourceTypes)
        {
            for (int i = 0; i < resourceTypes.Length; i++)
            {
                AddUnique(result, resourceTypes[i]);
            }
        }

        private void AddAllResourceTypes(List<ResourceType> result)
        {
            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                AddUnique(result, resourceType);
            }
        }

        private void AddUnique(List<ResourceType> result, ResourceType resourceType)
        {
            if (resourceType == ResourceType.None || result.Contains(resourceType))
            {
                return;
            }

            result.Add(resourceType);
        }

        private void DestroySpawnedObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
