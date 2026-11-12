# Furnace Rhythm Architecture

This document maps the project as it exists today and explains how its folders follow the conventions in [`RULES.md`](RULES.md). `RULES.md` is the source of truth for conventions; this file describes where the current implementation lives. For editor versions and first-run instructions, see [`SETUP.md`](SETUP.md). Update these documents when project conventions, architecture, or setup change.

## At a glance

```text
Player input
    ↓
FurnaceRhythmController ──updates──> FurnaceShift (Domain)
    │                                    │
    └── publishes snapshot ──> FurnaceRhythmEventBus ──> FurnaceRhythmPresenter
                                                               ↑
FurnaceRhythmGame (composition root) ── supplies tuning ───────┘
```

`FurnaceRhythmGame` wires the controller and presenter. The controller forwards input and elapsed time to the Unity-independent `FurnaceShift` rules, then publishes a display snapshot. The presenter listens for snapshots and builds and updates the HUD. `FurnaceRhythmTuning` supplies timing and art configuration.

## Current gameplay folders

```text
Assets/
├── _Project/
│   ├── Art/
│   ├── Behaviors/
│   ├── Config/
│   ├── Domain/
│   ├── Events/
│   ├── Presentation/
│   ├── Scenes/
│   └── Tests/
├── FurnaceRhythm/
│   ├── Editor/
│   ├── Runtime/
│   └── Scenes/
└── Scenes/
```

Unity `.meta` files sit beside their corresponding assets and should be committed with them.

### `Assets/_Project/`

This is the main home for the game's separated systems, matching the folder plan in `RULES.md`.

| Folder | What belongs here | Current contents |
|---|---|---|
| `Domain/` | Unity-independent game rules and state | `FurnaceShift`, settings, snapshots, state, and timing grades. Its assembly has **No Engine References** enabled. |
| `Behaviors/` | Thin Unity controllers, lifecycle, and input | `FurnaceRhythmController`; reads Space/left-click and forwards time and actions to the domain. |
| `Events/` | Data and messages between systems | Snapshot and grade/run-state data plus `FurnaceRhythmEventBus`. The current bus uses a C# event, which is allowed by R-3.7. |
| `Presentation/` | HUD, visuals, and display-only behavior | `FurnaceRhythmPresenter` builds and updates the HUD from event snapshots; `FurnaceRhythmPalette` holds presentation colors. |
| `Config/` | Tunable data assets | `FurnaceRhythmTuning` ScriptableObject and its configured asset, including timing values and the billet sprite reference. |
| `Art/` | Imported game art and its asset record/import settings | `hot_billet_anvil_concept.png`, `ASSETS.md`, and `hot_billet_sprite.preset`. |
| `Scenes/` | Scene composition and serialized references | `FurnaceRhythmDay1.unity`, the scene created by the Day 1 builder and first in Build Settings. |
| `Tests/` | Edit Mode tests for pure domain behavior | `FurnaceShiftTests.cs`; the assembly references the Domain assembly. |

The `Economy/`, `Combat/`, nested art-category, and `Art/Atlases/` subfolders shown in `RULES.md` are organizational options for future growth; they are not present because the current prototype is small and has no such systems yet. There is currently no `Assets/_Project/Audio/` folder or audio system.

### `Assets/FurnaceRhythm/`

This is a second game-specific tree that currently contains composition and Unity Editor tooling:

| Folder | Current role |
|---|---|
| `Runtime/` | `FurnaceRhythmGame`, the composition-root `MonoBehaviour` that creates/configures the controller and presenter, plus a runtime palette definition. |
| `Editor/` | `FurnaceRhythmDayOneSceneBuilder` creates the playable scene, tuning asset, and sprite preset; `FurnaceRhythmSpriteImportPostprocessor` applies art import settings. Editor-only assembly. |
| `Scenes/` | A second copy of `FurnaceRhythmDay1.unity`. |

The builder writes the canonical Day 1 scene to `Assets/_Project/Scenes/FurnaceRhythmDay1.unity` and puts it first in Build Settings. The separate `Assets/FurnaceRhythm/Scenes/FurnaceRhythmDay1.unity` is also currently enabled in Build Settings, as is `Assets/Scenes/SampleScene.unity`. Keep this duplication in mind when changing scenes; new gameplay scenes belong in `Assets/_Project/Scenes/` under R-7.1.

