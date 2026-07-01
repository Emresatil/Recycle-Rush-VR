# Recycle Rush - VR Game Implementation Plan

Bu proje planı, 20 günlük staj süresince "Recycle Rush" VR oyununun geliştirilmesi için hazırlanmıştır. Takım üyeleri (Hakan ve Sabri Emre) tüm çalışmalarda belirlenen Çevik (Agile) kurallarına ve kod standartlarına sıkı sıkıya uyacaktır.

## User Review Required
> [!IMPORTANT]
> Bu plan 4 fazlı (her biri 5 gün) bir yapıya oturtulmuştur. Takım iş bölümü Hakan (Sistem/Oyun Döngüsü ağırlıklı) ve Sabri Emre (VR Etkileşimleri/Cila ağırlıklı) arasında paylaştırılmıştır. Bu iş dağılımı ve fazlandırma uygun mu?

## Open Questions
> [!WARNING]
> Hangi VR SDK kullanılacak? (Örn: Unity XR Interaction Toolkit, Oculus Integration SDK, veya Auto Hand gibi bir asset). Zaman kazanmak adına ve standartlara uygun olması için Unity XR Interaction Toolkit (XRI) kullanılması önerilmektedir. Onaylıyor musunuz?

## Phase 1: Foundation, Movement & Core VR Interaction (Days 1-5)
**Hedef:** Proje iskeletinin kurulması, VR ortamında ellerin görünmesi ve temel tutma (grab) mekaniğinin hazır assetler ile entegre edilmesi.
- Projenin oluşturulması, Git/GitHub entegrasyonu, `.gitignore` ayarları.
- XR Rig'in kurulması ve temel el takibinin (controller/hand tracking) sağlanması.
- Çevre (Environment) hazır asset'lerinin sahneye yerleştirilmesi (Fabrika veya depo ortamı).
- Atık objelerinin (şişe, kutu, kağıt) prefab olarak hazırlanması ve `XRGrabInteractable` özelliklerinin eklenmesi.

**Görev Dağılımı:**
- **Hakan:** Proje reposunun kurulumu, `.gitignore` ayarı, çevre prefablarının sahneye dizilmesi.
- **Sabri:** XR Rig kurulumu, Controller/El modellerinin eklenmesi, objelere Grab/Throw fiziklerinin ayarlanması.

## Phase 2: Game Loop & Core Mechanics (Days 6-10)
**Hedef:** Yürüyen bant sisteminin yapılması, objelerin spawn olması ve atık kutularının mantığı.
- Yürüyen bant (Conveyor Belt) fizik/transform mantığının yazılması.
- Spawner sisteminin yazılması (Rastgele atık türlerini belirli aralıklarla bant üzerinde oluşturma).
- Geri dönüşüm kutularının (Plastik, Cam, Kağıt vb.) trigger mantığının yazılması (Doğru eşya atılırsa puan, yanlışsa ceza).
- Puan (Score) sisteminin kodlanması.

**Görev Dağılımı:**
- **Hakan:** Yürüyen bant scripti, Spawner sistemi, Score Manager.
- **Sabri:** Atık kutularının trigger scriptleri, objelerin kutuya girme (Snap/Destroy) kontrolleri.

## Phase 3: Progression, UI & Polish Prep (Days 11-15)
**Hedef:** Oyunun zorlaşması, kullanıcı arayüzü ve ses entegrasyonlarının temelleri.
- Zamanla hızlanan bant veya sıklaşan spawn süreleri (Progression).
- World-space UI (Oyuncunun görebileceği bir panelde Puan ve Kalan Zaman gösterimi).
- Ses yöneticisinin (Audio Manager) kurulması: Arka plan fabrikası sesi, obje tutma sesi, doğru/yanlış kutu sesi.
- Oyun sonu ekranı ve yeniden başlama (Restart) mekaniği.

**Görev Dağılımı:**
- **Hakan:** Progression (Zorluk) sistemi, Restart mekaniği, Game Manager güncellemeleri.
- **Sabri:** World-space UI tasarımı ve entegrasyonu, Audio Manager ve ses efektlerinin eklenmesi.

## Phase 4: Polish, Haptics, VFX & Publishing (Days 16-20)
**Hedef:** Oyun hissiyatını (Game Feel) zirveye taşımak ve Meta Store yayın süreçleri.
- Görsel Efektler (VFX): Doğru kutuya atıldığında konfeti/partikül efekti, objeyi tutunca highlight.
- Haptic Feedback (Titreşim): Obje tutulduğunda ve kutuya girdiğinde controller titreşimi.
- Performans Optimizasyonu: Draw call optimizasyonu, ışıklandırma bake işlemleri (VR için kritik).
- Meta Store (App Lab) yayınlanma sürecinin araştırılması, build alınması ve APK'nın test edilmesi.

**Görev Dağılımı:**
- **Hakan:** Performans optimizasyonu, Build ayarları, Meta Store dokümantasyon/App Lab araştırma ve yükleme.
- **Sabri:** VFX entegrasyonları, Haptic Feedback (XR Controller Rumble) ayarlamaları.

## Verification Plan
### Automated Tests
- Projenin zaman kısıtı ve doğası (VR etkileşimi) nedeniyle kapsamlı Unit Test yazılmayacaktır. Oyun döngüsü manuel test edilecektir.
### Manual Verification
- Her PR (Pull Request) öncesi VR gözlüğünde (veya Link kablosu ile) `Play Mode`'da test edilecektir.
- Obje tutma/fırlatma mekaniği ve conveyor belt'in performans/fizik sorunları yakından takip edilecektir.
