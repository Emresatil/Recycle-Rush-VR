ş# Implementation Plan — Recycle Rush MR (Mixed Reality)

### Karma Gerçeklik (AR) Geri Dönüşüm Simülatörü

> **Proje Adı:** Recycle Rush AR
> **Geliştirici Ekip:** Emre & Hakan
> **Süre:** 20 iş günü (4 hafta)
> **Platform:** Mixed Reality (Passthrough, Meta Quest)
> **Oyun Motoru:** Unity 3D (LTS)
> **Doküman Sürümü:** v8.0 (Nihai Sürüm - Performans metrikleri, Özellik Önceliklendirmesi, JSON Save standardı ve MR Oda Riski eklendi)
> **Doküman Türü:** Implementation Plan / Tek Doğruluk Kaynağı (Single Source of Truth)

> **Bu dokümanın amacı:** Projemizin mimarisini, oyun döngüsünü, durum makinesini (State Machine), dizin yapısını ve proje yönetim kurallarını gri nokta bırakmadan tanımlamaktır. Geliştirme süreci tamamen bu plana birebir uyarak ilerleyecektir. Projenin 2. ayında, VR konveyör bandı konseptinden çıkılarak, oyuncunun kendi gerçek odasında (Meta Passthrough) oynayabileceği, detaylı seviye dengelemesine (Level Balancing), XP/Coin ekonomisine, kaydedilebilir profil verilerine ve başarım sistemine sahip bir AR deneyimine dönüştürülmesi planlanmıştır.

---

## İçindekiler

1. Yönetici Özeti
2. Problem Tanımı ve Hedefler
3. Kapsam ve Özellik Önceliklendirmesi (Feature Hierarchy)
4. Teknoloji Yığını
5. Unity Dizin Ağacı ve Proje Mimarisi
6. Temel Oynanış ve Mekanikler (Core Game Loop & State Machine)
7. Sistem Mimarisi & Manager Sınıfları
8. Etkileşim ve Geri Bildirim Matrisi (Haptic & Audio)
9. Puanlama, Seviye Tasarımı (Balancing) ve Zorluk Eğrisi
10. Sanat Yönetimi (Low-Poly)
11. UI (Kullanıcı Arayüzü) ve Sahne Envanteri
12. Build & Meta Store Dağıtım Süreci
13. Kodlama Standartları, Veri Saklama (JSON Save System) ve Performans Hedefleri
14. Test Stratejisi
15. Dokümantasyon Yapısı (docs/)
16. 20 İş Günlük Yol Haritası (Gün Gün Detaylı Plan)
17. Haftalık Sprint Özeti
18. Git Workflow & PR Süreci
19. GitHub Projects Board & Issue Yönetimi
20. Daily Standup
21. Risk Yönetimi
22. Definition of Done (Bitti Kriteri)
23. Teslim Edilecekler

---

## 1. Yönetici Özeti

Recycle Rush MR; ilk ay tasarlanan VR konseptini oyuncunun gerçek fiziksel odasına (oturma odası, ofis vb.) taşıyan, seviye ilerlemeli (progression), matematiksel olarak dengelenmiş görev ve ekonomi sistemleriyle güçlendirilmiş bir Karma Gerçeklik (Mixed Reality) simülasyonudur. Ekip olarak amacımız, atık ayrıştırma sürecini, oyuncunun kendi evinde fiziksel olarak hareket ederek oynadığı interaktif bir "Arcade" deneyimine çevirmektir.

Oyuncu, Meta Quest'in dış kameraları sayesinde kendi odasını görürken; odanın farklı köşelerine yerleştirilmiş sanal geri dönüşüm kutularına (Kağıt, Cam, Plastik), odanın tavanındaki sanal portallardan düşen atıkları toplayıp fırlatarak puan, XP ve Coin kazanır. Oyunda 15 seviyelik zorluk dengesi, özel Golden Waste (Altın Çöp) sürprizleri, 6 farklı rastgele etkinlik (Lucky Drop, Slow Motion vb.), kombo sistemleri, seviye görevleri ve başarımlar (achievements) yer alır.

---

## 2. Problem Tanımı ve Hedefler

### 2.1 Çözülen Problem

İlk aşamada (VR versiyonunda) hareket eden konveyör bandı üzerinde obje sürtünmeleri, kilitlenmeler (deadlock) ve tünelleme gibi çözümü saatler süren fiziksel çarpışma bug'ları yaşanmıştır. Bu MR geçişiyle konveyör bandı tamamen silinmiş, fiziksel sürtünme sıfırlanmıştır. Çöpler doğrudan gerçek odanın zeminine düşecek, oyuncu yerinden kalkıp fiziksel olarak yürüyerek çöpleri toplayacaktır. Ayrıca tek dize skor yerine seviye hedefleri (Görevler), XP, Coin, Altın Çöp ve Başarımlar eklenerek oyunun tekrar oynanabilirliği (Replayability) artırılmıştır.

### 2.2 Başarı Kriterleri (Definition of Success)

