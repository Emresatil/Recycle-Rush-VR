using UnityEngine;
using UnityEngine.InputSystem;

namespace RecycleRush.Environment
{
    /// <summary>
    /// SADECE TEST İÇİNDİR. (Build alırken silebilir veya kapatabilirsiniz)
    /// XR Device Simulator yerine klavye ile basitçe sağa-sola ve ileri-geri hareket etmeyi sağlar.
    /// YENİ INPUT SİSTEMİ İLE YAZILMIŞTIR.
    /// XR Origin objesinin üzerine atın.
    /// </summary>
    public class DebugKeyboardMover : MonoBehaviour
    {
        [Tooltip("Hareket hızı")]
        public float moveSpeed = 2.0f;
        
        [Tooltip("Sadece WASD ile mi yoksa ok tuşlarıyla da mı hareket edilsin?")]
        public bool enableMovement = true;

        void Update()
        {
            // Yeni Input Sistemini kullanıyoruz. Eğer klavye takılı değilse hata vermesin diye kontrol ediyoruz.
            if (!enableMovement || Keyboard.current == null) return;

            float h = 0f;
            float v = 0f;

            // W, S, Yukarı, Aşağı
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            
            // A, D, Sol, Sağ
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                // XR Origin'in kendi "İleri" ve "Sağ" yönüne göre hareket et
                Vector3 moveDirection = transform.right * h + transform.forward * v;
                
                // Yüksekliği değiştirmemesi için Y eksenini sıfırla (Uçmayı engellemek için)
                moveDirection.y = 0f;

                transform.position += moveDirection.normalized * (moveSpeed * Time.deltaTime);
            }
        }
    }
}
