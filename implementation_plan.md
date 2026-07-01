

# Implementation Plan — Recycle Rush VR

### Sanal Gerçeklik (VR) Geri Dönüşüm Simülatörü

> **Proje Adı:** Recycle Rush VR
> **Geliştirici Ekip:** Emre Satıl & Hakan Üzer
> **Süre:** 20 iş günü (4 hafta)
> **Platform:** Mobil VR (Standalone VR, Meta Quest)
> **Oyun Motoru:** Unity 3D (LTS)
> **Doküman Sürümü:** v3.0 (Kapsam daraltma, hazır asset kullanımı, haptic/polish entegrasyonu ve Meta Store yayını hedefiyle güncellendi)
> **Doküman Türü:** Implementation Plan / Tek Doğruluk Kaynağı (Single Source of Truth)

> **Bu dokümanın amacı:** Projemizin mimarisini, oyun döngüsünü, dizin yapısını ve proje yönetim kurallarını gri nokta bırakmadan tanımlamaktır. Geliştirme süreci tamamen bu plana birebir uyarak ilerleyecektir. Staj hocamızın yönlendirmesiyle 3D modelleme iş yükü projeden çıkarılmış; tamamen Unity C# kod mimarisine, temiz XR etkileşimlerine ve oyun hissiyatına (polish) odaklanılmıştır. Plandan sapma gerekirse önce bu doküman güncellenecek, sonra kod yazılacaktır.

---

## İçindekiler

1. Yönetici Özeti
2. Problem Tanımı ve Hedefler
3. Kapsam
4. Teknoloji Yığını
5. Unity Dizin Ağacı ve Proje Mimarisi
6. Temel Oynanış ve Mekanikler (Core Game Loop)
7. Sistem Mimarisi & Manager Sınıfları
8. Etkileşim ve Geri Bildirim Matrisi (Haptic & Audio)
9. Puanlama, Olaylar (Events) ve Zorluk Eğrisi
10. Sanat Yönetimi (Low-Poly)
11. UI (Kullanıcı Arayüzü) ve Sahne Envanteri
12. Build & Meta Store Dağıtım Süreci
13. Kodlama Standartları ve Optimizasyon
14. Test Stratejisi
15. Dokümantasyon Yapısı (docs/)
16. 20 İş Günlük Yol Haritası (Gün Gün Detaylı Plan)
17. Haftalık Sprint Özeti
18. Git Workflow & PR Süreci
19. GitHub Projects Board & Issue Yönetimi
20. Daily Standup
21. Risk Yönetimi
22. Definition of Done
23. Teslim Edilecekler

---

## 1. Yönetici Özeti

Recycle Rush VR; oyuncuların çevre bilincini artırmayı hedefleyen, reflekslere ve el-göz koordinasyonuna dayalı bir tek oyunculu sanal gerçeklik (VR) simülasyonudur. Ekip olarak amacımız, oyunculara atık ayrıştırmayı eğlenceli ve hareketli bir yolla öğretmektir.

Oyuncu, hareket eden bir üretim bandının başında sabit durarak, saniyeler içinde önüne gelen atıkları doğru renkteki geri dönüşüm kutularına (Kağıt, Cam, Plastik) fırlatarak puan kazanır. Çıktımız, Meta Quest cihazlarında yüksek performansla (`72fps+`) çalışan, titreşim (haptic) ve ses geri bildirimleriyle cilalanmış bir VR uygulamasıdır.

---

## 2. Problem Tanımı ve Hedefler

### 2.1 Çözülen Problem

Eğitici oyunlar genellikle sıkıcı mekaniklere sahip olur ve VR ortamında hareket (locomotion) midesi bulantısına yol açabilir. Bizim çözümümüz, oyuncuyu sabit tutup (No Locomotion), objeleri oyuncuya getirmek ve öğrenme sürecini hızlı tempolu bir "Arcade" deneyimine çevirmektir.

### 2.2 Başarı Kriterleri (Definition of Success)

