using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleRush.Core.Packages
{
    public class PackageChallengeTracker : MonoBehaviour
    {
        public static PackageChallengeTracker Instance { get; private set; }

        public static event Action OnPackageCompleted;
        public static event Action OnPackageFailed;

        // Tracks which wastes belong to which package. Key: Waste GameObject, Value: Package ID
        private Dictionary<GameObject, string> _activePackageWastes = new Dictionary<GameObject, string>();
        
        // Tracks remaining required wastes for a specific package. Key: Package ID, Value: Count of remaining wastes
        private Dictionary<string, int> _packageRequirements = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        /// <summary>
        /// Registers a newly opened package and its spawned wastes into the tracker.
        /// </summary>
        public void RegisterNewPackage(List<GameObject> spawnedWastes)
        {
            if (spawnedWastes == null || spawnedWastes.Count == 0) return;

            string packageId = System.Guid.NewGuid().ToString();
            _packageRequirements.Add(packageId, spawnedWastes.Count);

            foreach (var waste in spawnedWastes)
            {
                if (waste != null)
                {
                    _activePackageWastes[waste] = packageId;
                }
            }
            
            Debug.Log($"<color=#D87093>[PackageTracker]</color> Yeni Sürpriz Paket kaydedildi! (ID: {packageId.Substring(0, 5)}, Hedef: {spawnedWastes.Count} Atık)");
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.ProcessedWaste == null) return;

            // Is this waste part of a package?
            if (_activePackageWastes.TryGetValue(data.ProcessedWaste, out string packageId))
            {
                _activePackageWastes.Remove(data.ProcessedWaste); // Her halükarda listeden çıkar

                if (!data.IsCorrect)
                {
                    // Yanlış atıldıysa paket görevi yanar!
                    FailPackage(packageId);
                }
                else
                {
                    // Doğru atıldıysa paketin kalan hedefini düşür
                    if (_packageRequirements.ContainsKey(packageId))
                    {
                        _packageRequirements[packageId]--;
                        
                        if (_packageRequirements[packageId] <= 0)
                        {
                            CompletePackage(packageId);
                        }
                    }
                }
            }
        }

        public void NotifyWasteDroppedOnFloor(GameObject waste)
        {
            // FloorPollutionDetector'den çağrılacak. Çöp yere düştüyse (3 saniye dolduysa) paket yanar.
            if (waste != null && _activePackageWastes.TryGetValue(waste, out string packageId))
            {
                _activePackageWastes.Remove(waste);
                FailPackage(packageId);
            }
        }

        private void FailPackage(string packageId)
        {
            if (_packageRequirements.ContainsKey(packageId))
            {
                _packageRequirements.Remove(packageId);
                Debug.Log($"<color=red>[PackageTracker]</color> Paket Başarısız! Çöp yanlış ayrıştırıldı veya cezaya düştü.");
                OnPackageFailed?.Invoke();
            }
        }

        private void CompletePackage(string packageId)
        {
            if (_packageRequirements.ContainsKey(packageId))
            {
                _packageRequirements.Remove(packageId);
                Debug.Log($"<color=#32CD32>[PackageTracker]</color> PACKAGE COMPLETE! Tüm çöpler hatasız ayrıştırıldı.");
                OnPackageCompleted?.Invoke();
            }
        }
    }
}
