using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using RecycleRush.Core.PrecisionSystem;

public enum AudioPriority
{
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// PoolSource objesi, hem AudioSource referansını hem de oynatılan sesin önceliğini (Priority) ve ne zaman çalmaya başladığını tutar.
    /// Bu sayede Round-Robin algoritması hangi sesi keseceğine (Override) karar verebilir.
    /// </summary>
    public class PoolSource
    {
        public AudioSource Source;
        public AudioPriority Priority;
        public float StartTime;
    }

    public class AudioManager : MonoBehaviour
    {
        // Singleton Instance
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer Groups")]
        [SerializeField] private AudioMixerGroup _bgmMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;
        [SerializeField] private AudioMixerGroup _spatialMixerGroup;

        [Header("BGM Settings")]
        [Tooltip("Fabrika ortamına uygun arkaplan müziği")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioClip _bgmClip;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.3f;

        [Header("Machine Sounds & Ambience")]
        [Tooltip("Sürekli çalışan bant/motor sesi (Reactive Ambience olarak kullanılır)")]
        [SerializeField] private AudioSource _engineSource;
        [SerializeField] private AudioClip _engineClip;
        [SerializeField] private AudioClip _leverClip;

        [Header("Precision Hit SFX (2D)")]
        [SerializeField] private AudioClip _perfectDingClip;
        [SerializeField] private AudioClip _greatDingClip;
        [SerializeField] private AudioClip _goodDingClip;
        [SerializeField] private AudioClip _dingClip; 
        [SerializeField] private AudioClip _buzzerClip; 

        [Header("SFX Clips - Gameplay (2D)")]
        [SerializeField] private AudioClip _uiClickClip;
        [SerializeField] private AudioClip _grabClip;
        [SerializeField] private AudioClip _releaseClip;
        [SerializeField] private AudioClip _floorPenaltyClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _levelUpFanfareClip;

        [Header("Spatial SFX (3D)")]
        [SerializeField] private AudioClip _goldenWasteClip;
        [SerializeField] private AudioClip _coinCollectClip;
        [SerializeField] private AudioClip _trashDropClip;

        // --- Pooling Variables (Voice Limiting) ---
        private const int MAX_2D_SOURCES = 6;
        private const int MAX_3D_SOURCES = 8; // AR performansı için optimize edildi (12 -> 8)

        private List<PoolSource> _pool2D = new List<PoolSource>();
        private List<PoolSource> _pool3D = new List<PoolSource>();

        // --- Reactive Systems ---
        private Dictionary<AudioClip, float> _lastPlayedTimes = new Dictionary<AudioClip, float>();
        private const float DEFAULT_COOLDOWN = 0.08f; 
        
        // Volume / Ambience Targets
        private float _uiVolume = 0.8f;
        private float _targetEngineVolume = 0.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); // Core objesini toptan silmemesi icin sadece scripti siliyoruz;
                return;
            }
            Instance = this;

            // Havuzları (Pools) oluştur
            InitializePools();

            // Mixer Entegrasyonu
            if (_bgmSource != null && _bgmMixerGroup != null) _bgmSource.outputAudioMixerGroup = _bgmMixerGroup;
            if (_engineSource != null && _sfxMixerGroup != null) _engineSource.outputAudioMixerGroup = _sfxMixerGroup;

            // BGM ve Ambiyans Başlatma
            if (_bgmSource != null && _bgmClip != null)
            {
                _bgmSource.clip = _bgmClip;
                _bgmSource.volume = _bgmVolume;
                _bgmSource.loop = true;
                _bgmSource.Play();
            }