* [ ] XR Interaction Toolkit entegrasyonu kusursuz çalışacak; oyuncu objeleri doğal bir şekilde tutup fırlatabilecek.
* [ ] Geliştirme süresince sadece hazır 3D (Low-Poly) asset'ler kullanılarak kod mimarisine odaklanılacak.
* [ ] Bant hızı ve atık gelme sıklığı, oyuncunun puanına göre dinamik olarak zorlaşacak.
* [ ] Tutma ve doğru atma anlarında "Haptic Feedback" (VR kontrolcü titreşimi) kesinlikle çalışacak.
* [ ] Tüm repomuz ve iş akışımız GitHub Projects (Kanban) ve branch/PR sistemi ile yönetilecek.
* [ ] Oyun, staj sonunda Meta App Lab (Store) üzerinde incelemeye gönderilecek.

---

## 3. Kapsam

### 3.1 Kapsam İçi (20 günde teslim edilecek)

* Unity XR Interaction Toolkit entegrasyonu ve VR Rig kurulumu.
* Hazır Low-Poly fabrika ortamı, taşıma bandı ve atık asset'lerinin import edilmesi.
* Spawner (Rastgele obje üretici) ve Bant fiziği scriptleri.
* `Grab & Throw` (Tut-Fırlat) mekaniklerinin kodlanması.
* Çarpışma algılama (Trigger) ve Puanlama sistemi.
* Görsel (VFX), İşitsel (Audio) ve Dokunsal (Haptic) cila (polish).
* World Space (VR içi) UI / Skor tabelası.
* Cihaz üzerinde test ve Meta App Lab'e yükleme.

### 3.2 Kapsam Dışı (Gelecek faz veya projeden çıkarılanlar)

* 3D Modelleme, UV açma veya animasyon hazırlama (Süreyi koda harcıyoruz).
* Mekan içinde yürüme (Locomotion) veya ışınlanma (Teleport).
* Multiplayer (Çok oyunculu) altyapı veya veritabanı skor tablosu.

---

## 4. Teknoloji Yığını

Tüm sistem, mobil VR optimizasyonu ve temiz C# mimarisi prensipleriyle kurgulanmıştır.

| Katman | Teknoloji | Gerekçe |
| --- | --- | --- |
| **Oyun Motoru** | Unity 3D (2022 LTS veya üstü) | Standart endüstri motoru, mobil VR performans dostu. |
| **Dil** | C# | Nesne yönelimli, temiz kod mimarisine uygun. |
| **VR Kütüphanesi** | Unity XR Interaction Toolkit (XRI) | Standartlaştırılmış el takibi, etkileşim (Grab/Throw). |
| **Versiyon Kontrol** | Git + GitHub | Main dalı korumalı, PR bazlı geliştirme. |
| **Proje Yönetimi** | GitHub Projects (Kanban) | To Do, In Progress, Review, Done akışıyla iş takibi. |
| **Görsel Assetler** | Unity Asset Store / CGTrader | Hoca geri bildirimi: Kapsamı daraltmak için hazır Low-Poly. |
| **Test & Build** | Meta Quest Developer Hub (MQDH) | Kablosuz build, performans profilleme (Logcat). |

---

## 5. Unity Dizin Ağacı ve Proje Mimarisi

Dışarıdan indirilen assetler ile kendi yazdığımız kodların birbirine girmemesi için klasör yapımız kesin kurallara bağlıdır.

```
Recycle-Rush-VR/
├── Assets/
│   ├── _App/                   # BİZİM GELİŞTİRDİĞİMİZ ÇEKİRDEK DOSYALAR
│   │   ├── Scenes/             # MainGame.unity
│   │   ├── Scripts/
│   │   │   ├── Core/           # GameManager.cs, ScoreManager.cs
│   │   │   ├── Environment/    # ConveyorBelt.cs, WasteSpawner.cs, DestroyZone.cs
│   │   │   ├── Interaction/    # BinTrigger.cs, WasteItem.cs
│   │   │   └── Polish/         # HapticManager.cs, AudioManager.cs, VfxManager.cs
│   │   ├── Prefabs/            # Kendi oluşturduğumuz, script eklenmiş Prefab'lar
│   │   ├── Materials/          # Renk paletlerimiz
│   │   ├── Audio/              # BGM.mp3, Ding.wav, Buzzer.wav
│   │   └── UI/                 # Fontlar ve Canvas prefabları
│   ├── ThirdParty/             # Asset Store'dan inen hazır 3D modeller ve eklentiler
│   └── XR/                     # XR Interaction Toolkit konfigürasyon dosyaları
├── docs/                       # GDD, Mimari notlar, Store görselleri (GitHub'da)
├── .gitignore                  # Unity için standart gitignore
└── implementation_plan.md      # BU DOSYA

```

