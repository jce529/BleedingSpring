# External Integrations

**Analysis Date:** 2026-03-27

## APIs & External Services

**None detected.**

No third-party API clients, SDKs, or HTTP client code are present in any C# script under `Assets/`. All game logic is self-contained.

## Data Storage

**Databases:**
- None. No database client or ORM detected.

**File Storage:**
- Unity serialized assets only (`.asset`, `.prefab`, `.unity` files committed to repository).
- No runtime file I/O code detected in game scripts.

**Caching:**
- None. No caching layer present.

**Save System:**
- Not yet implemented. No `PlayerPrefs`, `JsonUtility` save/load, or file persistence code detected in current scripts.

## Authentication & Identity

**Auth Provider:**
- None. No authentication system present.

## Unity Cloud Services

All Unity Cloud Services are **disabled** in `ProjectSettings/UnityConnectSettings.asset`:

- **Unity Analytics:** Disabled (`m_Enabled: 0`)
- **Unity Ads:** Disabled (`m_Enabled: 0`); Game IDs not configured
- **Crash Reporting:** Disabled (`m_EnableCloudDiagnosticsReporting: 0`)
- **Unity Purchasing (IAP):** Disabled (`m_Enabled: 0`)
- **Performance Reporting:** Disabled (`m_Enabled: 0`)
- **Unity Insights / Engine Diagnostics:** Engine diagnostics enabled in editor (`m_EngineDiagnosticsEnabled: 1`) but cloud reporting is off

The `com.unity.collab-proxy` package (Unity Version Control / Plastic SCM) is installed at version 2.11.4 but the project uses Git for source control (`.gitignore` present at root and `BleedingSpring/.gitignore`).

## Monitoring & Observability

**Error Tracking:**
- None configured for runtime.
- Unity Editor captures exceptions locally (`m_CaptureEditorExceptions: 1` in crash reporting settings) but does not send them to a remote service.

**Logs:**
- Unity's built-in player log (`usePlayerLog: 1` in `ProjectSettings/ProjectSettings.asset`).
- Log files written locally to `Logs/` directory (not committed — excluded by `.gitignore`).
- No structured logging library in use.

## CI/CD & Deployment

**Hosting:**
- Not configured. No deployment pipeline detected.

**CI Pipeline:**
- None. No CI configuration files (`.github/workflows/`, `Jenkinsfile`, `.gitlab-ci.yml`, etc.) found.

## Multiplayer

**Multiplayer Center** (`com.unity.multiplayer.center` 1.0.1) is installed as a setup helper package, but:
- Multiplayer roles are disabled (`m_EnableMultiplayerRoles: 0` in `ProjectSettings/MultiplayerManager.asset`).
- No Netcode for GameObjects, Mirror, or other networking package is present in `Packages/manifest.json`.
- No networking code detected in game scripts.

## Asset Store / Imported Assets

**Hero Knight - Pixel Art (Unity Asset Store):**
- Location: `Assets/ImportedAssets/Hero Knight - Pixel Art/`
- Includes: Sprites, animations, demo scripts (`HeroKnight.cs`, `Sensor_HeroKnight.cs`, `DestroyEvent_HeroKnight.cs`, `ColorSwap_HeroKnight.cs`)
- These are locally vendored and require no network access at runtime.

## Webhooks & Callbacks

**Incoming:**
- None.

**Outgoing:**
- None.

## Environment Configuration

**Required env vars:**
- None. The project has no environment variable dependencies.

**Secrets location:**
- None detected. No secret management or credential files present.

---

*Integration audit: 2026-03-27*