* [ ] Meta Passthrough ve Scene Understanding API'leri kusursuz entegre edilecek.
* [ ] Geri dönüşüm kutuları Spatial Anchors (Uzamsal Çapalar) ile gerçek odaya kalıcı olarak sabitlenecek.
* [ ] 15 Seviyelik Dengeleme Matrisine (Level Balancing Table) sadık kalınarak ilerleme sistemi kodlanacak.
* [ ] Golden Waste (Altın Çöp), Kombo Çarpanları, Başarımlar (Achievements) ve Rastgele Etkinlikler (Speed Mode, Lucky Drop vb.) koda entegre edilecek.
* [ ] GameManager Durum Makinesi (State Machine), JSON Save Data tablosu, Pause Menüsü ve Holo-Tutorial hazır olacak.
* [ ] İki geliştirici, paralel çalışarak "Stacked PR (Zincirleme Pull Request)" sistemiyle kodlarını başarıyla `main` dalında birleştirecek.

---

## 3. Kapsam ve Özellik Önceliklendirmesi (Feature Hierarchy)

20 günlük sıkı geliştirme sürecinde olası zaman baskısı durumunda projenin çekirdeğini korumak adına özellikler MoSCoW metodolojisiyle önceliklendirilmiştir:

| Öncelik Seviyesi | Kapsam / Özellikler | Açıklama |
| --- | --- | --- |
| **Must-Have (Öncelik 1 - Kritik)** | Passthrough, Spatial Anchors, AR Grab/Throw, Portal Spawner, Temel Puanlama | Oyunun çalışması için olmazsa olmaz teknik altyapı. |
| **Should-Have (Öncelik 2 - Ana Sistemler)** | 15 Level Yapısı, XP & Coin Ekonomisi, JSON Save System, Pause Menüsü, State Machine | Oyun hissini ve ilerlemeyi sağlayan temel mekanikler. |
| **Could-Have (Öncelik 3 - İçerik Zenginliği)** | Görev Sistemi, Başarımlar (Achievements), Golden Waste (%5-25), 6 Rastgele Etkinlik | Oynanabilirliği ve derinliği artıran arcade ögeler. |
| **Nice-to-Have (Öncelik 4 - Cila / Polish)** | 3D Uzamsal Sesler (Spatial Audio), Konfeti VFX, Gelişmiş 3D Holo-Tutorial | Görsel ve işitsel kaliteyi zirveye taşıyan cila elemanları. |

---

## 4. Teknoloji Yığını

| Katman | Teknoloji | Gerekçe |
| --- | --- | --- |
| **Oyun Motoru** | Unity 3D (2022 LTS veya üstü) | Standart endüstri motoru, mobil MR performans dostu. |
| **Dil** | C# | Nesne yönelimli, temiz kod mimarisine uygun. |
| **MR Kütüphanesi** | Unity XR Interaction Toolkit + Meta XR Core SDK | Passthrough, Spatial Anchors ve el takibi için zorunlu. |
| **Veri Saklama** | JSON Serialization (`Application.persistentDataPath`) | Esnek, modüler ve genişletilebilir profil kaydı için. |
| **Versiyon Kontrol** | Git + GitHub | Main dalı korumalı, Stacked PR bazlı geliştirme. |
| **Proje Yönetimi** | GitHub Projects (Kanban) | To Do, In Progress, Review, Done akışıyla iş takibi. |
| **Test & Build** | Meta Quest Developer Hub (MQDH) | Kablosuz build, gerçek odada MR testi. |

---

## 5. Unity Dizin Ağacı ve Proje Mimarisi

```
Recycle-Rush-AR/
├── Assets/
│   ├── _App/                   # BİZİM GELİŞTİRDİĞİMİZ ÇEKİRDEK DOSYALAR
│   │   ├── Scenes/             # MainGame_AR.unity
│   │   ├── Scripts/
│   │   │   ├── Core/           # GameManager.cs, ScoreManager.cs, LevelManager.cs, EconomyManager.cs, SaveManager.cs
│   │   │   ├── AR_Features/    # ARPlacementManager.cs, PortalSpawner.cs
│   │   │   ├── Gameplay/       # ComboManager.cs, MissionManager.cs, AchievementManager.cs, EventManager.cs
│   │   │   ├── Interaction/    # BinTrigger.cs, WasteItem.cs, GoldenWaste.cs
│   │   │   └── Polish/         # HapticManager.cs, AudioManager.cs, VfxManager.cs
│   │   ├── Prefabs/            # AR uyumlu kutular, portallar, altın çöpler
│   │   ├── Materials/          # Renk paletlerimiz, Hologram & Golden materyalleri
│   │   ├── Audio/              # 3D Uzamsal sesler, SFX, BGM
│   │   └── UI/                 # Level panoları, Pause menüsü, Wrist UI, Canvas prefabları
├── docs/                       # GDD, Mimari notlar, Store görselleri (GitHub'da)
├── .gitignore                  # Unity için standart gitignore
└── implementation_plan.md      # BU DOSYA

```

---

## 6. Temel Oynanış ve Mekanikler (Core Game Loop & State Machine)

### 6.1 GameManager Durum Makinesi (State Machine Diagram)

Oyunun tüm akışı `GameManager.cs` tarafından yönetilen açık bir durum makinesine (State Machine) dayanır:

