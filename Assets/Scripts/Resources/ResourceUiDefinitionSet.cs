using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources
{
    [CreateAssetMenu(fileName = "ResourceUiDefinitionSet", menuName = "Room Horror/Resources/UI Definition Set", order = 11)]
    public class ResourceUiDefinitionSet : ScriptableObject
    {
        [Serializable]
        public class ResourceUiDefinition
        {
            public ResourceType ResourceType = ResourceType.None;
            public Sprite Sprite;
            public string DisplayName;
        }

        [SerializeField] private ResourceUiDefinition[] _definitions = Array.Empty<ResourceUiDefinition>();

        private Dictionary<ResourceType, ResourceUiDefinition> _definitionsByType;

        public IReadOnlyList<ResourceUiDefinition> Definitions => _definitions;

        public IReadOnlyDictionary<ResourceType, ResourceUiDefinition> DefinitionsByType
        {
            get
            {
                EnsureDictionary();
                return _definitionsByType;
            }
        }

        public bool TryGetDefinition(ResourceType resourceType, out ResourceUiDefinition definition)
        {
            EnsureDictionary();
            return _definitionsByType.TryGetValue(resourceType, out definition);
        }

        public Sprite GetSprite(ResourceType resourceType)
        {
            return TryGetDefinition(resourceType, out ResourceUiDefinition definition)
                ? definition.Sprite
                : null;
        }

        public string GetDisplayName(ResourceType resourceType)
        {
            if (!TryGetDefinition(resourceType, out ResourceUiDefinition definition))
            {
                return resourceType == ResourceType.None ? string.Empty : resourceType.ToString();
            }

            return string.IsNullOrWhiteSpace(definition.DisplayName)
                ? resourceType.ToString()
                : definition.DisplayName;
        }

        private void OnValidate()
        {
            _definitionsByType = null;
        }

        private void EnsureDictionary()
        {
            if (_definitionsByType != null)
            {
                return;
            }

            _definitionsByType = new Dictionary<ResourceType, ResourceUiDefinition>();
            for (int i = 0; i < _definitions.Length; i++)
            {
                ResourceUiDefinition definition = _definitions[i];
                if (definition == null || definition.ResourceType == ResourceType.None)
                {
                    continue;
                }

                _definitionsByType[definition.ResourceType] = definition;
            }
        }
    }
}
