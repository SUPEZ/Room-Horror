using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace GameResources
{
    [AddComponentMenu("Room Horror/Resources/Collected Resources Manager")]
    public class CollectedResourcesManager : MMSingleton<CollectedResourcesManager>
    {
        private readonly Dictionary<ResourceType, int> _storedResources = new Dictionary<ResourceType, int>();

        public event Action<ResourceType, int, int> ResourceAmountChanged;

        public int GetAmount(ResourceType resourceType)
        {
            EnsureInitialized();
            return _storedResources.TryGetValue(resourceType, out int amount) ? amount : 0;
        }

        public void AddResource(ResourceType resourceType, int amount)
        {
            if (resourceType == ResourceType.None || amount <= 0)
            {
                return;
            }

            EnsureInitialized();
            int previousAmount = GetAmount(resourceType);
            int newAmount = previousAmount + amount;
            _storedResources[resourceType] = newAmount;
            ResourceAmountChanged?.Invoke(resourceType, newAmount, amount);
        }

        public bool TrySpendResource(ResourceType resourceType, int amount)
        {
            if (resourceType == ResourceType.None || amount <= 0)
            {
                return false;
            }

            EnsureInitialized();
            int previousAmount = GetAmount(resourceType);
            if (previousAmount < amount)
            {
                return false;
            }

            int newAmount = previousAmount - amount;
            _storedResources[resourceType] = newAmount;
            ResourceAmountChanged?.Invoke(resourceType, newAmount, -amount);
            return true;
        }

        public Dictionary<ResourceType, int> GetSnapshot()
        {
            EnsureInitialized();
            return new Dictionary<ResourceType, int>(_storedResources);
        }

        private void EnsureInitialized()
        {
            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                if (!_storedResources.ContainsKey(resourceType))
                {
                    _storedResources.Add(resourceType, 0);
                }
            }
        }
    }
}