```text
                  ┌────────────────────────┐
                  │       Main Menu        │
                  └───────────┬────────────┘
                              │ Oyun Başlatıldı
                              ▼
                  ┌────────────────────────┐
                  │ Placement State (AR)   │ (Kutuların odaya sabitlenmesi)
                  └───────────┬────────────┘
                              │ Kurulum Tamamlandı
                              ▼
                  ┌────────────────────────┐
                  │ Countdown State (3-2-1)│ (Oyuncunun hazırlanması)
                  └───────────┬────────────┘
                              │ Sayaç Bitti
                              ▼
                  ┌────────────────────────┐
                  │     Playing State      │◄────────────────┐
                  └─────┬──────────────┬───┘                 │
                        │              │                     │
         Pause Basıldı  │              │ Süre/Görev Bitti    │ Devam Et
                        ▼              ▼                     │
                 ┌────────────┐  ┌────────────┐              │
                 │Paused State│  │ Game Over  ├──────────────┘
                 └──────┬─────┘  └─────┬──────┘ (Sonraki Seviye)
                        │              │
                        └──────┬───────┘
                               │ Ana Menüye Dönüş
                               ▼
                  ┌────────────────────────┐
                  │       Main Menu        │
                  └────────────────────────┘
```

### 6.2 Sistemler Arası Veri ve Etkileşim Akışı (System Architecture Flow)

```text
                      ┌──────────────────────┐
                      │    WasteSpawner      │
                      └──────────┬───────────┘
                                 │ Obje Üretildi (Normal veya Golden %5-%25)
                                 ▼
                      ┌──────────────────────┐
                      │    WasteItem /       │
                      │    GoldenWaste       │
                      └──────────┬───────────┘
                                 │ Oyuncu Kutuya Fırlattı
                                 ▼
                      ┌──────────────────────┐
                      │     BinTrigger       │
                      └──────────┬───────────┘
                                 │ Action Event Tetiklendi
        ┌────────────────────────┼────────────────────────┐
        ▼                        ▼                        ▼
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│ ScoreManager │         │ComboManager  │         │EconomyManager│
└───────┬──────┘         └──────┬───────┘         └──────┬───────┘
        │ Puan Eklendi          │ Kombo Çarpanı          │ XP & Coin Eklendi
        ▼                        ▼                        ▼
┌────────────────────────────────────────────────────────────────┐
│                   LevelManager & UIManager                     │
│    (Görev Tamamlandı mı? Level Atlandı mı? UI Güncellemesi)     │
└────────────────────────────────────────────────────────────────┘
```

---

## 7. Sistem Mimarisi & Manager Sınıfları

* `GameManager.cs`: State Machine yapısını yönetir (`MainMenu`, `Placement`, `Countdown`, `Playing`, `Paused`, `GameOver`).
* `ScoreManager.cs` & `EconomyManager.cs`: Puan, Coin ve XP hesabı yapar.
* `SaveManager.cs`: Kaydedilecek verileri `JSON Serialization` ile `Application.persistentDataPath` dizininde dosya olarak saklar.
* `LevelManager.cs`: 15 seviyenin dengeleme verilerini ve ilerlemeyi kontrol eder.
* `MissionManager.cs` & `AchievementManager.cs`: Görevleri ve başarımları takip eder.
* `PortalSpawner.cs`: Çöpleri düşürür, seviyeye bağlı olarak `%5 → %25` oranında `GoldenWaste` spawn eder.
* `EventManager.cs`: 6 farklı rastgele etkinliği yönetir (`Speed Mode`, `Double Coin`, `Lucky Drop`, `Slow Motion`, `Double XP`, `Mega Combo`).
* `ARPlacementManager.cs`: Geri dönüşüm kutularını Spatial Anchors ile odaya kilitler.

---

## 8. Etkileşim ve Geri Bildirim Matrisi (Haptic & Audio)

| Etkileşim | Görsel (VFX) | İşitsel (Audio) | Dokunsal (Haptic) |
| --- | --- | --- | --- |
| Atığın Portaldan Düşmesi | Portaldan ışık süzülmesi | Atığın düştüğü fiziksel yönden gelen 'Pof' sesi | Yok |
| Golden Waste Belirmesi | Altın parıltılar & Işık hüzmesi | Özel şanslı 'Chime / Jingle' sesi | Ritmik özel titreşim |
| Çöpü Yerden Tutma | Obje elde parlar | Hafif 'Pop' sesi | Kısa ve düşük yoğunluklu titreşim (0.1s, 0.3 amp) |
| Doğru Kutuya Atma | Kutudan yeşil partikül çıkar | Kutunun olduğu yönden 'Ding!' | Çift vuruşlu orta titreşim (0.2s, 0.5 amp) |
| Kombo Yapma (x3) | Ekranda devasa Holo-Yazı | Heyecanlı bir 'Level Up' sesi | Uzun, güçlü titreşim (0.3s, 1.0 amp) |
| Seviye / Görev Tamamlama | Konfeti patlaması | Zafer Fon Müziği / Fanfar | Güçlü uzun titreşim (0.5s) |

---

## 9. Puanlama, Seviye Tasarımı (Balancing) ve Zorluk Eğrisi

### 9.1 Seviye Tasarımı ve Dengeleme Matrisi (Level Design & Balancing Table)

Oyun 15 seviyeden oluşmaktadır. Seviye ilerledikçe portal düşme hızı artar, altın çöp oranı `%5`'ten başlayıp finalde `%25`'e yükselir.

