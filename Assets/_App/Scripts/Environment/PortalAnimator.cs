using UnityEngine;

namespace RecycleRush.Environment
{
    public class PortalAnimator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Z ekseninde kendi etrafında dönme hızı")]
        public float rotationSpeed = 50f;

        [Header("Pulsing (Nefes Alma) Settings")]
        [Tooltip("Büyüyüp küçülme hızı")]
        public float pulseSpeed = 2f;
        [Tooltip("Ne kadar büyüyeceği (Genlik)")]
        public float pulseAmount = 0.05f;

        [Header("Spawn Effect Settings")]
        [Tooltip("İçinden çöp çıktığında ne kadar şişeceği")]
        public float spawnEffectScale = 1.3f;
        [Tooltip("Şişme sonrası eski haline dönme hızı")]
        public float effectRecoverySpeed = 5f;

        private Vector3 baseScale;
        private float currentEffectScale = 1f;

        private void Start()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            // 1. Kendi etrafında yavaşça dönme (Z ekseni)
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            // 2. Spawn Efekti sönümlemesi (Eğer patlamışsa yavaşça 1'e geri döner)
            currentEffectScale = Mathf.Lerp(currentEffectScale, 1f, Time.deltaTime * effectRecoverySpeed);

            // 3. Normal nefes alma (Sinüs dalgası) efekti ile birleştir
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float totalScaleMult = currentEffectScale + pulse;

            // Son ölçeği objeye uygula
            transform.localScale = baseScale * totalScaleMult;
        }

        /// <summary>
        /// Çöp üretildiği (Spawn) anında bu fonksiyon çağrılır.
        /// Portalı aniden büyütür.
        /// </summary>
        public void PlaySpawnEffect()
        {
            currentEffectScale = spawnEffectScale;
        }
    }
}
