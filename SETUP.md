# Furnace Rhythm Setup

This guide covers the tools and first-run steps for the current Furnace Rhythm Unity project. The project version and package manifest are the source of truth; keep this guide in sync when those settings change.

## Toolchain

| Tool or setting | Project version | Source of truth / notes |
|---|---:|---|
| Unity Editor | `6000.6.7f1` | [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt) |
| C# language version | `9.0` | Unity-generated `.csproj` files, for example `FurnaceRhythm.Domain.csproj`. Unity regenerates these IDE files. |
| .NET API compatibility | `.NET Standard 2.1` | Unity project setting; generated C# projects target `netstandard2.1`. |
| Universal Render Pipeline | `17.6.0` | [`Packages/manifest.json`](Packages/manifest.json) and `Packages/packages-lock.json` |
| Input System | `1.20.1` | Package manifest and lock file; the project is configured to use the new Input System. |
| Unity Test Framework | `1.8.0` | Package manifest and lock file; used for Edit Mode tests. |

The project does not pin a particular IDE. Visual Studio or Rider can be used with Unity's IDE integration packages. The C# 9.0 setting is Unity's current compiler/language setting; a separate C# SDK is not required to open and run the game in Unity.

## Install and open the project

1. Install Git and Git LFS, then clone the repository. Git LFS is used for binary assets such as PNG images. If you already cloned before installing Git LFS, run `git lfs install` and `git lfs pull` from the repository folder.
2. In Unity Hub, install Editor version `6000.6.7f1`.
3. In Unity Hub, choose **Add** or **Open** and select the repository root folder: the folder containing `Assets`, `Packages`, and `ProjectSettings`.
4. Let Unity finish importing assets and resolving packages before entering Play Mode. Keep `Packages/packages-lock.json` under version control so collaborators resolve the same package versions.
5. If using an IDE, select it in Unity's **Preferences → External Tools**. Let Unity generate or regenerate its solution and project files; do not treat the generated `.csproj` files as the place to change project-wide language settings.

## Open and play the game

Open `Assets/_Project/Scenes/FurnaceRhythmDay1.unity`, then press **Play**. The Game view must be focused for keyboard input. Press **Space** or left-click when the marker is inside the cyan timing band. The Day 1 scene builder is available from **Furnace Rhythm → Build Day 1 Scene**; it saves this scene under `Assets/_Project/Scenes/` and places it first in Build Settings.

## Project settings to preserve

- **Asset Serialization:** Force Text.
- **Version Control:** Visible Meta Files.
- **Active Input Handling:** Input System Package (New).
- Keep each Unity `.meta` file with its asset. Move or rename Unity assets through the Unity Project window so references retain their GUIDs.

## Optional: run Edit Mode tests

In the Unity Editor, open **Window → General → Test Runner**, select **EditMode**, and run the `FurnaceRhythm.Tests` assembly. The current tests are under `Assets/_Project/Tests/` and reference the Unity-independent Domain assembly.

## If the project opens in Safe Mode

Read the first compilation error in the Unity Console and resolve it before leaving Safe Mode. Unity has not finished importing the project in Safe Mode, and ignoring compilation errors can leave scripts unavailable. After fixing the errors, wait for compilation and asset import to complete, then reopen the Day 1 scene.