| Level | Hedef Puan | Portal Düşme Aralığı (sn) | Golden Waste İhtimali | Özel Etkinlik / Görev Hedefi | Kazanılan Ödül (Coin) |
| --- | --- | --- | --- | --- | --- |
| **Lvl 1** | 50 Puan | 3.5 sn | %5 | Öğretici: 3 Kağıt At | +20 Coin |
| **Lvl 2** | 80 Puan | 3.2 sn | %5 | 5 Atık Ayrıştır | +30 Coin |
| **Lvl 3** | 120 Puan | 3.0 sn | %8 | 1 Kombo Yap (x2) | +40 Coin |
| **Lvl 4** | 180 Puan | 2.8 sn | %8 | 3 Cam Atık At | +50 Coin |
| **Lvl 5** | 250 Puan | 2.5 sn | %10 | **Speed Mode Etkinliği!** (Hızlı Düşüş) | +75 Coin |
| **Lvl 6** | 350 Puan | 2.3 sn | %10 | 1 Altın Çöp Yakala | +90 Coin |
| **Lvl 7** | 480 Puan | 2.1 sn | %12 | **Lucky Drop Etkinliği!** (2x Altın Şansı) | +110 Coin |
| **Lvl 8** | 620 Puan | 1.9 sn | %14 | Hiç Hata Yapmadan 8 Atış | +130 Coin |
| **Lvl 9** | 780 Puan | 1.7 sn | %16 | **Slow Motion Etkinliği!** (Zaman Yavaşlar) | +150 Coin |
| **Lvl 10** | 1000 Puan | 1.5 sn | %18 | **Double Coin Etkinliği!** & 2 Altın Çöp | +200 Coin |
| **Lvl 11** | 1250 Puan | 1.4 sn | %20 | 4 Kombo Yap | +230 Coin |
| **Lvl 12** | 1550 Puan | 1.3 sn | %20 | **Double XP Etkinliği!** & 60sn Süre | +260 Coin |
| **Lvl 13** | 1900 Puan | 1.2 sn | %22 | 3 Altın Çöp Yakala | +300 Coin |
| **Lvl 14** | 2300 Puan | 1.1 sn | %22 | Hata Yapmadan 15 Atış | +350 Coin |
| **Lvl 15** | 3000 Puan | 0.9 sn (2 Portal) | **%25** | **Arcade Master (Final):** 2 Portal + Çift Altın + All Events! | +500 Coin + Master Tacı |

> **Level 15 (Arcade Master) Detayı:** Final seviyesinde odanın tavanında 2 farklı Portal aynı anda açılır. Golden Waste ihtimali zirveye (%25) çıkar. `Speed Mode` ve `Double Coin` etkinlikleri eş zamanlı aktif olur. Oyuncunun 3000 puana ulaşması ve x5 Kombo yapması gerekir.

---

### 9.2 Ekonomi, Coin Kullanımı ve İlerleme Matematiği

#### Coin Kullanım Amacı (Coin Economy Usage)
Kazanılan Coin'ler oyunda şu 3 ana alanda harcanır:
1. **İleri Seviye Paketlerinin Kilitlerini Açma:** Yüksek seviyelere daha hızlı erişim sağlamak.
2. **Kozmetik Kutu ve Eldiven Kaplamaları:** Geri dönüşüm kutularına ve VR ellerine neon/altın kaplama (skin) satın almak.
3. **Profil Unvanları & Rozetler:** Oyuncu profilinde sergilenen unvanların kilidini açmak.

#### Seviye İçin Gereken XP Formülü
Seviye atlamak için gereken XP tutarı aşağıdaki üstel denklemle hesaplanır:
$$XP_{gerekli} = \text{Mathf.RoundToInt}(100 \times Level^{1.4})$$

* **Level 1:** $100 \times 1.0 = 100\text{ XP}$
* **Level 5:** $100 \times 5^{1.4} = 952\text{ XP}$
* **Level 10:** $100 \times 10^{1.4} = 2512\text{ XP}$
* **Level 15:** $100 \times 15^{1.4} = 4442\text{ XP}$

#### Atık Puan ve Ödül Tablosu

| Atık Tipi | Puan | Kazanılan XP | Kazanılan Coin | Hata Cezası |
| --- | --- | --- | --- | --- |
| **Standart Atık (Kağıt/Cam/Plastik)** | +10 Puan | +15 XP | +5 Coin | -5 Puan (Kombo Sıfırlanır) |
| **Golden Waste (Altın Çöp)** | +50 Puan | +75 XP | +50 Coin | 0 Puan (Ceza yok, fırsat kaçtı) |
| **Kombo x2 Çarpanı** | +20 Puan | +30 XP | +10 Coin | — |
| **Kombo x3 Çarpanı** | +30 Puan | +45 XP | +15 Coin | — |

---

### 9.3 Rastgele Etkinlikler Matrisi (Random Events Matrix)

Oyun esnasında `EventManager.cs` tarafından aniden tetiklenen 6 farklı rastgele etkinlik bulunmaktadır:

