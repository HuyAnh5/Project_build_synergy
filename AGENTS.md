# Agent Rules

This is a Unity C# project.

Main goals:
- Refactor safely.
- Preserve current gameplay.
- Reduce duplicated logic.
- Improve FPS stability.
- Do not make one huge patch.

Rules:
- Work only in Assets/Scripts unless necessary.
- Ignore Library, Temp, Obj, Build, Builds, Logs, .git, .vs, UserSettings.
- Do not edit scenes, prefabs, sprites, textures, audio, animation, or binary assets unless absolutely required.
- Do not delete serialized fields unless proven safe.
- Do not delete public methods that may be used by Unity Inspector, UnityEvent, animation event, prefab, scene, or reflection.
- If unsure, write to report instead of deleting.
- Commit each safe batch separately.