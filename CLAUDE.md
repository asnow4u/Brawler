# Lone Wolf (Brawler) — Agent Requirements

## Unity Editor access: use the Unity CLI (required)

- All work in Unity MUST go through the Unity CLI (`unity`), not the Unity MCP server (`mcp__unityMCP__*` tools).
- Before editing any scene, prefab, or asset, run `unity status --project-path "<this project>"` and confirm the Editor is `ready`.
- Drive the live Editor with `unity command ... --project-path "<this project>" --caller plugin --skill unity-cli` (e.g. `unity command script eval`, Editor-exposed scene/asset commands, `editor.playmode start`). Discover commands with `unity command --query <term>`.
- Check compilation with `unity recompile` / `unity status` and read the Editor console through the CLI.
- Do not hand-edit `.unity`, `.prefab`, or `.asset` YAML while a live Editor is reachable. If the Editor is unreachable, say so before falling back to file edits.