| Etkinlik Adı | Süre | Etki / Avantaj |
| --- | --- | --- |
| **Speed Mode** | 15 sn | Portallardan atık düşme hızı 2 katına çıkar. Kazanılan Coin 2 kat olur. |
| **Double Coin** | 20 sn | Doğru atılan tüm atıklardan 2 kat Coin kazanılır. |
| **Lucky Drop** | 15 sn | Golden Waste düşme ihtimali geçici olarak %50'ye yükselir. |
| **Slow Motion** | 10 sn | Oyun hızı 0.5x yavaşlar, oyuncuya rahat nişan alma imkanı verir. |
| **Double XP** | 20 sn | Tüm atışlardan ve kombolardan kazanılan XP 2 katına çıkar. |
| **Mega Combo** | 12 sn | Kombo seviyesi anında en üst kademeye (x5) yükselir. |

---

### 9.4 Başarım (Achievement) Matrisi

| Başarım Adı | Açıklama / Koşul | Ödül |
| --- | --- | --- |
| **İlk Adım** | İlk atık ayrıştırmanı yap. | +50 Coin |
| **Altın Avcısı** | Toplam 5 adet Golden Waste yakala. | +150 Coin + Altın Çöp İkonu |
| **Kombo Üstadı** | x5 Kombo çarpanına ulaş. | +200 Coin |
| **Temiz Oda** | Bir seviyeyi hiç hata yapmadan (%100 İsabet) bitir. | +300 Coin + Steril Rozet |
| **Koleksiyoner** | Toplam 1000 Coin biriktir. | +500 XP |
| **Geri Dönüşüm Şampiyonu**| 15. Seviyeyi (Arcade Master) tamamla. | Master Tacı (3D UI Görseli) |

---

## 10. Sanat Yönetimi (Low-Poly)

* **Arka Plan:** Tamamen kullanıcının gerçek odasıdır (Meta Passthrough).
* **Tarz:** Performansı (72-90 FPS) korumak için Low-Poly materyaller tercih edilecek; Altın Çöp için parıltılı materyaller, UI için saydam Hologram kaplamaları kullanılacaktır.
* **Renk Paleti:**
  * **Mavi Kutu:** Kağıt / Karton
  * **Yeşil Kutu:** Cam
  * **Sarı Kutu:** Plastik
  * **Altın Efektler:** Sürpriz çöp ve ödüller için sarı/altın parıltı.
* **Aydınlatma:** Çöplerin gerçek halı üzerinde gölge bırakması hedeflenecektir.

---

## 11. UI (Kullanıcı Arayüzü) ve Sahne Envanteri

* **Canvas Tipi:** `World Space` (3D Süzülen Panolar).
* **Giriş / Seçim Paneli:** 15 Seviyenin listelendiği, kilitli/açık seviyeleri gösteren 3D Harita/Pano.
* **Oyun İçi UI:** Anlık Puan, XP Barları, Coin Sayacı, Aktif Seviye Görevi ve Kombo göstergesi.
* **Pause Menüsü:** Oyuncunun sol bileğinde veya önünde açılan 3D "Devam Et / Yeniden Başlat / Ayarlar / Çıkış" paneli.
* **Oyun Sonu İstatistik Paneli:** Atış İsabet Oranı (Accuracy %), Kazanılan XP, Kazanılan Coin, Yapılan Kombo ve High Score rozeti.
* **Tutorial UI:** Havada süzülen 3D oklar ve hologram el figürleri.

---

## 12. Build & Meta Store Dağıtım Süreci

* **Build Target:** Android (ASTC Compression).
* **XR Plugin Management:** Meta XR SDK (Passthrough aktif) seçili.
* **Meta App Lab:** Geliştirme bittiğinde `com.Emre.RecycleRushMR` paket adıyla Keystore imzalanarak APK alınacak ve Meta Store kontrol paneline "Mixed Reality (Karma Gerçeklik)" oyunu olarak yüklenecektir.

---

## 13. Kodlama Standartları, Veri Saklama (JSON Save System) ve Performans Hedefleri

### 13.1 Kaydedilecek Veriler Matrisi (JSON Save Data Table)

Karmaşık ve genişletilebilir veri tiplerini (başarım listeleri, ayarlar) saklamak adına projemizde **JSON Serialization** (`SaveManager.cs`) standardı seçilmiştir. Dosya `Application.persistentDataPath + "/save_data.json"` dizininde tutulur.

| Veri Adı | Veri Tipi | Varsayılan Değer | Açıklama |
| --- | --- | --- | --- |
| `CurrentLevel` | `int` | 1 | Oyuncunun ulaştığı en yüksek seviye. |
| `TotalXP` | `int` | 0 | Biriktirilen toplam XP miktarı. |
| `TotalCoin` | `int` | 0 | Harcanabilir mevcut Coin bakiyesi. |
| `HighScore` | `int` | 0 | Tüm zamanların en yüksek skoru. |
| `UnlockedAchievements`| `List<string>`| `[]` | Kilit açılmış başarım ID listesi. |
| `TutorialCompleted` | `bool` | `false` | AR rehberinin tamamlanma durumu. |
| `AudioSettings` | `AudioData` | `{bgm: 0.8, sfx: 1.0}` | Ses seviye tercihlerini içeren sınıf. |

### 13.2 Optimizasyon ve Performans Hedefleri (Performance Metric Matrix)