The builder's old art location, `Assets/FurnaceRhythm/Art/`, is not present. The sample art and asset register are now in `Assets/_Project/Art/`.

### Other project-owned folders

- `ArtSource/` is outside `Assets/` and stores the original artwork, provenance, and source notes, as required by R-5.9 and R-5.10.
- `Docs/` contains supporting project documents, including the implementation plan.
- `Assets/_Recovery/` contains Unity scene recovery data. It is recovery material, not part of the playable scene architecture.
- `Assets/Scenes/` contains Unity's `SampleScene.unity`; it remains enabled alongside the Day 1 scenes in Build Settings.
- `Assets/Settings/`, `Assets/TextMesh Pro/`, and `Assets/Welcome/` hold Unity/render-pipeline, TextMesh Pro, and template/sample support assets respectively. Keep game-specific code and art in the mapped game folders above.

## Assembly dependencies

References in the `.asmdef` files enforce most of the boundaries described by `RULES.md`:

```text
FurnaceRhythm.Runtime (composition root)
 ├── FurnaceRhythm.Behaviors ──> FurnaceRhythm.Domain
 │                             └─> FurnaceRhythm.Events
 ├── FurnaceRhythm.Config
 ├── FurnaceRhythm.Domain
 └── FurnaceRhythm.Presentation ──> FurnaceRhythm.Events
                                  └─> FurnaceRhythm.Config

FurnaceRhythm.Editor ──> Runtime, Config, Presentation, Unity Editor APIs
FurnaceRhythm.Tests  ──> Domain (Edit Mode)
```

- `Domain` has no project-assembly references and no Unity engine references.
- `Events` is also Unity-independent in the current implementation; it carries plain data and a C# event boundary.
- `Behaviors` depends on the domain rules and event boundary. The domain does not know about the controller.
- `Presentation` depends on events and configuration, and does not reference `Domain` or `Behaviors` directly.
- `Config` uses Unity asset types to expose tuning and the art reference; game rules consume copied values as `FurnaceShiftSettings`.
- `Runtime` is the composition root. It is the intentional place that knows both controller and presenter so it can wire them together.
- `Tests` references only `Domain`, keeping domain tests independent of Unity runtime assemblies.

## Where new work goes

Use `RULES.md` for the full requirements. The short folder guide is:

| If you are adding... | Put it in... |
|---|---|
| Scoring, timing, quality, or other game rules | `Assets/_Project/Domain/` as plain C# |
| Input handling or a thin Unity lifecycle controller | `Assets/_Project/Behaviors/` |
| Cross-system snapshot or message data | `Assets/_Project/Events/` |
| HUD, effects, and other displayed feedback | `Assets/_Project/Presentation/` |
| Balance/tuning values or references to art assets | `Assets/_Project/Config/` |
| Imported sprites, atlas assets, and asset register | `Assets/_Project/Art/` |
| Scene composition and serialized object wiring | `Assets/_Project/Scenes/` |
| Edit Mode tests for domain rules | `Assets/_Project/Tests/` |
| Import/build menu tooling | `Assets/FurnaceRhythm/Editor/` (Editor-only assembly) |

Keep scene files focused on composition, keep each code folder's assembly references explicit, and include Unity `.meta` files in changes. Raw originals and provenance stay in `ArtSource/`; imported, game-ready art stays under `Assets/_Project/Art/`.

## Current structural follow-ups

These describe the existing layout; they do not change the project by themselves:

1. There are two Day 1 scene assets and a template `SampleScene`, all enabled in Build Settings. The scene builder treats the `_Project/Scenes` copy as canonical.
2. The composition root and Editor tooling live under `Assets/FurnaceRhythm/`, while the domain, controller, presentation, config, art, and tests live under `Assets/_Project/`.
3. Palette code exists in both `Assets/FurnaceRhythm/Runtime/` and `Assets/_Project/Presentation/`.
4. The rules describe future Audio and art-category folders that do not exist yet; add them only when those concerns are introduced.

When moving or consolidating Unity assets, do so through the Unity Project window so references and `.meta` GUIDs stay intact, as required by R-10.1.
