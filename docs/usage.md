# BC4LinearImport Usage Guide

This guide shows the normal day-to-day workflow for BC4LinearImport after the package is already in your Unity project.

If you want the short version: open `Project Settings > BC4 Linear Import`, keep `Enable BC4 linear import` enabled, add exclusions only for textures or folders you want to skip, and run a reimport if the textures were already in the project before you changed the settings.

## Overview

BC4LinearImport helps Unity import eligible grayscale `PNG` / `JPG` / `JPEG` textures as linear data instead of sRGB.

The important word is **eligible**. This tool is meant for textures that Unity is treating as **BC4-targeted**.

For everyday use, the safest and clearest setup is:

- your texture is a grayscale `PNG`, `JPG`, or `JPEG`
- Unity is set up so the texture is explicitly using **Standalone BC4**

There is also an automatic **Single Channel** path that can make Unity treat a texture as BC4 on Standalone, but that behavior is more conditional. It is best treated as an advanced case, not a guarantee.

## Before you start

Before changing anything, keep these practical rules in mind:

- This guide does **not** cover installation or setup. It assumes BC4LinearImport is already present in your project.
- The easiest supported file types are `PNG`, `JPG`, and `JPEG`.
- The safest eligibility rule is: **explicit Standalone BC4** is the recommended path.
- If a texture is not explicitly using **Standalone BC4**, do not assume it will count as eligible.
- If you are relying on **Single Channel** automatic BC4 behavior, treat it as conditional and verify it when needed.

## Open the settings page

1. Open Unity.
2. Go to `Project Settings > BC4 Linear Import`.
3. You will see the main controls for:
   - `Enable BC4 linear import`
   - `Excluded assets and folders`
   - `Reimport Eligible PNG/JPG/JPEG Textures`

This is the main page you will use most of the time.

## Enable or disable the feature

Use `Enable BC4 linear import` to turn the workflow on or off for the whole project.

- **Enabled:** supported, eligible textures can be processed when they are imported.
- **Disabled:** BC4LinearImport stops acting on textures in this project.

If you only want to skip a few textures, it is usually better to leave the feature enabled and use exclusions instead of turning everything off.

## Understand which textures are the safest fit

You do **not** need to memorize Unity internals, but this one distinction matters:

- **Recommended / safest:** the texture is explicitly set to **Standalone BC4**.
- **Advanced / conditional:** Unity automatically chooses BC4 for a **Single Channel** texture on Standalone.

If you want the most predictable results, use the explicit Standalone BC4 route.

In plain language, BC4LinearImport is not a "convert every grayscale image" button. It only works on textures that Unity is already treating as BC4-targeted.

## Exclude specific assets or folders

Use exclusions when you want BC4LinearImport to leave certain textures alone.

### Add exclusions

1. Open `Project Settings > BC4 Linear Import`.
2. Find `Excluded assets and folders`.
3. Drag a project asset or a project folder into the area labeled `Drop project assets or folders here`.
4. Confirm that the item appears in `Stored exclusion paths`.

You do not need to type raw paths manually. The intended workflow is drag-and-drop from your project.

### Remove one exclusion

- In `Stored exclusion paths`, click `Remove` next to the item you no longer want excluded.

### Clear everything

- Click `Clear all` to remove every stored exclusion.

### What exclusions affect

Exclusions apply to:

- the exact asset you excluded
- everything inside an excluded folder
- automatic import-time behavior
- manual bulk reimport

So if a texture is excluded, it will also be skipped by the reimport tools described below.

## Reimport textures that were already in the project

BC4LinearImport works automatically when eligible textures are imported. That means existing textures may need a manual repair pass if they were already in the project before your current settings were in place.

Run a bulk reimport when:

- you just enabled `Enable BC4 linear import`
- you removed an exclusion and want previously skipped textures to be processed again
- you changed a texture so it is now BC4-targeted and want Unity to re-run the import flow
- you are not sure whether older imported textures were processed under the current settings

### Reimport from Project Settings

Use the button:

- `Reimport Eligible PNG/JPG/JPEG Textures`

Location:

- `Project Settings > BC4 Linear Import`

### Reimport from the Tools menu

Use the menu item:

- `Tools > BC4 Linear Import > Reimport Eligible Textures`

### What the reimport action actually does

The reimport action scans the project and reimports textures that are all of the following:

- `PNG`, `JPG`, or `JPEG`
- not excluded
- still allowed by `Enable BC4 linear import`
- currently treated by Unity as BC4-targeted

If the reimport count is lower than you expected, the most common reasons are:

- the texture is excluded
- the feature is disabled project-wide
- the texture is not currently BC4-targeted
- the file is not `PNG`, `JPG`, or `JPEG`

If you want a more detailed “why didn’t it convert?” checklist, see the [troubleshooting guide](troubleshooting.md).

## Use diagnostics when behavior is unclear

If a texture did not behave the way you expected and you are not sure whether the issue is the **BC4 targeting side**, use:

- `Tools > BC4 Linear Import > Diagnostics > Observe Targeting`

This is most useful when:

- you are testing whether your Unity environment is exposing the automatic **Single Channel** BC4 path
- you want extra evidence before assuming a texture should count as BC4-targeted
- you need to understand whether Unity is reporting BC4 on the Standalone side in the way this tool expects

Think of this as a quick observation tool, not a repair button. It helps answer "Is Unity treating this kind of setup as BC4-targeted here?" when the answer is not obvious from the normal UI.

## Normal workflow recap

For most users, the easiest routine is:

1. Open `Project Settings > BC4 Linear Import`.
2. Leave `Enable BC4 linear import` enabled.
3. Use exclusions only for textures or folders you want to skip.
4. Prefer an explicit **Standalone BC4** texture setup when you want the safest results.
5. Run `Reimport Eligible PNG/JPG/JPEG Textures` or `Tools > BC4 Linear Import > Reimport Eligible Textures` if the textures were already imported before your current settings.
6. If behavior is still unclear, try `Tools > BC4 Linear Import > Diagnostics > Observe Targeting`.

## Related documentation

- [README](../README.md)
- [Troubleshooting guide](troubleshooting.md)