Meta Quest donanımında takılmasız (stutter-free) 60-72 FPS deneyimi sağlamak adına aşağıdaki teknik bütçe hedeflerine uyulacaktır:

| Metrik / Hedef | Hedef Değer | Uygulanacak Optimizasyon |
| --- | --- | --- |
| **Kare Hızı (FPS)** | **≥ 72 FPS** | Realtime ışıklar kaldırılacak, Unlit/Simple Lit materyaller kullanılacak. |
| **GC Spike (Bellek Sıçraması)** | **0 Byte (Oyun İçi)** | `Update()` döngüsünde `new` ve `GetComponent` yasak. Object Pooling zorunlu. |
| **Draw Calls (Batches)** | **< 100 Batches** | Doku atlasları (Texture Atlasing) ve Static/Dynamic Batching kullanılacak. |
| **Bellek Kullanımı (RAM)** | **< 1.0 GB** | Doku boyutları max 1080p ile sınırlandırılacak. |
| **Passthrough Latency** | **< 15 ms** | Kamera arka plan işleme yükü minimize edilecek. |

---

## 14. Test Stratejisi

* **Editör Testi (Play Mode):** XR Device Simulator ile klavye/mouse üzerinden State Machine geçişleri, Level atlama, Coin kazanımı ve Save Data testi.
* **Cihaz Testi (Device Build):** Günlük Meta Quest gözlüğüne kablosuz build atılarak fiziksel odada Altın Çöp yakalama, odada yürüme ve 3D UI okunabilirliği test edilecek.

---

## 15. Dokümantasyon Yapısı (docs/)

Hocamızın tavsiyesiyle tüm belgeler repoda düzenli tutulacaktır.

```
docs/
├── design/
│   ├── gdd-ar.md               # AR Oyun Tasarım Belgesi & Seviye Listesi
│   └── reference-images/       # Hologram UI, Altın Çöp ve Başarım görselleri
├── store/
│   ├── description.md          # Meta Store için AR/MR oyun açıklaması
│   └── screenshots/            # Odanın içinde çekilmiş AR oyun içi görselleri
└── architecture/
    └── game-loop-diagram.md    # Mekanik şemalar
```

---

## 16. 20 İş Günlük Yol Haritası (Gün Gün Detaylı Plan)

Her gün en az 1 anlamlı commit atılması zorunludur. Görevler iki geliştirici arasında (Görev 1 ve Görev 2) eşit ve paralel yürütülecek biçimde paylaştırılmıştır.

### 🟦 Faz 0 — AR Geçişi, Passthrough ve Temel Kurulum (Gün 1-4)

* **Gün 1:**
  * **Görev 1:** Unity içinde yeni bir `MainGame_AR` sahnesi kopyalamak ve GitHub'da yeni branch kurmak.
  * **Görev 2:** Meta XR paketlerini güncelleyerek sahnede Passthrough (Dış Kamera) özelliğini aktif etmek.
* **Gün 2:**
  * **Görev 1:** AR projesi için gereken Android Manifest izinlerini ve proje ayarlarını (MRTK veya Meta Core) yapmak.
  * **Görev 2:** Scene Understanding (Zemin ve duvar algılama) API'sini projeye entegre edip test sahneleri oluşturmak.
* **Gün 3:**
  * **Görev 1:** Spatial Anchors API entegrasyonu. (Kutuların yerini hafızada tutan temel kodun yazılması).
  * **Görev 2:** 3 adet geri dönüşüm kutusu prefabını AR ışıklandırmasına ve materyallerine göre revize etmek.
* **Gün 4:**
  * **Görev 1:** `ARPlacementManager` kodlanması. Oyuncunun oyun başında kutuları lazerle odanın köşelerine yerleştirmesini sağlayan sistem.
  * **Görev 2:** Oculus'a ilk saf AR build'in (APK) kablosuz atılması ve Passthrough kararlılık testi.

### 🟩 Faz 1 — AR Spawner, Core Mekanikler & Ekonomi (XP, Coin, Golden Waste) (Gün 5-9)

* **Gün 5:**
  * **Görev 1:** AR Object Pool altyapısını kurmak ve `PortalSpawner.cs` temelini hazırlamak.
  * **Görev 2:** Çöplerin düşeceği "Sanal Portal" görsel tasarımını ve animasyonunu yapmak.
* **Gün 6:**
  * **Görev 1:** Spawner mantığına seviyeye göre artan (%5 → %25) `GoldenWaste` (Altın Çöp) nadir nesne üretim algoritmasını eklemek.
  * **Görev 2:** Grab/Throw etkileşimlerinin AR ortamına göre kalibre edilmesi (Fırlatma gücü ve kütle ayarlamaları).
* **Gün 7:**
  * **Görev 1:** Çöplerin gerçek odadaki fiziksel zemine (Halı/Masa) çarpıp durması için Mesh Collider kodlamaları.
  * **Görev 2:** `BinChecker.cs` triggerlarını tamamlayıp doğru atışta Coin ve XP ödülü veren Action Event'leri tetiklemek.
* **Gün 8:**
  * **Görev 1:** `LevelManager.cs`, `EconomyManager.cs` ve `SaveManager.cs` (JSON Serialization) yazımı. XP/Coin kazanımı, üstel seviye formülü ve Save Data yapısı.
  * **Görev 2:** Haptic titreşim sistemi: Normal tutma, doğru atış ve Altın Çöp yakalandığında devreye girecek ritmik özel titreşimler.
