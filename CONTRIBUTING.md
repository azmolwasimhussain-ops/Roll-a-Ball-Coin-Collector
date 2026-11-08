# Contributing

Thanks for helping improve Roll-a-Ball Coin Collector.

## Development setup

- Use Unity **6000.6.7f1** to match the project settings.
- Open the `RollABallCoinCollector` folder from Unity Hub and let Package Manager resolve the manifest.
- The playable scene is `Assets/Scenes/RollABallMain.unity`.

## Unity asset changes

- Keep each Unity `.meta` file with its matching asset. Unity stores references by GUID, so missing or regenerated metadata can break scene references.
- Commit `Packages/manifest.json` and `Packages/packages-lock.json` together when changing package dependencies.
- Do not commit `Library`, `Temp`, `Logs`, `Obj`, `Build`, `Builds`, or `UserSettings`; the root `.gitignore` excludes these local/generated folders.
- The scene generator protects an existing `RollABallMain.unity`. Move or rename that file before generating a replacement, then confirm the scene remains enabled in Build Settings.

## Changes and documentation

- Keep commits focused on a single project change and use clear conventional commit messages.
- Update the README when controls, setup, packages, or implemented gameplay change.
- Add audio or visual assets only when their licenses permit redistribution; optional audio slots work without bundled clips.
