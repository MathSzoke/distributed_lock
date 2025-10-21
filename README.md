# 🧩 Distributed Locking Demo — Matheus Szoke

<p align="center">
  <img src="https://lockdemo.mathszoke.com/assets/lockDemo.png" alt="Distributed Locking Demo Banner" width="800"/>
</p>

<p align="center">
  <b>A practical .NET Aspire + React demo that visualizes how distributed locks prevent race conditions between concurrent API requests using Redis, Postgres, or SQL advisory locks.</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET%20Aspire-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/React-61DAFB?style=for-the-badge&logo=react&logoColor=black"/>
  <img src="https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white"/>
  <img src="https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white"/>
  <img src="https://img.shields.io/badge/Dapper-0C4B33?style=for-the-badge&logo=nuget&logoColor=white"/>
</p>

---

## 🧠 About the Project

This project demonstrates how **distributed locking mechanisms** can prevent race conditions and inconsistent states in systems that handle **simultaneous operations** on shared resources.

When two or more requests attempt to perform the same operation at the same time (for example, updating an order or processing a payment), this system ensures that:
- The first request acquires the lock and executes the critical section safely;
- The second request waits or fails gracefully with a `409 Conflict` if the lock cannot be acquired within a given timeout.

The frontend visually represents two users (“João” and “Maria”) racing through a simulated workflow, showing in real time how distributed locks coordinate execution and prevent data corruption.

> Although distributed locking solves many concurrency issues, **the most robust real-world approach** combines *idempotent request design* and *database constraints* to ensure consistency even under retries or failures.

---

## ⚙️ Core Stack

| Layer | Technologies |
|:--|:--|
| **Frontend** | React + Fluent UI |
| **Backend** | .NET Aspire 9 + Minimal APIs |
| **Lock Providers** | Redis, Postgres (Medallion.Threading), Dapper advisory locks |
| **Infrastructure** | Aspire Orchestration (AppHost) + Redis + Postgres |
| **Language** | C# |

---

## 🧩 Project Structure

```
src/
 ├─ Aspire/LockDemo.AppHost/         → Orchestrator with .NET Aspire
 │                                     - Starts Redis, Postgres and API
 │                                     - Injects URLs and environment variables for frontend
 │                                     - Health checks and service composition

 ├─ Backend/LockDemo.ApiService/     → Main API Service
 │                                     - Endpoints for distributed lock simulation
 │                                     - Implements Redis, Postgres and Dapper lock strategies
 │                                     - Includes WorkSimulator and RaceCoordinator logic
 │                                     - Models (RunRequest, RunResult)
 │
 └─ Frontend/lockdemo_web/           → React interface
                                       - Visual simulator with Fluent UI
                                       - Users “João” and “Maria” move through workflow stages
                                       - Interactive controls: lock toggle, provider selector, timers
```

---

## 🌟 Key Features

- Visual simulation of concurrent API execution  
- Support for **Redis**, **Postgres**, and **Dapper advisory locks**  
- Toggle for enabling/disabling the distributed lock in real time  
- Adjustable **work duration** and **lock timeout**  
- Step-based visualization with animated user personas  
- Clean, minimal UI built with Fluent UI and React Hooks  

---

## 🎨 Demo Overview

The interface simulates two concurrent users executing the same process:
- When **Lock Enabled**, only one user executes at a time — the other waits or fails on timeout.
- When **Lock Disabled**, both execute simultaneously, illustrating potential race conditions.
- Each provider (Redis, Postgres, Dapper) shows a different backend strategy for lock acquisition.

---

## 🚀 How to Run Locally

### Prerequisites
- .NET 9 SDK  
- Node.js (v19+)  
- Redis and PostgreSQL (handled automatically via Aspire)

### Run via Aspire

```bash
dotnet run --project src/Aspire/LockDemo.AppHost
```

Aspire will:
- Start Redis and Postgres containers
- Build and run the backend + frontend
- Expose URLs in your terminal (Swagger + React UI)

---

## 🌐 Live Demo (optional)

> Coming soon — this project will be hosted as a public micro-demo on **mathszoke.com**.

---

## 📫 Contact

📧 **Email:** [matheusszoke@gmail.com](mailto:matheusszoke@gmail.com)  
💼 **LinkedIn:** [linkedin.com/in/matheusszoke](https://linkedin.com/in/matheusszoke)  
🌐 **Portfolio:** [portfolio.mathszoke.com](https://portfolio.mathszoke.com)

---

<p align="center">
  <sub>Made with 💚 by <strong>Matheus Szoke</strong></sub>
</p>
