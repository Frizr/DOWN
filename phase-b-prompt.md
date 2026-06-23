# Phase B Prompt: Visual Overhaul & Pixel Art Premium

**Context:**
Game DOWN adalah proyek Action RPG top-down dengan tema dark fantasy/nostalgic pixel art (terinspirasi dari MMORPG Drakantos).
Di fase sebelumnya (Fase A), kita sudah merombak sistem combat menjadi lebih *snappy* (Friction tinggi), menambahkan mekanisme Dodge Cancel, dan sistem Skill Aktif (Cleave & Buff).
**Catatan Penting dari Fase A:** Tombol Q dan E *TIDAK* boleh dipakai untuk Skill karena sudah di-reserve untuk **Camera Zoom In / Zoom Out (POV)**. Skill 1 dan 2 telah dipindahkan sementara ke tombol angka `1` dan `2`.

**Tujuan Fase B:**
Kita ingin meningkatkan kualitas visual secara keseluruhan agar karakter dan aset tidak lagi terlihat seperti "AI slop" atau murahan. Kita ingin karakter yang benar-benar memancarkan nuansa premium seperti Drakantos.

**Langkah-langkah yang harus dilakukan oleh AI di sesi ini (Fase B):**
1. **Asset Replacement:** Ganti aset visual `Player` dan `Enemy` (terutama *spritesheet* dan *animations*) dengan aset baru beresolusi tinggi/premium pixel art yang cocok untuk Action RPG top-down (Drakantos style).
2. **Camera Integration (Q/E):** Pastikan skrip `IsometricCamera.cs` atau `PlayerController.cs` sudah benar-benar merespons tombol `Q` (Zoom In) dan `E` (Zoom Out) secara halus (smooth lerp). Jika belum ada, implementasikan fitur ini di IsometricCamera.
3. **Visual Polish:** Tambahkan elemen *juiciness* yang belum ada:
   - *Shadows* / bayangan di bawah karakter.
   - Partikel (Dust saat berlari/dodge).
   - *Hit flash* putih (Shader) saat musuh terkena serangan agar impact lebih terasa.
   - Perbaikan palet warna arena jika perlu.

**Instruksi Tambahan untuk AI:**
Selalu catat semua progres dan temuan di file Markdown terpisah (misalnya `phase-b-report.md`) agar rekam jejak tetap rapi. Gunakan pendekatan `subagent-driven-development` untuk mendelegasikan tugas-tugas visual dan kamera.