---

## 6. Temel Oynanış ve Mekanikler (Core Game Loop)

**Oyun Döngüsü Şeması:**

```
Spawner atık üretir → Bant atığı oyuncuya taşır → Oyuncu atığı tutar (Grab)
        │                                                     │
        ▼                                                     ▼
Atık yere düşer (-2 Puan)                          Oyuncu kutuya fırlatır (Throw)
                                                              │
                     ┌────────────────────────────────────────┴────────────┐
                     ▼                                                     ▼
         Yanlış Kutuya Girdi (-5 Puan)                     Doğru Kutuya Girdi (+10 Puan)
         (Buzzer sesi + Kırmızı X)                         (Ding sesi + Yeşil Efekt + Haptic)

```

---

## 7. Sistem Mimarisi & Manager Sınıfları

Projemizde "Singleton" veya modüler sistem mimarisi kullanılarak scriptlerin birbiriyle iletişimi sağlanacaktır.

* `GameManager.cs`: Oyunun anlık durumunu (Başladı, Bitti, Duraklatıldı) kontrol eder.
* `ScoreManager.cs`: Puanı tutar, `OnScoreChanged` event'ini tetikleyerek UI'ı günceller ve `GameManager`'a zorluğu artırması için sinyal gönderir.
* `WasteSpawner.cs`: Kendisine verilen prefab listesinden (Cam, Kağıt, Plastik) `Instantiate` ile obje üretir. Havuzlama (Object Pooling) ihtiyacı duyulursa bu sınıfa eklenecektir.
* `ConveyorBelt.cs`: Üzerindeki RigidBody'lere `FixedUpdate` içinde sabit bir `Vector3` hızı ekler.

---

## 8. Etkileşim ve Geri Bildirim Matrisi (Haptic & Audio)

Sürükleyiciliği (Immersion) sağlamak için her etkileşimin bir geri bildirimi olmalıdır.

| Etkileşim | Görsel (VFX) | İşitsel (Audio) | Dokunsal (Haptic) |
| --- | --- | --- | --- |
| Atığı Banttan Tutma | Obje elde hizalanır | Hafif 'Pop' sesi | Kısa ve düşük yoğunluklu titreşim (0.1s, 0.3 amp) |
| Doğru Kutuya Atma | Kutudan yeşil partikül çıkar | Tatmin edici 'Ding!' | Çift vuruşlu orta titreşim (0.2s, 0.5 amp) |
| Yanlış Kutuya Atma | Kutudan kırmızı partikül çıkar | Rahatsız etmeyen 'Buzzer' | Uzun, boğuk titreşim (0.4s, 0.8 amp) |
| Atığı Yere Düşürme | Obje silinir (Destroy) | Düşme / Yere çarpma sesi | Titreşim yok |

---

## 9. Puanlama, Olaylar (Events) ve Zorluk Eğrisi

Kodlar arasında spagetti bağlantılar (sürekli birbirini GetComponent ile arama) yapmamak için **C# Action/Event** yapısı kullanacağız.

* `BinTrigger.cs` tetiklendiğinde `ScoreManager.AddScore(10)` fonksiyonunu çağırır.
* **Dinamik Zorluk:** `ScoreManager` içindeki puan;
* **50'yi geçince:** Bant hızı 1.2x, Spawner hızı 1.2x artar.
* **100'ü geçince:** Bant hızı 1.5x, Spawner hızı 1.5x artar.
* **200'ü geçince:** Maksimum hız (Arcade Modu başlar).



---

## 10. Sanat Yönetimi (Low-Poly)

