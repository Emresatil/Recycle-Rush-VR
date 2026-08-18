using UnityEngine;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// Çöpün "Kirli" olma durumunu (State) ve görsel geri bildirimini yönetir.
    /// Sorumluluğu: Kirlilik seviyesini tutmak, görseli güncellemek ve Object Pool için resetlenmektir (SRP).
    /// </summary>
    public class DirtyWasteController : MonoBehaviour
    {
        [Header("Dirt Settings")]
        [Tooltip("Mevcut kirlilik seviyesi (0-100)")]
        public float dirtiness = 100f;
        
        [Tooltip("Kirliliği temsil eden Child Object (Örn: Çamur küresi veya balçık modeli)")]
        public GameObject dirtVisual;

        [Header("Washing Limits")]
        [Tooltip("Suyun saniyede kirliliği düşürme hızı")]
        public float washRate = 40f;

        // Her particle collision veya tickte çok hızlı düşmesini engellemek için cooldown
        private float _lastWashTime;
        private const float WashCooldown = 0.05f; 

        public bool IsDirty => dirtiness > 0f;

        private Vector3 _initialVisualScale;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (dirtVisual != null)
            {
                _initialVisualScale = dirtVisual.transform.localScale;
            }
        }

        public void InitializeDirtyState()
        {
            dirtiness = 100f;
            if (dirtVisual != null)
            {
                if (!_isInitialized)
                {
                     _initialVisualScale = dirtVisual.transform.localScale;
                     _isInitialized = true;
                }
                dirtVisual.SetActive(true);
                dirtVisual.transform.localScale = _initialVisualScale;
            }
        }

        /// <summary>
        /// Su tabancasından su temas ettiğinde çağrılır.
        /// </summary>
        public void Wash()
        {
            if (!IsDirty) return;

            // Çok hızlı (her particle) tetiklenmesini engelle, sabit bir hızda düşür
            if (Time.time - _lastWashTime < WashCooldown) return;
            _lastWashTime = Time.time;

            // Kirliliği düşür (Saniyede washRate kadar düşecek şekilde normalize ettik)
            float reductionStep = washRate * WashCooldown;
            dirtiness = Mathf.Clamp(dirtiness - reductionStep, 0f, 100f);

            UpdateVisuals();

            if (dirtiness <= 0f)
            {
                CleanWaste();
            }
        }

        private void UpdateVisuals()
        {
            if (dirtVisual != null)
            {
                // Kirlilik oranına göre (0-1) küçültme efekti
                float scaleFactor = dirtiness / 100f;
                dirtVisual.transform.localScale = _initialVisualScale * scaleFactor;
            }
        }

        private void CleanWaste()
        {
            dirtiness = 0f;
            if (dirtVisual != null)
            {
                dirtVisual.SetActive(false);
            }
            Debug.Log($"<color=cyan>[Dirty Waste]</color> {gameObject.name} tamamen temizlendi!");
        }

        private void OnDisable()
        {
            // Object Pool Güvenliği: Havuza dönen çöp temizlenir.
            // Spawner doğururken tekrar InitializeDirtyState() çağıracaktır.
            dirtiness = 0f;
            if (dirtVisual != null)
            {
                dirtVisual.SetActive(false);
            }
        }
    }
}