            if (_engineSource != null && _engineClip != null)
            {
                _engineSource.clip = _engineClip;
                _engineSource.loop = true;
                _targetEngineVolume = _uiVolume;
                _engineSource.Stop();
            }
        }

        private void InitializePools()
        {
            // 2D Havuzu (Manager'ın kendisine bağlı AudioSource'lar)
            for (int i = 0; i < MAX_2D_SOURCES; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
                src.playOnAwake = false;
                if (_uiMixerGroup != null) src.outputAudioMixerGroup = _uiMixerGroup;
                
                _pool2D.Add(new PoolSource { Source = src, Priority = AudioPriority.Low, StartTime = 0 });
            }

            // 3D Havuzu (Manager'ın altında Child objeler olarak)
            for (int i = 0; i < MAX_3D_SOURCES; i++)
            {
                GameObject obj3D = new GameObject($"SpatialAudioSource_{i}");
                obj3D.transform.SetParent(this.transform);
                
                var src = obj3D.AddComponent<AudioSource>();
                src.spatialBlend = 1f;
                src.minDistance = 1f;
                src.maxDistance = 15f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.playOnAwake = false;
                if (_spatialMixerGroup != null) src.outputAudioMixerGroup = _spatialMixerGroup;

                _pool3D.Add(new PoolSource { Source = src, Priority = AudioPriority.Low, StartTime = 0 });
            }
        }

        private void Update()
        {
            // Reactive Ambience: Motor/Ortam sesini hedeflenen değere yumuşak (fade) geçiş yap
            if (_engineSource != null && Mathf.Abs(_engineSource.volume - _targetEngineVolume) > 0.01f)
            {
                _engineSource.volume = Mathf.Lerp(_engineSource.volume, _targetEngineVolume, Time.deltaTime * 2f);
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            MachineLever.OnLeverPulledAction += PlayLeverSound;
            
            RecycleRush.Managers.ComboManager.OnComboChanged += HandleComboChanged;

            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            MachineLever.OnLeverPulledAction -= PlayLeverSound;
            
            RecycleRush.Managers.ComboManager.OnComboChanged -= HandleComboChanged;

            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        // --- POOLING LOGIC ---

        private PoolSource GetAvailableSource(List<PoolSource> pool, AudioPriority requestedPriority)
        {
            PoolSource oldestSource = null;
            float oldestTime = float.MaxValue;

            foreach (var ps in pool)
            {
                // 1. Boş (Çalmayan) kaynak var mı?
                if (!ps.Source.isPlaying)
                {
                    return ps;
                }

                // 2. Çalıyor ama hangisi daha eski? (Olası override için)
                if (ps.StartTime < oldestTime)
                {
                    oldestTime = ps.StartTime;
                    oldestSource = ps;
                }
            }

            // Tüm kaynaklar doluysa: Hiyerarşi (Priority) kontrolü yap.
            // Sadece benden DAHA DÜŞÜK veya EŞİT öncelikli bir sesi kesebilirim.
            if (oldestSource != null && requestedPriority >= oldestSource.Priority)
            {
                return oldestSource;
            }

            // Havuz tamamen benden daha yüksek öncelikli seslerle doluysa iptal et (Voice Limiting)
            return null;
        }

        // --- AAA AUDIO ENGINE METHODS ---

        private void PlayWithVariation(AudioClip clip, AudioPriority priority = AudioPriority.Low, float volume = 1f, float pitchRandomness = 0.04f)
        {
            if (clip == null) return;

            // Audio Cooldown (Spam koruması)
            if (_lastPlayedTimes.TryGetValue(clip, out float lastTime))
            {
                if (Time.time - lastTime < DEFAULT_COOLDOWN) return; 
            }
            _lastPlayedTimes[clip] = Time.time;

            // Havuzdan boş veya eski bir 2D kaynak al
            PoolSource ps = GetAvailableSource(_pool2D, priority);
            if (ps == null) return; // Çalınabilecek kaynak yok

            ps.Priority = priority;
            ps.StartTime = Time.time;
            
            // Ayarlar ve Varyasyon
            ps.Source.clip = clip;
            ps.Source.volume = volume * _uiVolume;
            ps.Source.pitch = 1f + UnityEngine.Random.Range(-pitchRandomness, pitchRandomness);
            
            ps.Source.Play();
        }

        public void PlaySpatialSound(AudioClip clip, Vector3 position, AudioPriority priority = AudioPriority.Low, float volume = 1f)
        {
            if (clip == null) return;

            PoolSource ps = GetAvailableSource(_pool3D, priority);
            if (ps == null) return;

            ps.Priority = priority;
            ps.StartTime = Time.time;

            ps.Source.transform.position = position; // Kaynağı olaya ışınla
            ps.Source.clip = clip;
            ps.Source.volume = volume;
            ps.Source.pitch = 1f + UnityEngine.Random.Range(-0.03f, 0.03f);

            ps.Source.Play();
        }

        // --- GAMEPLAY TRIGGERS ---

        private void HandleWasteProcessed(SortResultData data)
        {
            if (!data.IsCorrect)
            {
                PlayWithVariation(_buzzerClip, AudioPriority.Medium, 1.0f);
                return;
            }

            // Çöp Kutuya Düştü Sesi (3D)
            PlayTrashDropSound(data.ActionPosition);

            // Altın Çöp Sesi (3D)
            if (data.WasGoldenWaste)
            {
                PlayGoldenWasteSound(data.ActionPosition);
            }

            // İsabet (Precision) Sesi (UI)
            switch (data.PrecisionData.Tier)
            {
                case PrecisionTier.Perfect:
                    PlayWithVariation(_perfectDingClip, AudioPriority.High, 1.0f, 0.02f);
                    break;
                case PrecisionTier.Great:
                    PlayWithVariation(_greatDingClip, AudioPriority.Medium, 0.9f, 0.03f);
                    break;
                case PrecisionTier.Good:
                    PlayWithVariation(_goodDingClip, AudioPriority.Low, 0.8f, 0.04f);
                    break;
                case PrecisionTier.Normal:
                default:
                    PlayWithVariation(_dingClip, AudioPriority.Low, 0.75f, 0.05f);
                    break;
            }
        }

        private void HandleComboChanged(int comboCount, int multiplier, bool isRankUp)
        {
            // Kombo sıfırlandıysa (Kırıldıysa)
            if (comboCount == 0)
            {
                // Kombo sıfırlanınca Ambiyans normale döner
                _targetEngineVolume = _uiVolume;
                return;
            }

            // Sadece Seviye (Rank) Atlandığında çal (AAA Pitch-Shift Escalation)
            if (isRankUp && _comboClip != null)
            {
                PoolSource ps = GetAvailableSource(_pool2D, AudioPriority.Medium);
                if (ps != null)
                {
                    _lastPlayedTimes[_comboClip] = Time.time;
                    ps.Priority = AudioPriority.Medium;
                    ps.StartTime = Time.time;

                    ps.Source.clip = _comboClip;
                    ps.Source.volume = 1.0f * _uiVolume; 
                    
                    // Müzikal Tırmanış (Pitch Escalation): Her çarpan artışında ses giderek incelir.
                    // x2 = 1.0f, x3 = 1.1f, x4 = 1.25f, x5 = 1.4f
                    float targetPitch = 1.0f;
                    switch (multiplier)
                    {
                        case 2: targetPitch = 1.0f; break;
                        case 3: targetPitch = 1.1f; break;
                        case 4: targetPitch = 1.25f; break;
                        case 5: targetPitch = 1.4f; break;
                        default: targetPitch = 1.4f; break;
                    }
                    
                    ps.Source.pitch = targetPitch;
                    ps.Source.Play();
                }
            }
            
            // Reactive Ambience: Kombo arttıkça makine/ortam uğultusu çok hafifçe yükselir (Subtle etki)
            if (multiplier > 1)
            {
                _targetEngineVolume = Mathf.Clamp(_uiVolume + (multiplier * 0.02f), 0f, 1f);
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing || state == GameState.Tutorial)
            {
                if (_engineSource != null && !_engineSource.isPlaying) _engineSource.Play();
                _targetEngineVolume = _uiVolume; // Ambiyans normal
            }
            else if (state == GameState.GameOver)
            {
                // Reactive Ambience: Oyun bitince ortam sesi yavaşça sıfıra düşer (Fade out)
                _targetEngineVolume = 0f;
            }
            else if (state == GameState.MainMenu || state == GameState.Initialization)
            {
                if (_engineSource != null && _engineSource.isPlaying) _engineSource.Stop();
            }
        }

        public void PlayLeverSound() { PlayWithVariation(_leverClip, AudioPriority.Medium); }
        public void PlayGrabSound(Vector3 position) { PlayWithVariation(_grabClip, AudioPriority.Low); }
        public void PlayReleaseSound(Vector3 position) { PlayWithVariation(_releaseClip, AudioPriority.Low); }
        public void PlayFloorPenaltySound() { PlayWithVariation(_floorPenaltyClip, AudioPriority.Medium); }
        public void PlayUIClick() { PlayWithVariation(_uiClickClip, AudioPriority.High, 1.0f, 0.01f); }
        public void PlayLevelUpFanfare() { PlayWithVariation(_levelUpFanfareClip, AudioPriority.High, 1.0f, 0f); }
        
        public void PlayGoldenWasteSound(Vector3 position) { PlaySpatialSound(_goldenWasteClip, position, AudioPriority.Medium); }
        public void PlayCoinCollectSound(Vector3 position) { PlaySpatialSound(_coinCollectClip, position, AudioPriority.Low, 0.8f); }
        public void PlayTrashDropSound(Vector3 position) { PlaySpatialSound(_trashDropClip, position, AudioPriority.Low); }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
        }

        public void SetSFXVolume(float volume)
        {
            _uiVolume = Mathf.Clamp01(volume);
            // Engine volume hedefini güncelliyoruz ki Update'te Fade ile otursun
            if (_targetEngineVolume > 0.01f) _targetEngineVolume = _uiVolume;
        }
    }
