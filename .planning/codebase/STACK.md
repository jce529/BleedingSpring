# Technology Stack

**Analysis Date:** 2025-02-18

## Languages

**Primary:**
- C# 12.0 - Core game logic and Unity scripts (`Assets/**/*.cs`)

**Secondary:**
- HLSL - Shader programming (via Shader Graph, referenced in `Assets/Settings/Renderer2D.asset`)

## Runtime

**Environment:**
- Unity 6 (Version: 6000.3.11f1)

**Package Manager:**
- Unity Package Manager (UPM)
- Lockfile: `Packages/packages-lock.json` present

## Frameworks

**Core:**
- Universal Render Pipeline (URP) 17.3.0 - Rendering and graphics (`Assets/Settings/UniversalRP.asset`)
- Unity UI (uGUI) 2.0.0 - User Interface system
- Unity Input System 1.19.0 - Modern input handling (`Assets/InputSystem_Actions.inputactions`)

**Testing:**
- Unity Test Framework 1.6.0 - Testing runner (`Assets/Tests/`)
- NUnit - Assertion library used in tests

**Build/Dev:**
- Unity Editor 6000.3.11f1

## Key Dependencies

**Critical:**
- `com.unity.render-pipelines.universal` - Provides the 2D renderer and lighting
- `com.unity.inputsystem` - Handles player controls and input mapping
- `com.unity.ugui` / TextMeshPro - UI rendering and layout

**Infrastructure:**
- `com.unity.2d.animation` - 2D skeletal animation support
- `com.unity.2d.tilemap` - 2D world building

## Configuration

**Environment:**
- Unity Project Settings (`ProjectSettings/*.asset`)
- URP Asset (`Assets/Settings/UniversalRP.asset`)

**Build:**
- Unity Build Settings (configured in Editor)

## Platform Requirements

**Development:**
- Windows/macOS/Linux with Unity 6 installed

**Production:**
- Target platforms: PC, Mac & Linux Standalone (based on default Unity project structure)

---

*Stack analysis: 2025-02-18*
