# RULES.md: Project Conventions

**Project type:** Unity 2D, C#, small-team academic project
**Core principle:** Separation of Concerns (SoC). Each part of the game has one job, one folder, and one clearly defined way of talking to the other parts.

> Items marked **[CONFIRM]** are team decisions that were not yet settled. Fill them in once and treat them as fixed.

---

## 1. Concerns and where they live

| Concern | Location | Unity-dependent? | Allowed to know about |
|---|---|---|---|
| Game rules, money system | `Domain/` (plain C#) | **No** | Nothing outside `Domain/` |
| Behavior (input, movement, animation triggers) | `Behaviors/` | Yes | `Domain/`, `Events/` |
| Display (UI, HUD, VFX) | `Presentation/` | Yes | `Events/`, `Config/` |
| Sound effects | `Audio/` | Yes | `Events/` |
| Scenes | `Scenes/` | Yes | Wiring only, no logic |
| Art (PNG sprites) | `Art/` | N/A | Contains no scripts |
| Data and configuration | `Config/` (ScriptableObjects) | Light | Art references only |
| Cross-system messaging | `Events/` (ScriptableObject channels) | Light | Nothing |

### Dependency rule

Dependencies point **inward** toward `Domain/`. Never the reverse.

```
Presentation  ->  Events  <-  Behaviors  ->  Domain
Audio         ->  Events
Config        ->  (Art)
```

- `Domain/` references **nothing** in Unity or in other folders.
- `Presentation/` and `Audio/` **never** reference `Domain/` or `Behaviors/` directly. They listen to events.
- No two sibling systems (for example `Audio/` and `Presentation/`) reference each other.

---

## 2. Folder structure

```
Assets/_Project/
├── Domain/            # Pure rules (Domain.asmdef, "No Engine References" enabled)
│   ├── Economy/
│   └── Combat/
├── Behaviors/         # Controllers: Unity lifecycle and input
├── Presentation/      # Views, UI, VFX
├── Audio/             # SFX players, clips, mixer
├── Events/            # ScriptableObject event channels
├── Config/            # ScriptableObject data (items, enemies, balance)
├── Scenes/
├── Art/
│   ├── Characters/
│   ├── Environment/
│   ├── UI/
│   ├── VFX/
│   └── Atlases/
└── Tests/             # Edit Mode tests for Domain

ArtSource/             # OUTSIDE Assets/: raw AI outputs, prompts, notes
```

### Rules

- **R-1.1** Every code folder has its own **assembly definition** (`.asmdef`) with explicit references. This makes the compiler enforce Section 1.
- **R-1.2** `Domain.asmdef` must have **No Engine References** enabled. No `using UnityEngine;` in `Domain/`, ever.
- **R-1.3** Scripts never live in `Art/`. Images never live in code folders.
- **R-1.4** Do not use the `Resources/` folder as a general storage area.
- **R-1.5** Third-party packages go in `Assets/ThirdParty/` and are never edited in place.

---

## 3. Architecture and code rules

### 3.1 Layers (Controller, Service, Repository mapping)

| Classic layer | Unity equivalent |
|---|---|
| Controller | Thin `MonoBehaviour` in `Behaviors/`: lifecycle and input only |
| Service | Plain C# class in `Domain/`: all game rules |
| Repository | Interface in `Domain/`, implementation in a Unity-aware folder (save/load) |

- **R-3.1** `MonoBehaviour`s contain **no business rules**. They forward input to a domain object and publish results.
- **R-3.2** Money, scoring, damage, and progression rules live in plain C# classes that can be unit tested without Unity.
- **R-3.3** Depend on **interfaces** for persistence and external systems (Dependency Inversion).
- **R-3.4** Wiring happens in a small number of "composition root" scripts, not scattered across classes.
- **R-3.5** Do not create a global `GameManager` singleton that does everything. If a singleton is truly needed, document why in its XML comment.
- **R-3.6** Prefer **composition over inheritance**. Use small components (`Health`, `Movement`, `InputHandler`), not deep class hierarchies.

### 3.2 SOLID quick reference

| Principle | Project rule of thumb |
|---|---|
| Single Responsibility | One class, one reason to change. If a class needs "and" to describe it, split it. |
| Open/Closed | Add behavior with new components or definitions, not by editing working classes. |
| Liskov Substitution | Any implementation of an interface must behave safely wherever the interface is used. |
| Interface Segregation | Small, focused interfaces (`ISaveRepository`, not `IEverything`). |
| Dependency Inversion | Rules depend on abstractions, never on `UnityEngine` or file paths. |

### 3.3 Communication between systems

- **R-3.7** Systems communicate through **ScriptableObject event channels** or C# events, not through direct references to each other.
- **R-3.8** Every subscription made in `OnEnable` **must** be removed in `OnDisable`. This is a merge-blocking review item.
- **R-3.9** Views are read-only. UI never edits state. It calls a controller method.

### 3.4 Documentation

- **R-3.10** Every public type and member has an XML documentation comment (`/// <summary>`), including `<param>`, `<returns>`, and `<exception>` where relevant.
- **R-3.11** Comments explain **why**, not what the code already says.

Example of the expected standard:

```csharp
/// <summary>
/// Holds and validates the player's money. Contains no Unity code,
/// so it can be unit tested and reused.
/// </summary>
public sealed class Wallet
{
    /// <summary>Attempts to spend money.</summary>
    /// <param name="amount">Must be positive.</param>
    /// <returns>True if funds were sufficient and the amount was deducted.</returns>
    public bool TrySpend(int amount) { /* ... */ }
}
```

---

## 4. Naming conventions

| Item | Convention | Example |
|---|---|---|
| Classes, methods, properties | `PascalCase` | `WalletController` |
| Private fields | `_camelCase` | `_wallet` |
| Serialized private fields | `camelCase` with `[SerializeField]` | `balanceChanged` |
| Interfaces | `I` prefix | `ISaveRepository` |
| Scripts | One public type per file, file name equals type name | `Wallet.cs` |
| Image files | `lowercase_category_name_state_frame.png` | `enemy_slime_idle_01.png` |
| Folders | `PascalCase` | `Characters/` |
| ScriptableObject assets | Descriptive, no spaces | `Item_HealthPotion.asset` |

---

## 5. Art asset rules (2D, AI-generated PNGs)

### 5.1 Import settings

| Setting | Rule |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | One value project-wide: **[CONFIRM]** (proposed default: 100) |
| Filter Mode | Bilinear for painted art, Point for pixel art: **[CONFIRM style]** |
| Compression | Normal Quality (None for pixel art) |

- **R-5.1** Use a **Preset Manager folder filter** or a folder-scoped importer that applies one saved Preset so every PNG in a folder gets identical settings automatically.
- **R-5.2** Resize AI images **before** importing. Do not ship 2048×2048 images for sprites shown at 64×64.
- **R-5.3** Group related sprites into **Sprite Atlases** in `Art/Atlases/`.
- **R-5.4** Verify real transparency. AI tools often bake in fake checkerboard or white backgrounds.
- **R-5.5** Use one consistent prompt style and resolution per asset category to keep the look coherent.

### 5.2 Keeping code away from images

- **R-5.6** Game logic never references a sprite. It references an **ID** or a **ScriptableObject definition**.
- **R-5.7** Only the `Presentation/` layer reads a `Sprite` from a definition and displays it.
- **R-5.8** Do not load art with string paths (`Resources.Load("...")`).

### 5.3 Source files and provenance

- **R-5.9** Raw AI outputs, prompts, and unedited originals live in `ArtSource/`, **outside** `Assets/`.
- **R-5.10** Keep an `ASSETS.md` listing, for each asset or batch: generator tool, date, prompt reference, and license terms.
- **R-5.11** Strip embedded metadata (prompts, tool info) from PNGs before committing, by re-exporting through an image editor.
- **R-5.12** Check your institution's AI-usage and disclosure policy, and your generator's license terms, before submission. This document does not decide those for you.

---

## 6. Audio rules

- **R-6.1** Sound effects are played by components in `Audio/` that **react to events**. Gameplay code never calls `AudioSource.Play()` directly.
- **R-6.2** Audio clips are assigned through `[SerializeField]` fields or ScriptableObject definitions, not string paths.
- **R-6.3** Use an `AudioMixer` for volume groups (Music, SFX) so volume settings need no code changes.

---

## 7. Scene rules

- **R-7.1** Scenes **compose** objects and assign references. They contain no logic.
- **R-7.2** Use empty GameObjects as organizers in the hierarchy: `--- Managers ---`, `Environment`, `Characters`, `UI`.
- **R-7.3** If runtime scene loading is introduced, route it through a single scene-loader class. The current one-scene prototype does not load scenes at runtime; choose and document the loader design before adding runtime scene transitions.
- **R-7.4** Two people should not edit the same scene at the same time. Split content into prefabs and additive scenes to reduce conflicts.

---

## 8. Security rules

- **R-8.1** Validate **every** public input: ranges, nulls, and negative values. Throw `ArgumentOutOfRangeException` or return `false` as appropriate.
- **R-8.2** Use `checked` arithmetic for currency and counters so overflow throws instead of silently wrapping.
- **R-8.3** **Never use `BinaryFormatter`.** It is insecure and deprecated. Use JSON (`JsonUtility`) or another safe serializer.
- **R-8.4** Treat every save file as **untrusted**. Validate on load and fall back to safe defaults on corruption. Never crash on bad data.
- **R-8.5** Write saves to a temp file first, then replace the original, so a crash mid-write cannot corrupt the save.
- **R-8.6** Local saves can always be edited by players. If tamper detection matters, add an HMAC or signature. For any multiplayer feature, validate everything server-side.
- **R-8.7** Never commit secrets (API keys, tokens). Do not log sensitive values.
- **R-8.8** Log exception **types**, not raw user data, in warnings.

---

## 9. Testing

- **R-9.1** All `Domain/` logic has **Edit Mode** unit tests (NUnit). This is the main payoff of keeping rules free of Unity code.
- **R-9.2** Test the boundaries: zero, negative, maximum values, and insufficient funds.
- **R-9.3** A bug fix in `Domain/` starts with a failing test that reproduces it.

---

## 10. Version control and teamwork

- **R-10.1** Commit `.meta` files with their assets. Never rename or move assets outside the Unity Project window.
- **R-10.2** Use a Unity-specific `.gitignore` (exclude `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`).
- **R-10.3** Use **Git LFS** for large binary assets (PNG, audio) or keep originals out of the repository.
- **R-10.4** Set `Asset Serialization` to **Force Text** and `Version Control` to **Visible Meta Files** in Project Settings.
- **R-10.5** Work on branches and merge through pull requests, with at least one reviewer.
- **R-10.6** One concern per commit and per pull request where possible.

---

## 11. Pull request checklist

Reviewers should reject a PR if any of these fail:

- [ ] No `UnityEngine` usage inside `Domain/`
- [ ] No business rules inside a `MonoBehaviour`
- [ ] No direct references between sibling systems (Audio, Presentation, Behaviors)
- [ ] Every event subscription has a matching unsubscription
- [ ] Public members have XML documentation
- [ ] Inputs are validated; no `BinaryFormatter`; save data treated as untrusted
- [ ] New sprites use the standard import preset and naming convention
- [ ] AI-generated assets are recorded in `ASSETS.md` and stripped of metadata
- [ ] New domain logic has unit tests
- [ ] `.meta` files are included

---

## 12. Open decisions

| Decision | Owner | Status |
|---|---|---|
| Pixels Per Unit value | Team | Confirmed: 100 PPU for the painted sprite preset |
| Art style filter mode (Bilinear or Point) | Team | Confirmed: Bilinear for the painted concept art |
| Scene loader design | Team | Deferred until runtime scene transitions are needed; no scene is loaded at runtime in the one-scene prototype |
| Unity version and render pipeline (2D URP or built-in) | Team | Confirmed: Unity 6000.6.7f1 with URP 17.6.0 |
| Dependency injection (manual wiring vs. a framework) | Team | Confirmed: manual wiring in the scene composition root |

---

*Review this document at the start of each milestone and update it when a decision changes. A rule the team does not follow should be fixed or removed, not ignored.*
