# Spesifikasi Desain: Combat Overhaul (Drakantos Style)
**Tanggal:** 2026-06-23
**Fase:** Awal (Mekanik & Game Feel)

## 1. Tujuan
Mengubah rasa permainan (*game feel*) dari game *top-down shooter* generik menjadi *Action RPG* bertempo cepat yang *skill-based* (seperti Drakantos). Fokus utama adalah merombak pergerakan pemain dan sistem serangan.

## 2. Arsitektur & Komponen Utama

### A. Sistem Pergerakan (PlayerController.cs)
- **Snappy Movement:** Karakter tidak meluncur seperti di atas es. Akselerasi dan friksi diperbesar sehingga karakter langsung berhenti ketika tombol arah dilepas.
- **Dash / Dodge Roll:**
  - Pemicu: Tombol `Space` atau `Shift`.
  - Efek: Karakter melesat ke arah jalan (atau arah kursor) dengan kecepatan 3x lipat selama ~0.2 detik.
  - I-Frames: Kebal dari serangan (`HurtBox` dinonaktifkan sementara) selama animasi Dash berlangsung.
  - Pembatalan (Animation Canceling): Dapat memotong animasi serangan yang sedang berlangsung.
  - Cooldown: ~1.5 detik.

### B. Sistem Hero Kit (AttackSystem.cs)
Sistem menembak generik diganti dengan *Active Skills* (Hero Kit).
1. **LMB (Basic Attack): Combo 3-Hit**
   - Hit 1 & 2: Serangan cepat (*light attack*).
   - Hit 3 (Finisher): Serangan lebih lambat, mengunci pergerakan pemain (Animation Lock), memberikan *damage* besar dan efek *Knockback* ke musuh.
   - Jeda combo: Pemain harus menyambung combo dalam 0.5 detik, jika tidak maka combo me-reset.
2. **Tombol Q (Skill 1): Cleave / Dash Strike**
   - Serangan spesifik (sementara kita buat tebasan AOE atau serudukan).
   - Cooldown terpisah (misal: 5 detik).
3. **Tombol E (Skill 2): Buff / Ranged**
   - Sementara diset sebagai lemparan *projectile* atau penambah *movement speed*.
   - Cooldown terpisah (misal: 8 detik).

### C. Combat Impact (Game Feel)
- **Hit-Stop:** Saat serangan pemain mengenai musuh, mesin Godot akan memperlambat waktu (TimeScale) mendekati 0 selama 0.05 detik untuk memberi kesan "berat" pada pukulan.
- **Screen Shake (Opsional tapi disarankan):** Getaran kecil pada kamera saat pukulan ketiga (Finisher) mendarat.

## 3. Data Flow
- Input -> `PlayerController` memvalidasi status (sedang Dash? Terkunci animasi?).
- Jika bisa menyerang -> Panggil `AttackSystem.TryAttack(SkillType)`.
- `AttackSystem` mengatur *cooldown*, menghentikan kecepatan `PlayerController` jika diperlukan (Animation Lock), dan mengaktifkan *HitBox*.
- Jika `HitBox` mengenai musuh, picu `HitStop` dan berikan *damage*.

## 4. Rencana Implementasi (Next Step)
1. Edit `PlayerController.cs` untuk menambahkan logika *Dash* dan merapikan *friction*.
2. Refactor `AttackSystem.cs` untuk mendukung sistem *Skill* 1 (Q), Skill 2 (E), dan memisahkan *logic* Basic Attack.
3. Sambungkan Input map di Godot Project Settings (`dash`, `skill_1`, `skill_2`).
