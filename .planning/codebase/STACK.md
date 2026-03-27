# Technology Stack

**Analysis Date:** 2026-03-27

## Languages

**Primary:**
- C# 9.0 - All game logic scripts under `Assets/`

**Secondary:**
- YAML - Unity asset serialization (`.asset`, `.prefab`, `.unity` files)
- JSON - Package manifests (`Packages/manifest.json`, `Packages/packages-lock.json`)

## Runtime

**Environment:**
- Unity Engine 6000.3.11f1 (Unity 6 LTS)
- Mono scripting backend
- .NET Standard 2.1 target framework (`netstandard2.1`)

**Package Manager:**
- Unity Package Manager (UPM)
- Lockfile: present at `Packages/packages-lock.json`

## Frameworks

**Core:**
- Unity 6 (`com.unity.modules.*`) - Game engine runtime, physics, audio, animation
- Universal Render Pipeline (URP) 17.3.0 (`com.unity.render-pipelines.universal`) - 2D rendering pipeline configured at `Assets/UniversalRenderPipelineGlobalSettings.asset`

**Input:**
- Unity Input System 1.19.0 (`com.unity.inputsystem`) - New input system; actions defined in `Assets/InputSystem_Actions.inputactions`, generated C# wrapper at `Assets/InputSystem_Actions.cs`

**2D Tooling:**
- 2D Animation 13.0.4 (`com.unity.2d.animation`) - Skeletal/sprite animation
- 2D Aseprite Importer 3.0.1 (`com.unity.2d.aseprite`) - Direct `.ase` file import
- 2D PSD Importer 12.0.1 (`com.unity.2d.psdimporter`) - Photoshop file import
- 2D Sprite 1.0.0 (`com.unity.2d.sprite`) - Core sprite handling
- 2D Sprite Shape 13.0.0 (`com.unity.2d.spriteshape`) - Procedural 2D shapes
- 2D Tilemap 1.0.0 + Extras 6.0.1 (`com.unity.2d.tilemap`, `com.unity.2d.tilemap.extras`) - Tile-based level building

**UI:**
- Unity UI (UGUI) 2.0.0 (`com.unity.ugui`) - Runtime UI components

**Timeline / Visual Scripting:**
- Timeline 1.8.11 (`com.unity.timeline`) - Cutscene/sequencing
- Visual Scripting 1.9.10 (`com.unity.visualscripting`) - Node-based scripting (present but usage in custom scripts not detected)

**Testing:**
- Unity Test Framework 1.6.0 (`com.unity.test-framework`) - Unit/integration test runner

**Multiplayer (infrastructure only):**
- Multiplayer Center 1.0.1 (`com.unity.multiplayer.center`) - Multiplayer setup helper; multiplayer roles disabled in `ProjectSettings/MultiplayerManager.asset`

## Key Dependencies (transitive, critical)

**Performance:**
- Burst Compiler (via `com.unity.2d.common` dependency) - SIMD/native code compilation for hot paths
- Unity Collections 2.4.3 (via `com.unity.2d.animation`) - High-performance native containers
- Unity Mathematics 1.2.6 (via `com.unity.2d.aseprite`) - SIMD-friendly math library

**IDE Integration:**
- Rider Integration 3.0.39 (`com.unity.ide.rider`) - JetBrains Rider support
- Visual Studio Integration 2.0.26 (`com.unity.ide.visualstudio`) - VS support; Unity Analyzers DLL loaded from `C:\Users\MSI\.vscode\extensions\visualstudiotoolsforunity.vstuc-1.2.1\`

**Source Control:**
- Collaborate Proxy 2.11.4 (`com.unity.collab-proxy`) - Unity Version Control (Plastic SCM) stub; project uses Git instead

## Configuration

**Environment:**
- No `.env` files detected
- No runtime environment variable configuration
- Game is self-contained; all configuration via Unity Inspector (serialized `.asset` and `.prefab` files)

**Build:**
- Solution file: `My project.sln` / `My project.slnx`
- C# project file (generated): `Assembly-CSharp.csproj`
- Build target: `StandaloneWindows64` (Windows PC, 64-bit)
- Output resolution: 1920x1080 default
- Color space: Linear (`m_ActiveColorSpace: 1`)
- Application ID: `com.DefaultCompany.2D-URP`
- Product name: `BleedingSpring`
- Company: `DefaultCompany` (placeholder, not updated)

**Editor IDEs:**
- VS Code with Unity extension (`visualstudiotoolsforunity.vstuc-1.2.1`) configured in `.vscode/`
- Visual Studio with `Microsoft.VisualStudio.Workload.ManagedGame` workload (`.vsconfig`)
- JetBrains Rider also supported via UPM package

## Platform Requirements

**Development:**
- Windows OS (project configured for StandaloneWindows64)
- Unity Hub with Unity 6000.3.11f1 installed
- .NET Standard 2.1 compatible IDE (Visual Studio 2022, Rider, or VS Code with Unity extension)

**Production:**
- Target: Windows PC standalone (64-bit)
- Android build settings present (`AndroidMinSdkVersion: 25`) but not the primary target
- No web, console, or mobile builds configured as primary

---

*Stack analysis: 2026-03-27*
