using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RecycleRush.Environment
{
    /// <summary>
    /// VR elleriyle (Collider) fiziksel olarak basılabilen, UnityEvent tetikleyen endüstri standartlarında (AAA) buton sistemi.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PhysicalVRButton : MonoBehaviour
    {
        [Header("Button Settings")]
        [Tooltip("Butonun aşağı-yukarı hareket eden kısmı (Örn: Butonun kırmızı görsel objesi)")]
        [SerializeField] private Transform _pushPart;
        
        [Tooltip("Butonun Y ekseninde ne kadar aşağı çökeceği (Metre cinsinden, genelde 0.02f iyidir)")]
        [SerializeField] private float _pressDistance = 0.02f;
        
        [Tooltip("Butonun aşağı inme ve eski haline dönme (yaylanma) animasyon hızı")]
        [SerializeField] private float _springSpeed = 0.05f;

        [Header("Events (Olaylar)")]
        [Tooltip("Butona tam olarak basıldığında tetiklenecek fonksiyonlar (Örn: GameManager.StartGame)")]
        public UnityEvent OnPressed;
        
        [Tooltip("Buton serbest bırakıldığında tetiklenecek fonksiyonlar")]
        public UnityEvent OnReleased;

        [Header("Audio")]
        [Tooltip("Butona basıldığında çalınacak donanımsal Klik sesi")]
        [SerializeField] private AudioClip _clickSound;

        private AudioSource _audioSource;
        private Vector3 _originalLocalPos;
        private bool _isPressed = false;
        
        // Debounce: Birden fazla parmağın aynı anda butona basması durumunda oyunun bug'a girmesini engeller.
        private int _collidersInTrigger = 0; 

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false; // Oyun başlar başlamaz kendi kendine çalmasın
            
            if (_pushPart != null)
            {
                // Butonun orijinal (başlangıç) pozisyonunu hafızaya al ki el çekilince nereye döneceğini bilsin.
                _originalLocalPos = _pushPart.localPosition;
            }
            else
            {
                Debug.LogWarning($"<color=orange>[PhysicalVRButton]</color> {gameObject.name} üzerinde Push Part atanmadı! Lütfen Inspector'dan atayın.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // İstenirse buraya 'if (!other.CompareTag("PlayerHand")) return;' eklenerek sadece elin basması sağlanabilir.
            
            _collidersInTrigger++;

            // Eğer buton zaten basılı DEĞİLSE ve içeriye en az 1 cisim girdiyse butonu çökert
            if (!_isPressed && _collidersInTrigger > 0)
            {
                PressButton();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _collidersInTrigger--;
            
            // Güvenlik önlemi: Sayacın sıfırın altına düşüp bug yaratmasını engeller
            if (_collidersInTrigger < 0) _collidersInTrigger = 0;

            // Eğer buton basılıysa ve içerideki TÜM cisimler (eller) çıktıysa butonu serbest bırak
            if (_isPressed && _collidersInTrigger == 0)
            {
                ReleaseButton();
            }
        }

        private void PressButton()
        {
            _isPressed = true;
            
            // Klik sesi çal
            if (_clickSound != null)
            {
                _audioSource.PlayOneShot(_clickSound);
            }

            // Butonu fiziksel olarak aşağı itmek için çalışan eski animasyonları durdur ve yenisini başlat
            StopAllCoroutines();
            StartCoroutine(MoveButtonRoutine(_originalLocalPos.y - _pressDistance));
            
            // Inspector'dan atanan fonksiyonları (Örn: StartGame) tetikle!
            OnPressed?.Invoke();
        }

        private void ReleaseButton()
        {
            _isPressed = false;

            // Butonu fiziksel olarak yukarı çek (Yaylanma hissi)
            StopAllCoroutines();
            StartCoroutine(MoveButtonRoutine(_originalLocalPos.y));
            
            OnReleased?.Invoke();
        }

        /// <summary>
        /// Butonun aşağı veya yukarı doğru yumuşak (Smooth) bir şekilde kaymasını sağlayan Animasyon Motoru.
        /// </summary>
        private IEnumerator MoveButtonRoutine(float targetY)
        {
            if (_pushPart == null) yield break;

            float elapsed = 0f;
            Vector3 startPos = _pushPart.localPosition;
            Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);

            while (elapsed < _springSpeed)
            {
                elapsed += Time.deltaTime;
                // Lerp (Linear Interpolation) ile pürüzsüz hareket
                _pushPart.localPosition = Vector3.Lerp(startPos, endPos, elapsed / _springSpeed);
                yield return null;
            }

            _pushPart.localPosition = endPos;
        }
    }
}
