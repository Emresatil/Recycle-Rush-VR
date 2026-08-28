<div align="center">

# ♻️ Recycle Rush MR
### Mixed Reality (AR/MR) Recycling Simulator for Meta Quest

[![Unity](https://img.shields.io/badge/Unity-2022_LTS-000000?style=for-the-badge&logo=unity&logoColor=white)](https://unity.com/)
[![Platform](https://img.shields.io/badge/Platform-Meta_Quest_2%20%7C%203%20%7C%20Pro-0668E1?style=for-the-badge&logo=meta&logoColor=white)](https://www.meta.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Event--Driven%20%7C%20State%20Machine-2ea44f?style=for-the-badge)](#technical-architecture)
[![Status](https://img.shields.io/badge/Status-Release_Candidate-blueviolet?style=for-the-badge)](#)

*Transform your physical room into an interactive, high-score chasing recycling center.*

</div>

---

## 📖 About The Project

**Recycle Rush MR** is a next-generation Mixed Reality (MR) simulator designed to gamify the waste-sorting process. Initially conceptualized as a standard Virtual Reality (VR) conveyor belt game, the project was completely re-architected in its second month to leverage **Meta Passthrough** and **Spatial Anchors**. 

By removing virtual locomotion and physical friction constraints, the game seamlessly integrates with the player's real-world environment. Trash falls from spatial portals on your ceiling directly onto your actual floor. The result is a physically engaging, stutter-free "Arcade" experience that encourages active movement, quick decision-making, and environmental awareness.

---

## ✨ Key Features & Mechanics

- **Mixed Reality Passthrough:** See your real room while interacting with high-quality holographic bins and dynamic spatial portals.
- **Deep Progression System:** 15 carefully balanced levels featuring scaling difficulty, decreasing portal drop rates, and unique mission objectives.
- **Dynamic Economy (XP & Coin):** Earn XP and Coins through successful sorts. Unlock achievements, level up, and build your high score.
- **Advanced Combo & Grace System:** Chain correct throws to build up to a **x5 Multiplier**. Utilize the "Grace" mechanic to save a combo from breaking during critical moments.
- **Random Events:** The `EventManager` dynamically injects 6 unique game-altering events (e.g., *Speed Mode, Slow Motion, Double XP, Lucky Drop*).
- **Golden Waste System:** A rare, high-value item with dynamic spawn rates (scaling from 5% to 25% in the final levels).
- **Adaptive Audio & Spatial Haptics:** Features 3D Spatial Audio and an **Adaptive BGM Pitch** system that dynamically increases the music tempo during the final 30 seconds to elevate adrenaline.

---

## 🏗️ Technical Architecture & Design Patterns

Designed with scalability, performance, and clean code principles in mind. This repository strictly adheres to industry-standard software architectures.

### 1. Robust State Machine (`GameManager.cs`)
The core loop is managed by a strictly typed Finite State Machine (FSM):
`MainMenu` ➔ `Placement (AR Anchor Setup)` ➔ `Countdown` ➔ `Playing` ➔ `Paused` / `GameOver`
This ensures deterministic transitions and prevents race conditions between UI and Gameplay layers.

### 2. Event-Driven Architecture
To decouple systems and prevent monolithic "Spaghetti Code", inter-system communication is handled via C# `Action` events.
*Example:* When `BinTrigger.cs` detects a correct sort, it fires `OnWasteProcessed`. `ScoreManager`, `ComboManager`, and `AchievementManager` listen to this event independently, calculate their respective logic, and update the UI without direct dependencies.

### 3. Persistent JSON Serialization (`SaveManager.cs`)
All player data (Current Level, Total XP, Coins, Unlocked Achievements, and Audio Settings) is securely serialized to JSON and stored in `Application.persistentDataPath`. The architecture includes fallback mechanisms to prevent data corruption.

### 4. Zero-Allocation Object Pooling
To meet the strict **72-90 FPS** requirement for Meta Quest, `Instantiate` and `Destroy` are completely banned during the `Playing` state. `PortalSpawner.cs` and `VfxManager.cs` utilize generic Object Pools, resulting in **0 Bytes of GC Allocation (Garbage Collection Spikes)** during runtime.

### 5. Catastrophic Merge Conflict Resolution
During development, the team successfully utilized Git best practices to recover from catastrophic merge conflicts (involving `UIManager.cs` and `GameManager.cs`), demonstrating strong version control proficiency, file integrity restoration, and clean PR merging.

---

## 🎮 Level Design & Balancing

The game features an exponential difficulty and XP curve, mathematically balanced to ensure a state of "Flow". 

| Metric | Level 1 (Tutorial) | Level 8 (Mid-Game) | Level 15 (Arcade Master) |
| :--- | :---: | :---: | :---: |
| **Target Score** | 50 | 620 | 3000 |
| **Portal Spawn Rate** | 3.5s | 1.9s | 0.9s (Dual Portals) |
| **Golden Waste Chance**| 5% | 14% | 25% |
| **Unique Event** | Basic Sorting | Flawless Accuracy | All Events Active |

*Equation used for XP requirements:* `Mathf.RoundToInt(100 * Mathf.Pow(Level, 1.4f))`

---

## 🏆 Achievement System

A hidden and public achievement matrix designed to increase replayability. Includes milestone tracking (Thresholds at 50%, 75%, 90%) and immediate reward injection.
* **Golden Rain:** Catch 3 Golden Wastes in a single run. (Hidden)
* **Cool-Headed Master:** Reach a x5 combo without using the Grace mechanic.
* **Perfect Streak:** Make 20 correct throws flawlessly.

---

## 🛠️ Development Workflow (Git & Agile)

This project was developed over a 20-day sprint cycle simulating a professional studio environment:
- **Agile Kanban:** Tracked via GitHub Projects (To Do, In Progress, Review, Done).
- **Git Flow:** Direct pushes to `main` were prohibited. Development occurred on isolated `feature/` branches.
- **Stacked PRs:** Code was integrated using Pull Requests requiring peer review and approval before merging.
- **Atomic Commits:** Commit messages followed semantic versioning (e.g., `fix(core): resolve merge conflicts in game manager`).

---

## 🚀 Installation & Build

1. Clone the repository: `git clone https://github.com/Emresatil/Recycle-Rush-VR.git`
2. Open the project in **Unity 2022 LTS** (or newer).
3. Ensure **Meta XR Core SDK** and **XR Interaction Toolkit** are installed via Package Manager.
4. Switch platform to **Android** (ASTC Compression enabled).
5. Build and deploy to your Meta Quest device via Meta Quest Developer Hub (MQDH).

---

## 👥 Meet The Team

**Emre & Hakan**
*Internship Developers & Technical Architects*
Developed as a capstone internship project to demonstrate advanced proficiency in XR development, C# software architecture, and agile project management.