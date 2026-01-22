# Architecture Overview

This project uses a **CQRS-inspired, event-driven architecture** built around an **in-process Message Bus**. The goal is to clearly separate **intent**, **side effects**, and **state**, while remaining simple enough for a game server mod environment.

This document defines **responsibilities, boundaries, and naming conventions** so both humans and AI tools (e.g. GitHub Copilot) can reason about the system correctly.

---

## Core Principles

1. **Explicit Intent** – All state changes start as Commands
2. **Events are Facts** – Events describe what already happened
3. **Side Effects are Isolated** – Only Command Handlers talk to the Vintage Story API
4. **Database is a Projection** – Database state is derived from events
5. **Startup is Discovery** – Server startup emits discovery events, not commands

---

## Message Bus

- Singleton, in-process
- Type-based publish/subscribe
- Used for both Commands and Events
- No persistence, no threading assumptions

```text
Application / Execution / Projections
           ↓
       MessageBus
```

---

## Commands

**Commands represent intent to change state.**

Examples:

- `BanPlayerCommand`
- `AddPlayerGroupCommand`
- `InstallModCommand`

Rules:

- Handled by **exactly one** Command Handler
- Must not read or write the database
- Must not be emitted by projections
- Include a `CommandId` for correlation

---

## Command Handlers (Execution Layer)

**Command Handlers perform side effects.**

Responsibilities:

- Interact with the Vintage Story server API
- Translate commands into server actions
- Emit domain events on success or failure

Rules:

- No database access
- No business rule decisions
- No issuing other commands directly

Examples:

- `BanPlayerCommandHandler`
- `InstallModCommandHandler`

---

## Events

**Events represent facts that already occurred.**

Examples:

- `PlayerBannedEvent`
- `PlayerGroupAddedEvent`
- `ModInstalledEvent`

Rules:

- Immutable
- Past-tense naming
- Zero or more subscribers

---

## Projections

**Projections build and maintain database state.**

Responsibilities:

- Subscribe to events
- Update database read models
- Be idempotent and replay-safe

Rules:

- May write to the database
- Must not call the Vintage Story API
- Must not issue commands directly

Examples:

- `PlayerProjection`
- `PermissionProjection`

---

## Application Services

**Application Services coordinate business logic.**

Responsibilities:

- Read from the database
- Apply rules and validations
- Decide which command to issue

Rules:

- Must not call Vintage Story APIs
- Must not perform side effects

Examples:

- `PlayerGroupService`
- `PermissionService`

---

## Startup & Discovery

On server startup, the Vintage Story server is treated as the **source of truth**.

A dedicated startup service:

- Queries server state (players, bans, whitelist, mods, server mode)
- Emits **discovery events**, not commands

Examples:

- `PlayerDiscoveredEvent`
- `PlayerBanDiscoveredEvent`
- `InstalledModDiscoveredEvent`

These events flow through the same projections as runtime events.

---

## Reconciliation & Decisions

Projections may detect inconsistencies but **must not perform side effects**.

Two supported approaches:

1. Emit **decision events** (e.g. `PlayerBanRequiredEvent`)
2. Use a dedicated **ReconciliationService** after startup

A handler responds by issuing the appropriate command.

This avoids infinite loops and keeps responsibilities clear.

---

## Layer Responsibilities Summary

| Layer                | DB Read | DB Write | VS API | Issues Commands |
| -------------------- | ------- | -------- | ------ | --------------- |
| Application Services | ✅      | ❌       | ❌     | ✅              |
| Command Handlers     | ❌      | ❌       | ✅     | ❌              |
| Projections          | ❌      | ✅       | ❌     | ❌              |
| Startup Sync         | ❌      | ❌       | ✅     | ❌              |

---

## Naming Conventions

- `*Command` – Intent
- `*CommandHandler` – Side effects
- `*Event` – Fact
- `*Projection` – Database updates
- `*Service` – Application logic

---

## Architectural Goal

This design intentionally balances:

- CQRS clarity
- Event-driven extensibility
- Mod-friendly simplicity
- Future multi-server support

Without introducing full event sourcing or external infrastructure.
