using UnityEngine;
using RecycleRush.Core;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Kirlilik seviyesini (RoomPollutionManager) dinleyerek AR ortamında 
    /// UI olmadan oyuncuya durumu hissettiren görsel/işitsel tepkiler verir.
    /// SRP gereği sadece ambiyans (görüntü/ses) işlerini yapar.
    /// </summary>
    public class PollutionAmbienceController : MonoBehaviour
    {
        [Header("Görsel Tepkiler (Particle Systems)")]
        [Tooltip("Critical (%50) durumunda havada uçuşacak hafif toz/sinek partikülleri")]
        [SerializeField] private ParticleSystem _mildDirtParticles;
        
        [Tooltip("Danger (%75) durumunda eklenecek daha yoğun kir veya sis efekti")]
        [SerializeField] private ParticleSystem _dangerDirtParticles;

        [Header("İşitsel Tepkiler (Audio)")]
        [Tooltip("Danger (%75) seviyesine geçildiğinde çalacak kısa uyarı sesi")]
        [SerializeField] private AudioClip _dangerWarningSound;
        
        [Tooltip("Game Over (%100) olduğunda çalacak başarısızlık sesi")]
        [SerializeField] private AudioClip _gameOverSound;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            
            // Başlangıçta partikülleri kapalı tut
            SetParticlesActive(_mildDirtParticles, false);
            SetParticlesActive(_dangerDirtParticles, false);
        }

        private void OnEnable()
        {
            RoomPollutionManager.OnPollutionStateChanged += HandlePollutionStateChanged;
        }

        private void OnDisable()
        {
            RoomPollutionManager.OnPollutionStateChanged -= HandlePollutionStateChanged;
        }

        private void HandlePollutionStateChanged(PollutionState newState)
        {
            switch (newState)
            {
                case PollutionState.Clean:
                case PollutionState.Mild:
                    // Ortam temiz, tüm kötü efektleri kapat
                    SetParticlesActive(_mildDirtParticles, false);
                    SetParticlesActive(_dangerDirtParticles, false);
                    break;

                case PollutionState.Critical: // %50+
                    // Hafif kir partikülleri devreye girer
                    SetParticlesActive(_mildDirtParticles, true);
                    SetParticlesActive(_dangerDirtParticles, false);
                    break;

                case PollutionState.Danger: // %75+
                    // Yoğun partiküller devreye girer
                    SetParticlesActive(_mildDirtParticles, true);
                    SetParticlesActive(_dangerDirtParticles, true);
                    
                    // Oyuncuya panik yaptıracak uyarı sesini çal
                    if (_dangerWarningSound != null && !_audioSource.isPlaying)
                    {
                        _audioSource.PlayOneShot(_dangerWarningSound);
                    }
                    break;

                case PollutionState.GameOver: // %100
                    // Oyun biterken büyük hata sesi
                    if (_gameOverSound != null)
                    {
                        _audioSource.PlayOneShot(_gameOverSound);
                    }
                    break;
            }
        }

        private void SetParticlesActive(ParticleSystem ps, bool active)
        {
            if (ps == null) return;

            if (active && !ps.isPlaying)
            {
                ps.Play();
            }
            else if (!active && ps.isPlaying)
            {
                ps.Stop();
                ps.Clear();
            }
        }
    }
}