* **Gün 9:**
  * **Görev 1:** `GameManager.cs` Durum Makinesi (State Machine: MainMenu, Placement, Countdown, Playing, Paused, GameOver) algoritmasının yazılması.
  * **Görev 2:** 3D Pause Menüsü tasarımı (Devam Et, Yeniden Başlat, Çıkış) ve bilek/hava canvas entegrasyonu.

### 🟨 Faz 2 — Görevler, Seviyeler, Kombo & Başarımlar (Gün 10-14)

* **Gün 10:**
  * **Görev 1:** `ComboManager.cs` kodlanması. Arka arkaya 3 doğru atışta x2, x3 puan/XP çarpanı mantığı.
  * **Görev 2:** Rastgele Etkinlikler (`EventManager.cs`): Speed Mode, Lucky Drop, Slow Motion vb. 6 etkinliğin kodlanması.
* **Gün 11:**
  * **Görev 1:** Görev Sistemi (`MissionManager.cs`). Her seviye için hedefler ("5 Cam At", "200 XP Kazan") kodlamak.
  * **Görev 2:** Floating UI. Gerçek odada duvarda süzülen "Kombo x3!", Aktif Görev Paneli ve XP barı tasarlamak.
* **Gün 12:**
  * **Görev 1:** Başarım Sistemi (`AchievementManager.cs`). "İlk Altın Çöp", "Kombo Ustası" başarımları ve kilit açma logic'i.
  * **Görev 2:** 15 Seviyelik Level Seçim Haritası / Panosu tasarımı (Kilitli ve Yıldızlı Seviyeler).
* **Gün 13:**
  * **Görev 1:** High Score kaydı & Oyun Sonu İstatistik algoritması (İsabet Oranı %, Kazanılan Coin/XP hesabı).
  * **Görev 2:** Oyun Sonu İstatistik UI Paneli tasarımı ve rozet efektleri.
* **Gün 14:**
  * **Görev 1:** Yeni AR Holo-Tutorial Scripti. Oyuna ilk girene odada adımları zorunlu kılan sistem.
  * **Görev 2:** Tutorial için 3D hologram oklar, el rehberleri ve yönlendirme görsellerinin hazırlanması.

### 🟧 Faz 3 — Cila (Polish), VFX, SFX & Rastgele Etkinlikler (Gün 15-18)

* **Gün 15:**
  * **Görev 1:** Spatial Audio (3D Ses) entegrasyonu. Altın çöp sesi, Coin toplama sesi ve 3D düştüğü yön sesi.
  * **Görev 2:** AR uyumlu, tatmin edici kısa ses efektlerinin (Ding, Hata, Kombo, Level Up fanfarı) projeye eklenmesi.
* **Gün 16:**
  * **Görev 1:** `VfxManager.cs` optimizasyonu ve efekt havuzlama (Particle System Pooling).
  * **Görev 2:** Görsel cila: Altın Çöp parıltısı, Konfeti efekti, Speed Mode ekran parlaması ve Neon efektler.
* **Gün 17:**
  * **Görev 1:** FPS (Performans) Profiler ile kod optimizasyonu. GC (Garbage Collector) bellek sızıntılarını temizleme.
  * **Görev 2:** UX Hata Yönetimi: Odanın ışığı yetersiz olduğunda oyuncuyu uyaracak "Odayı Aydınlatın" güvenlik paneli.
* **Gün 18:**
  * **Görev 1:** 15 Seviyenin Dengeleme Matrisine sadık kalınarak zorluk eğrisi ve XP katsayılarının kalibre edilmesi.
  * **Görev 2:** Level 15 (Arcade Master: 2 Portal, %25 Altın Çöp, All Events) özel aksiyon playtesti.

### 🟥 Faz 4 — Meta Store, Test ve Teslim (Gün 19-20)

* **Gün 19:**
  * **Görev 1:** Projenin son Release APK Build'inin alınması ve Meta Keystore imzası.
  * **Görev 2:** Meta Quest kaydı ile "Odanın İçinde Oyun İçi Fragman" (Gameplay Trailer) çekilmesi ve montajı.
* **Gün 20:**
  * **Görev 1:** Tüm branch'lerin GitHub'da `main` dalı ile birleştirilmesi (Merge) ve `README.md`'nin yenilenmesi.
  * **Görev 2:** Meta App Lab (Mağaza) sayfasına girip oyun türünü "Mixed Reality" yapmak, yeni fragmanı ve fotoğrafları yükleyip teslime göndermek.

---

## 17. Haftalık Sprint Özeti

| Hafta | Günler | Hedef | Çıktı |
| --- | --- | --- | --- |
| **1** | 1–5 | AR Altyapısı, Passthrough, Zemin Algılama, Spawner ve Altın Çöp altyapısı. | Oyuncunun kendi odasını gördüğü ve çöplerin tavandan düştüğü temel mekanik. |
| **2** | 6–10 | Ekonomi (XP, Coin), State Machine, 3-2-1 Sayaç, Pause Menüsü, Kombo ve 6 Etkinlik. | XP/Coin kazandığımız, durum makinesiyle çalışan tam oyun döngüsü. |
| **3** | 11–15 | 15 Level Sistemi, Görevler, Başarımlar, JSON Save System, İstatistikler ve Tutorial. | Seviyeli, görevli, verileri saklayan ve tutorial içeren zengin oyun döngüsü. |
| **4** | 16–20 | 3D Ses, Altın VFX, Level 15 Arcade Master testi, Fragman Çekimi ve App Lab Yayını. | Mağazaya yüklemeye hazır, tam cilalı bir Karma Gerçeklik (MR) oyunu. |

