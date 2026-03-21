# BC4LinearImport

**Language:** English | [日本語](README.ja.md)

BC4LinearImport helps Unity import eligible BC4-targeted grayscale `PNG` / `JPG` / `JPEG` textures as linear data instead of sRGB.

This is meant to save time when you are working with grayscale textures in a VRChat project and do not want to keep fixing import behavior by hand.

## What it helps with

- Automatically handles eligible textures when they are imported.
- Lets you exclude specific assets or folders that should be left alone.
- Gives you a quick way to repair already-imported textures after changing settings.
- Includes a diagnostics menu when you need to check why targeting did or did not happen.

## How it works at a glance

1. Open `Project Settings > BC4 Linear Import`.
2. Leave `Enable BC4 linear import` enabled if you want the feature active for the project.
3. If some textures or folders should be skipped, add them under `Excluded assets and folders`.
4. If the textures were already in the project before you changed settings, run a reimport so eligible textures are processed again.
5. If you are not sure why targeting did or did not happen, use the diagnostics menu to observe targeting behavior.

For a visual map of the manual reimport path, automatic import-time path, and safe no-op outcomes, see the [BC4LinearImport color-space flow diagram](docs/diagrams/bc4linearimport-color-space-flow.svg).

## Where to find it

- `Project Settings > BC4 Linear Import`
	- Main settings page for `Enable BC4 linear import`, `Excluded assets and folders`, and `Reimport Eligible PNG/JPG/JPEG Textures`.
- `Tools > BC4 Linear Import > Reimport Eligible Textures`
	- Reimports currently eligible textures across the project.
- `Tools > BC4 Linear Import > Diagnostics > Observe Targeting`
	- Logs targeting observations to help you understand how Unity is treating a test texture.

## What this documentation covers

This documentation assumes BC4LinearImport is already present in your Unity project.

It covers how to use the tool after that point. It does **not** cover installation or setup steps.

## Read next

- [Usage guide](docs/usage.md)
- [Troubleshooting guide](docs/troubleshooting.md)

If you are here because reimport found `0 eligible textures` or a texture stayed unchanged, head straight to the [troubleshooting guide](docs/troubleshooting.md).