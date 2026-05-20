# 🎮 DOWN — Isometric Action & Combat Game

[![Godot Engine](https://img.shields.io/badge/Godot-4.x%20%28.NET%2F%23C%29-478CBF?style=for-the-badge&logo=godot-engine&logoColor=white)](https://godotengine.org)
[![Project Status](https://img.shields.io/badge/Status-Under%20Development-orange?style=for-the-badge&logo=gitkraken)](https://github.com)
[![Language](https://img.shields.io/badge/Language-C%23-green?style=for-the-badge&logo=c-sharp)](https://dotnet.microsoft.com)

> **⚠️ STATUS: WORK IN PROGRESS (AKTIF DALAM PENGEMBANGAN)**
>
> Game ini sedang berada dalam tahap pengembangan intensif. Fitur utama engine isometrik, kontrol karakter, AI musuh, sistem combat, dan manajemen game state dasar telah diimplementasikan, tetapi pengembangan aset visual, level, dan polishing masih terus berjalan!

---

## 📖 Deskripsi Singkat
**DOWN** adalah game bergenre action-combat isometrik 2D yang dibangun menggunakan **Godot Engine 4.x (C#)**. Game ini berfokus pada pertempuran taktis bertempo cepat, di mana pemain harus menavigasi arena isometrik, menghindari serangan musuh dengan *dodge roll*, dan membangun skor tertinggi lewat rantai serangan (*combo streak*).

---

## 🛠️ Fitur Utama (Implemented Core)

1. **Isometric Engine & Camera**: 
   - Konversi gerakan 8-arah (WASD) langsung diproyeksikan ke sistem koordinat isometrik secara akurat.
   - Kamera dinamis yang mengikuti pemain dengan fitur *Camera Shake* responsif terhadap serangan maupun manuver.
2. **Player Movement & Dodge Roll**:
   - Kontrol responsif dengan perlambatan berbasis gesekan (*friction deceleration*).
   - Fitur *Sprint* untuk penjelajahan cepat.
   - *Dodge Roll* dengan *Invincibility Frames (i-frames)* untuk menghindari *damage*.
3. **Responsive Combat & Combo System**:
   - Sistem *Hitbox* & *Hurtbox* real-time.
   - *Combo Multiplier Tier*: Semakin lama rantai hit/kill tanpa terkena hit/waktu habis, semakin besar poin skor yang diperoleh (×1.5, ×2.0, hingga ×3.0).
4. **Enemy AI & Grunt System**:
   - Musuh yang berpatroli, mengejar pemain (*chase*), dan melakukan *telegraphed attacks*.
5. **Tactical Game Manager & Pause**:
   - Global singleton (Autoload) untuk kontrol transisi status game (`MainMenu`, `Playing`, `Paused`, `GameOver`).

---

## 📂 Struktur Folder Project

Berikut adalah peta struktur direktori di project **DOWN**:

```bash
DOWN/
├── .godot/                  # Cache & metadata internal Godot
├── Assets/                  # Seluruh resource visual dan audio
│   ├── Audio/               # Aset suara (.wav, .mp3, dll.)
│   ├── Sprites/             # Aset gambar/2D Sprite
│   └── Tiles/               # Ubin/Tile untuk rancangan Map Isometrik
├── Levels/                  # Scene khusus untuk rancangan Level Map
├── Scenes/                  # Kumpulan Scene Utama (.tscn)
│   ├── Main.tscn            # Scene utama game (Arena pertarungan)
│   ├── Player.tscn          # Scene entitas Player
│   ├── Enemy.tscn           # Scene entitas Musuh
│   ├── HUD.tscn             # UI heads-up display (Health, Combo, Score)
│   └── DeathScreen.tscn     # Layar game over
├── Scripts/                 # Kode sumber (C#)
│   ├── Audio/               # (Placeholder) Skrip audio manager masa depan
│   ├── Player/              # (Placeholder) Skrip khusus behavior pemain
│   ├── Enemy/               # (Placeholder) Skrip AI musuh tambahan
│   ├── UI/                  # (Placeholder) Skrip UI & Menu
│   └── Core/                # PUSAT LOGIKA CORE ENGINE & SISTEM GAME
│       ├── GameManager.cs       # Singleton state game, skor, combo, & pause
│       ├── PlayerController.cs  # Pergerakan isometrik pemain, input, & dodge roll
│       ├── EnemyBase.cs         # Base class/template dasar semua tipe musuh
│       ├── EnemyAI.cs           # State machine AI musuh (Patrol, Chase, Attack)
│       ├── EnemyGrunt.cs        # Implementasi tipe musuh dasar (Grunt)
│       ├── Health.cs            # Komponen penanganan nyawa, damage, & i-frames
│       ├── AttackSystem.cs      # Mekanisme serangan pemain & trigger hitbox
│       ├── CombatTrigger.cs     # Area deteksi serangan
│       ├── IsometricCamera.cs   # Kamera follow + camera shake effect
│       ├── IsometricUtils.cs    # Helper konversi ruang 2D flat ke isometrik
│       ├── LevelManager.cs      # Penanganan spawning, progress, dan ganti level
│       ├── TilemapSetup.cs      # Inisialisasi peta tilemap isometrik
│       ├── PlaceholderSetup.cs  # Programmatic drawing untuk sprite visual sementara
│       ├── HUD.cs               # Handler UI di layar aktif (HP bar, combo text)
│       └── DeathScreen.cs       # Handler UI ketika pemain kalah
├── DOWN.csproj              # Konfigurasi project C#/.NET
├── DOWN.sln                 # Solution file untuk IDE (VS Code / Visual Studio)
└── project.godot            # File konfigurasi utama engine Godot
```

> [!NOTE]
> Folder kosong seperti `Scripts/Player`, `Scripts/Enemy`, `Scripts/UI`, dan `Scripts/Audio` dipersiapkan sebagai wadah modularisasi kode di masa mendatang ketika fitur game semakin meluas, memisahkannya dari folder `Core`.

---

## 🎮 Cara Menjalankan Project

### Prasyarat (Requirements):
1. **Godot Engine 4.x (.NET / Mono Edition)**. Pastikan Anda mengunduh versi Godot yang mendukung C#.
2. **.NET SDK 6.0 / 8.0** terinstal di sistem Anda.
3. IDE pendukung seperti **VS Code** (dengan ekstensi C# Dev Kit) atau **Visual Studio 2022**.

### Langkah Instalasi:
1. Clone atau download folder project ini.
2. Buka **Godot Engine (C# Edition)**.
3. Pilih **Import**, lalu arahkan ke lokasi folder project ini dan pilih file `project.godot`.
4. Build solution C# terlebih dahulu dengan menekan tombol **Build** di pojok kanan atas editor Godot sebelum menjalankan game untuk pertama kalinya.
5. Tekan tombol **Play (F5)** untuk menjalankan scene utama (`Main.tscn`).

---

## 🕹️ Skema Kontrol (Controls)

| Aksi | Tombol Keyboard / Mouse | Deskripsi |
| :--- | :--- | :--- |
| **Bergerak** | `W` `A` `S` `D` / `Tombol Arah` | Berjalan ke 8 arah isometrik |
| **Sprint** | `Shift` (Tahan) | Berlari lebih cepat |
| **Serang** | `Space` / `Klik Kiri` | Menyerang ke arah hadapan karakter |
| **Dodge Roll** | `Space` / `Tombol Dodge` | Roll cepat disertai kekebalan sementara (*i-frames*) |
| **Pause** | `Esc` / `Tab` | Menghentikan permainan sementara (Tactical Pause) |

---

## 📈 Roadmap Pengembangan (Next Steps)
- [x] Kerangka Utama Engine Isometrik (Gerakan & Proyeksi 2.5D)
- [x] AI Musuh Tingkat Dasar (Patrol & Chase State Machine)
- [x] Combat System (Hitbox, Damage, Invincibility, dan Combo multiplier)
- [ ] Integrasi Aset Gambar Asli (Mengganti *Placeholder Setup*)
- [ ] Pembuatan Tilemap Isometrik yang Lebih Bervariasi
- [ ] Sistem Audio (Musik latar & Sound Effect serangan/hurt/dodge)
- [ ] Desain Berbagai Jenis Musuh Baru (Ranged, Heavy, Boss)