---

## 18. Git Workflow & PR Süreci

Staj hocalarının beklentisi doğrultusunda:

* Asla doğrudan `main` dalına kod atılmayacaktır (Push yok).
* Her yeni özellik için `main` dalından yeni branch açılır (Örn: `feature/save-system`, `feature/state-machine`).
* Geliştirme bitince GitHub üzerinden Pull Request (PR) açılır. Hocalar reviewer olarak eklenir.
* Bir PR onaylanmadan diğerine geçilmesi gerekirse, **Stacked PR (Zincirleme Pull Request)** yöntemi kullanılacaktır.
* PR açıklamasına ne yapıldığı yazılır, hocalardan onay (Approve) gelince kod `main` ile birleştirilir (Merge).

**Commit Mesaj Standardı:**

* `feat(core): game manager durum makinesi (state machine) eklendi`
* `feat(save): json tabanlı save manager kaydedici yazıldı`
* `design(level): level 15 arcade master 2 portal aksiyonu yapılandırıldı`

---

## 19. GitHub Projects Board & Issue Yönetimi

* Repomuzun **Projects** sekmesindeki Kanban panosu aktif olarak kullanılacaktır.
* Yeni 20 günlük planın her bir adımı yeni bir **Issue** olarak açılacaktır.
* Sütunlar: **To Do** / **In Progress** / **Review** / **Done**.
* İşe başlamadan önce Issue *In Progress*'e çekilecek, PR açıldığında *Review*'a alınacak, merge edilince *Done* yapılacaktır.

---

## 20. Daily Standup

Her iş günü stajyer ekibi (Emre ve Hakan) olarak standup değerlendirmesi yapacağız:

1. **Dün ne yaptım?**
2. **Bugün hangi Task/Issue üzerinde çalışacağım?**
3. **Beni engelleyen (Blocker) bir durum var mı?**
Bu özetler, o gün atılan commit'lerle (staj günlüğüyle) birebir uyumlu olacaktır.

---

## 21. Risk Yönetimi

| Risk | Olasılık | Etki | Önlem |
| --- | --- | --- | --- |
| MR Ortamının Farklı Odalarda / Işıkta Farklı Davranması | Orta | Orta | Farklı aydınlatma ve oda düzenlerinde (dar/geniş) test yapılacak; yetersiz ışıkta uyarı UI'ı açılacak. |
| Passthrough'da FPS Dropları | Orta | Yüksek | Kesinlikle Low-Poly asset kullanımı, ışıkların ve partiküllerin sınırlandırılması. |
| Fazla Mekanik Sebebiyle Süre Aşımı | Orta | Orta | Özellik Önceliklendirmesi (MoSCoW) matrisine göre Must-Have adımları önce tamamlanacak. |
| Git Merge Çakışması (Scene Conflict) | Yüksek | Orta | İki kişi aynı anda `.unity` sahne dosyasını düzenlemeyecek, Prefab çalışılacak. |
| Veri Kaybı (Save/Load Hataları) | Düşük | Orta | `SaveManager` için JSON serileştirme testleri ve fallback verileri oluşturulacak. |

---

## 22. Definition of Done (Bitti Kriteri)

Bir görevin tamamen bitti sayılması için koşullar:

* [ ] AR Etkileşimler (Grab/Throw, Altın Çöp, Görevler) Quest cihazında gerçek odayla test edilmiştir.
* [ ] Kod standartlara uygun yazılmış ve `feature` dalından Push edilmiştir.
* [ ] Seviye atlama, XP/Coin kaydı, JSON Save Data ve Pause menüsü hatasız çalışıyor.
* [ ] GitHub'da PR açılmış, hocalar tarafından onaylanmış ve `main`'e merge edilmiştir.
* [ ] Kanban kartı *Done* sütununa taşınmıştır.

---

## 23. Teslim Edilecekler

1. **Unity Kaynak Kodu:** Temiz AR mimarisiyle, `main` branch üzerinde çalışan Seviyeli & Ekonomili MR projesi.
2. **Build Dosyası (APK):** Gözlüğe doğrudan kurulabilir sürüm.
3. **Görsel ve Dokümantasyon:** Plandaki `docs/` klasörü içindeki tüm gereksinimler (GDD, Ekran görüntüleri).
4. **App Lab Başvurusu:** Oyunun Meta Store Developer paneline MR (Mixed Reality) etiketiyle yüklenmiş hali.
5. **AR Gameplay Fragmanı:** Oyuncunun kendi odasında oynarken dışarıdan ve içeriden çekilmiş karma gerçeklik videosu.
6. **Git ve Kanban Geçmişi:** İş günlerini kapsayan günlük commitler, Stacked PR'lar ve dolu bir staj defteri kanıtı.