using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources
{
    [CreateAssetMenu(fileName = "ResourceInitialSpriteSet", menuName = "Room Horror/Resources/Initial Sprite Set", order = 10)]
    public class ResourceInitialSpriteSet : ScriptableObject
    {
        [Serializable]
        public class ResourceSpritesEntry
        {
            public ResourceType ResourceType = ResourceType.None;
            public Sprite[] Sprites = Array.Empty<Sprite>();
        }

        [SerializeField] private ResourceSpritesEntry[] _entries = Array.Empty<ResourceSpritesEntry>();

        private Dictionary<ResourceType, Sprite[]> _spritesByType;

        public IReadOnlyDictionary<ResourceType, Sprite[]> SpritesByType
        {
            get
            {
                EnsureDictionary();
                return _spritesByType;
            }
        }

        public bool TryGetSprites(ResourceType resourceType, out Sprite[] sprites)
        {
            EnsureDictionary();
            return _spritesByType.TryGetValue(resourceType, out sprites) && sprites != null && sprites.Length > 0;
        }

        public Sprite GetSprite(ResourceType resourceType, int index)
        {
            if (!TryGetSprites(resourceType, out Sprite[] sprites))
            {
                return null;
            }

            int safeIndex = Mathf.Clamp(index, 0, sprites.Length - 1);
            return sprites[safeIndex];
        }

        public Sprite GetRandomSprite(ResourceType resourceType)
        {
            if (!TryGetSprites(resourceType, out Sprite[] sprites))
            {
                return null;
            }

            return sprites[UnityEngine.Random.Range(0, sprites.Length)];
        }

        private void OnValidate()
        {
            _spritesByType = null;
        }

        private void EnsureDictionary()
        {
            if (_spritesByType != null)
            {
                return;
            }

            _spritesByType = new Dictionary<ResourceType, Sprite[]>();
            for (int i = 0; i < _entries.Length; i++)
            {
                ResourceSpritesEntry entry = _entries[i];
                if (entry == null || entry.ResourceType == ResourceType.None)
                {
                    continue;
                }

                _spritesByType[entry.ResourceType] = entry.Sprites ?? Array.Empty<Sprite>();
            }
        }
    }
}
