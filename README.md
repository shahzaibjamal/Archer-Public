> 🛡️ **Portfolio Notice:** *This public repository contains the core codebase and architecture for the Archer Prototype. To comply with licensing agreements, premium 3D models and textures in `Assets/Art` have been omitted. The project is provided for code review and architectural demonstration.*

# 🏹 Archer Rogue-Like Prototype
### High-Performance Top-Down Combat Engine

A technical prototype for a top-down, rogue-like combat system built with **Unity URP**. This project serves as a showcase for advanced AI behaviors, agentic development workflows, and a strictly decoupled architecture optimized for low-end mobile performance.

<img width="250" alt="archer_01" src="https://github.com/user-attachments/assets/758c8215-5cfe-4302-bdb5-8f8017c38d97" />
<img width="250" alt="archer_02" src="https://github.com/user-attachments/assets/c28d4279-31d7-407d-a8a2-012977e44c25" />

---

## 🎮 Gameplay Mechanics

* **Precision Combat:** Control an archer using a refined **single-joystick** system. The player automatically locks onto the nearest enemy, with the ability to switch targets dynamically based on movement direction.
* **Tactical Maneuvering:** Use the 3D environment to your advantage. Maneuver around obstacles to block incoming projectiles and break enemy line-of-sight.
* **Complex Enemy Ecosystem:** Face a diverse roster including Melee brawlers, Ranged snipers, Healers, Tanks, and Bosses. Enemies utilize varied attacks from predictive arrows to throwable acid and snow spells.
* **Advanced Abilities:** Both player and AI can perform a suite of tactical abilities including **Dodge-rolls, Blocking, Stat Buffs, and Minion Summoning**.

---

## ⚙️ Core Architecture & Engineering

The project is built using a "Code-First" approach with an emphasis on modularity:

* **Decoupled Data-View:** Strict separation of **Data**, **Logic**, and **View** classes. This ensures that the gameplay logic is independent of the visual representation, allowing for easier testing and refactoring.
* **Animation Excellence:** Uses a seamless **Animation State Machine** with sophisticated **Animation Layers**. This allows for complex blended actions, such as the character playing a "Running" animation on the lower body while performing a "Shooting" or "Drinking Potion" animation on the upper body.
* **Performance-First Rendering:** Built on **URP** to deliver crisp 3D graphics and high-fidelity effects while maintaining a locked 60 FPS on lower-end mobile devices.

---

## 🤖 Advanced AI & Agentic Prototyping

This project leverages cutting-edge AI techniques to create a challenging and "human-like" combat experience:

* **Predictive Projectiles:** Enemy AI doesn't just fire at the player's current position; it calculates the player's velocity to "lead" shots and intercept movement.
* **NavMesh & Obstacle Avoidance:** Enemies utilize advanced **NavMesh Layers** combined with **Anti-Gravity** logic and obstacle avoidance to navigate complex 3D environments fluidly.
* **Agentic Prototyping:** Development speed is significantly increased through **Agentic Programming** workflows, using AI agents to rapidly iterate on enemy behavior trees and balance metadata.

---

## 📊 Data Management & Extensibility

* **Metadata-Driven Design:** All unit statistics, projectile behaviors, and ability variables are driven by metadata, making the game easily tweakable without code changes.
* **Addressables System:** Assets are managed via **Unity Addressables** to ensure optimal memory usage and fast loading times.
* **JSON State Handling:** Uses a robust **DataManager** with local **JSON GameState** for extendable and persistent save/load logic.

---

> [!NOTE]
> **Current Status:** Active Prototype.
> *Focusing on refining AI decision-making loops and expanding the metadata-driven ability system.*