* **Tarz:** Performansı (72-90 FPS) korumak ve göz yormamak için Low-Poly kullanılacaktır.
* **Renk Paleti:**
* **Mavi Kutu:** Kağıt / Karton
* **Yeşil Kutu:** Cam
* **Sarı Kutu:** Plastik


* **Aydınlatma:** Gerçek zamanlı gölgeler (Realtime Shadows) minimumda tutulacak; çevre ışıkları `Baked` (pişirilmiş) kullanılarak cihaz işlemcisi rahatlatılacaktır.

---

## 11. UI (Kullanıcı Arayüzü) ve Sahne Envanteri

* **Canvas Tipi:** `World Space`. VR'da Screen Space kullanılmaz; menüler ve skor tabelaları fiziksel bir pano gibi oyun dünyasına yerleştirilir.
* **Ana Pano:** Bandın arkasında büyük, oyuncunun rahat görebileceği bir dijital skor tabelası.
* Anlık Puan göstergesi.
* Hata Sayacı (isteğe bağlı).


* **Font:** Okunabilirliği yüksek, kalın hatlı (örn. Roboto veya Fredoka One).

---

## 12. Build & Meta Store Dağıtım Süreci

* **Build Target:** Android (ASTC Compression).
* **XR Plugin Management:** OpenXR ve Oculus seçili.
* **Meta App Lab:** Geliştirme bittiğinde `com.Emre.RecycleRushVR` paket adıyla Keystore imzalanarak APK alınacak ve Meta Store kontrol paneline yüklenecektir.

---

## 13. Kodlama Standartları ve Optimizasyon

