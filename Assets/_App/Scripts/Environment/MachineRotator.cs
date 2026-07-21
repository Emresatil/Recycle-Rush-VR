using UnityEngine;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Makinenin çarkı veya pervanesi gibi dönen objeleri, sadece oyun aktifken (veya öğreticideyken) döndürür.
    /// </summary>
    public class MachineRotator : MonoBehaviour
    {
        [Tooltip("Saniyede hangi eksende kaç derece dönecek? (Örn: Z ekseninde 100)")]
        public Vector3 rotationSpeed = new Vector3(0, 0, 100f);

        private void Update()
        {
            // Oyun sadece Playing veya Tutorial durumundayken objeyi döndür
            if (GameManager.Instance != null && 
                (GameManager.Instance.CurrentState == GameState.Playing || 
                 GameManager.Instance.CurrentState == GameState.Tutorial))
            {
                transform.Rotate(rotationSpeed * Time.deltaTime);
            }
        }
    }
}