* İsimlendirme: `PascalCase` (Sınıflar, Metotlar), `camelCase` (Lokal Değişkenler), `_camelCase` (Private Field'lar).
* Garbage Collection (GC) Optimizasyonu: `Update` içinde `new`, `GetComponent` veya `Find` kullanılmayacak. Tüm referanslar `Awake` veya `Start` içinde önbelleğe alınacak (Cache).
* Hatalar Unity Console'da bırakılmayacak, Debug.Log'lar build alınırken kapatılacak.

---

## 14. Test Stratejisi

* **Editör Testi (Play Mode):** XR Device Simulator kullanılarak mouse/klavye ile ellerin ve sistemin temel çalışması test edilecek.
* **Cihaz Testi (Device Build):** Haftada en az 2 kez Meta Quest gözlüğüne kablosuz build atılarak performans (FPS), haptic hissiyatı ve bant hızının oyuncuyu yorup yormadığı test edilecek.

---

## 15. Dokümantasyon Yapısı (docs/)

Hocamızın tavsiyesiyle tüm belgeler repoda düzenli tutulacaktır.

```
docs/
├── design/
│   ├── gdd.md                  # Oyun Tasarım Belgesi
│   └── reference-images/       # UI veya Asset referans görselleri
├── store/
│   ├── description.md          # Meta Store için oyun açıklaması
│   └── screenshots/            # App Lab'e yüklenecek 1080x1080 ve 16:9 görseller
└── architecture/
    └── game-loop-diagram.md    # Mekanik şemalar

```

---

## 16. 20 İş Günlük Yol Haritası (Gün Gün Detaylı Plan)

Her gün en az 1 anlamlı commit atılması **zorunludur**.

### 🟦 Faz 0 — Kurulum ve Proje İskeleti (Gün 1-4)

* **Gün 1:** GitHub repository'sinin oluşturulması, Projects (Kanban) board kurulumu, Unity projesinin (LTS) açılması ve `.gitignore` ayarı.
* **Gün 2:** Unity XR Interaction Toolkit ve OpenXR paketlerinin kurulumu. Meta Quest build ayarlarının (Android) yapılması.
* **Gün 3:** Asset Store / dış kaynaklardan Low-Poly çevre, bant ve kutu modellerinin bulunup projeye import edilmesi (`ThirdParty/` klasörüne).
* **Gün 4:** Ana oyun sahnesinin oluşturulması (Blockout). XR Origin (Oyuncu kamerası ve elleri) sahneye yerleştirilmesi. İlk cihaz testinin alınması.

### 🟩 Faz 1 — Çekirdek Mekanikler (Gün 5-9)

* **Gün 5:** `BeltMovement.cs` scriptinin yazılması. Taşıma bandı üzerindeki fizik objelerinin X ekseninde pürüzsüz kaydırılması.
* **Gün 6:** Atık prefab'larının (Cam, Plastik, Kağıt) oluşturulması, Collider ve RigidBody ayarlarının yapılıp `Tag`'lerinin atanması.
* **Gün 7:** XR Grab Interactable ayarları. Oyuncunun banttan gelen atığı VR kontrolcüsüyle düzgünce tutabilmesinin (Grab) test edilmesi.
* **Gün 8:** `WasteSpawner.cs` scriptinin yazılması. Belirli saniye aralıklarında rastgele atık objelerinin bandın başında üretilmesi.
* **Gün 9:** Geri dönüşüm kutularına BoxCollider(Trigger) eklenmesi ve içine düşen objenin etiketini kontrol eden `BinChecker.cs` kodunun yazılması.

### 🟨 Faz 2 — Oyun Döngüsü ve UI (Gün 10-14)

* **Gün 10:** `ScoreManager.cs` ve `GameManager.cs` sınıflarının oluşturulması. Doğru atışlarda puan ekleme, yanlışta çıkarma mantığının kurulması.
* **Gün 11:** Bandın sonuna `DestroyZone.cs` eklenerek tutulamayan objelerin sahneden silinmesi ve eksi puan düşülmesi.
* **Gün 12:** World Space Canvas eklenmesi. Oyuncunun karşısındaki duvara anlık skoru ve durumu gösteren dijital ekranın tasarlanması.
* **Gün 13:** Dinamik zorluk algoritmasının koda eklenmesi. (Puan 50-100-200 oldukça spawner süresinin kısalması ve bant hızının artması).
* **Gün 14:** Core game loop'un (Oyun döngüsünün) uçtan uca cihaz üzerinde test edilmesi ve varsa kritik bug'ların (hataların) fixlenmesi.

### 🟧 Faz 3 — Polish (Cila) Aşaması (Gün 15-18)

* **Gün 15:** `AudioManager.cs` yazılması. Arka plan Lo-Fi müziğinin (BGM) ve etkileşim seslerinin (Tutma, Ding, Buzzer) projeye entegrasyonu.
* **Gün 16:** Kutu içine doğru atık girdiğinde tetiklenecek "Yeşil Onay Partikül" (Particle System) efektlerinin yapılması.
* **Gün 17:** **Haptic Feedback:** XR API kullanılarak `HapticManager.cs` yazılması. Obje tutulduğunda ve doğru kutuya atıldığında titreşim verilmesi.
* **Gün 18:** Performans optimizasyonu. Işıkların Bake edilmesi (Lightmapping), gereksiz Update döngülerinin temizlenmesi.

### 🟥 Faz 4 — Meta Store, Test ve Teslim (Gün 19-20)

* **Gün 19:** Projenin son Build'inin (Release APK) alınması. Meta Quest Developer Hub (MQDH) üzerinden cihazda sıfırdan kurulum ve test.
* **Gün 20:** Meta App Lab (Mağaza) Developer Paneli işlemlerinin tamamlanması. Oyun ikonları, screenshot'lar ve açıklama metninin yüklenmesi. Staj hocalarına son projenin (ve GitHub reposunun) teslimi.

---

## 17. Haftalık Sprint Özeti

| Hafta | Günler | Hedef | Çıktı |
| --- | --- | --- | --- |
| **1** | 1–5 | Altyapı, XR kurulumu, Assetlerin dizilimi ve bant fiziği. | İçinde VR ellerimizle durduğumuz, bandın aktığı sahne. |
| **2** | 6–10 | Spawner, atıklar, Grab/Throw mekaniği ve Kutu triggerları. | Gelen objeleri tutup doğru kutulara atabildiğimiz prototip. |
| **3** | 11–15 | Puanlama, Canvas UI, dinamik zorluk ve oyun döngüsü. | Puanlandığımız, UI'ın çalıştığı ve oyunun gittikçe hızlandığı tam döngü. |
| **4** | 16–20 | Ses, VFX, Haptic (Titreşim), Optimizasyon ve App Lab yayını. | Titreşim ve sesle cilalanmış, mağazaya yüklemeye hazır VR oyunu. |

---

## 18. Git Workflow & PR Süreci

Staj hocalarının beklentisi doğrultusunda:

* Asla doğrudan `main` dalına kod atılmayacaktır (Push yok).
* Her yeni özellik için `main` dalından yeni branch açılır (Örn: `feature/spawner-system`, `feature/haptic-feedback`).
* Geliştirme bitince GitHub üzerinden Pull Request (PR) açılır. Hocalar (`berkay_calti@hotmail.com` ve `umitreva@icloud.com`) reviewer olarak eklenir.
* PR açıklamasına ne yapıldığı yazılır, hocalardan onay (Approve) gelince kod `main` ile birleştirilir (Merge).

**Commit Mesaj Standardı:**

* `feat(core): bant hareket sistemi eklendi`
* `fix(ui): skor tabelası yenilenmeme sorunu çözüldü`
* `design(env): çevre assetleri import edildi`

---

## 19. GitHub Projects Board & Issue Yönetimi

* Repomuzun **Projects** sekmesindeki Kanban panosu aktif olarak kullanılacaktır.
* Yol haritasındaki 20 günün her bir adımı bir **Issue** olarak açılacaktır.
* Sütunlar: **To Do** / **In Progress** / **Review** / **Done**.
* İşe başlamadan önce Issue *In Progress*'e çekilecek, PR açıldığında *Review*'a alınacak, merge edilince *Done* yapılacaktır.

---

## 20. Daily Standup

Her iş günü stajyer ekibi (Emre ve ekip arkadaşı) olarak standup değerlendirmesi yapacağız:

1. **Dün ne yaptım?**
2. **Bugün hangi Task/Issue üzerinde çalışacağım?**
3. **Beni engelleyen (Blocker) bir durum var mı?**
Bu özetler, o gün atılan commit'lerle (staj günlüğüyle) birebir uyumlu olacaktır.

---

## 21. Risk Yönetimi

| Risk | Olasılık | Etki | Önlem |
| --- | --- | --- | --- |
| VR'da Düşük Performans (FPS Dropları) | Orta | Yüksek | Kesinlikle Low-Poly asset kullanımı. Işıkların Bake edilmesi (Realtime gölge iptali). |
| Git Merge Çakışması (Scene Conflict) | Yüksek | Orta | İki kişi aynı anda `.unity` sahne dosyasını düzenlemeyecek. Modüller Prefab olarak geliştirilip sahneye atılacak. |
| Haptic/Titreşim API Uygunsuzluğu | Düşük | Düşük | Yeni XRI dokümantasyonu baz alınacak. |
| 20 Güne Sığamama | Orta | Orta | 3D modelleme yapılmayacak, hazır assetlerle süreden tasarruf edilecek. |

---

## 22. Definition of Done (Bitti Kriteri)

Bir görevin tamamen bitti sayılması için koşullar:

* [ ] Unity Console'da derleme hatası (Error) bulunmamaktadır.
* [ ] XR Rig ve etkileşimler Quest simülatöründe (veya cihazda) test edilmiştir.
* [ ] Kod standartlara uygun yazılmış ve `feature` dalından Push edilmiştir.
* [ ] GitHub'da PR açılmış, hocalar tarafından onaylanmış ve `main`'e merge edilmiştir.
* [ ] Kanban kartı *Done* sütununa taşınmıştır.

---

## 23. Teslim Edilecekler

1. **Unity Kaynak Kodu:** Temiz C# mimarisiyle, `main` branch üzerinde çalışan proje.
2. **Build Dosyası (APK):** Gözlüğe doğrudan kurulabilir sürüm.
3. **Görsel ve Dokümantasyon:** Plandaki `docs/` klasörü içindeki tüm gereksinimler.
4. **App Lab Başvurusu:** Oyunun Meta Store Developer paneline yüklenmiş hali.
5. **Git ve Kanban Geçmişi:** İş günlerini kapsayan günlük commitler, onaylanmış PR'lar ve dolu bir staj defteri kanıtı.
