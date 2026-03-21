# BC4LinearImport Troubleshooting

This page helps you figure out why a texture did not convert, why reimport found nothing to do, and which behaviors are expected.

If you want the normal workflow first, read the [usage guide](usage.md). If you want the short overview, see the [README](../README.md).

## Quick answers

### Why did reimport say `0 eligible textures`?

That means nothing in the current project passed **all** of the required checks at the same time.

The most common verified reasons are:

- `Enable BC4 linear import` is turned off in `Project Settings > BC4 Linear Import`
- the texture or its parent folder is listed under `Excluded assets and folders`
- the file is not a supported source type (`PNG`, `JPG`, or `JPEG`)
- Unity is not currently treating the texture as BC4-eligible for this workflow

If you need help checking those items, see [What counts as BC4-eligible?](#what-counts-as-bc4-eligible) and [Exclusions and project-wide disable](#exclusions-and-project-wide-disable).

### Why didn’t my texture convert?

BC4LinearImport only acts when **all** of these are true:

1. the source file is `PNG`, `JPG`, or `JPEG`
2. `Enable BC4 linear import` is enabled
3. the asset path is not excluded
4. the texture importer is currently BC4-eligible
5. the detector decides it should convert, instead of safely skipping or returning an unknown result

If any one of those checks fails, the texture can stay unchanged. That is usually a safe no-op, not a crash.

### Does seeing `BC4` in the Default panel count?

No. Seeing `BC4` in the Default panel by itself does **not** count as eligible for this workflow.

The safest documented path is an explicit **Standalone BC4** setup. There is also an observed automatic **Single Channel** path that can qualify, but that path is conditional and should not be treated as guaranteed.

### What happens if compute shaders are unavailable?

Conversion prefers the compute path when it can use it, but it is **not** a hard requirement.

If compute is unavailable, unsupported for that imported texture, or the compute attempt fails, the tool falls back to the CPU path. That means conversion can still succeed; it just does not depend on compute support being present.

## Supported source formats

The supported source file types are:

- `PNG`
- `JPG`
- `JPEG`

That support is based on the source file extension. If the original file is something else, BC4LinearImport will not process it.

## What counts as BC4-eligible?

In plain language, BC4LinearImport is for textures that Unity is already treating as **BC4-targeted** on the Standalone side.

### Safest documented path

The clearest supported case is:

- the Standalone override is active
- the Standalone format is explicitly `BC4`

If you want the most predictable result, this is the path to aim for.

### Observed conditional path

There is also an observed automatic path where Unity can still count the texture as eligible when:

- the Standalone override is **not** active
- the automatic Standalone format resolves to `BC4`
- the texture type is `Single Channel`
- the single-channel component is `Red` or `Alpha`

This path is useful, but it is environment-dependent. Treat it as **observed behavior**, not a promise that every Unity setup will show it.

### What does not count

These cases are verified non-eligible examples:

- `BC4` appears in the Default panel, but the Standalone override is inactive
- the Standalone override is active, but the Standalone format is **not** `BC4`
- a `Single Channel` texture does **not** resolve to automatic Standalone `BC4`

## Common reasons nothing changed

If a texture stays unchanged, these are the evidence-backed reasons to check.

### 1. Unsupported source extension

BC4LinearImport only supports `PNG`, `JPG`, and `JPEG` sources.

If the file extension is something else, the workflow safely does nothing.

### 2. Project-wide disable is active

If `Enable BC4 linear import` is off, the workflow is disabled for the whole project.

That affects both:

- automatic import-time behavior
- manual reimport through `Reimport Eligible PNG/JPG/JPEG Textures` or `Tools > BC4 Linear Import > Reimport Eligible Textures`

### 3. The asset or folder is excluded

If the exact asset path is excluded, or the texture is inside an excluded folder, BC4LinearImport leaves it alone.

This also affects both automatic import-time behavior and manual reimport.

### 4. The importer is not currently BC4-eligible

Even if the file is grayscale, that is not enough by itself.

The importer still has to qualify through the BC4-targeting rules described above. If it does not, the workflow stops before conversion.

### 5. The detector safely chose not to convert

After the targeting checks pass, the source detector still has three possible outcomes:

- **Convert to linear**: conversion runs
- **Skip conversion**: the source appears to already be linear-authored or should stay as-is
- **Unknown**: the tool could not classify the source safely, so it does nothing instead of guessing

So a no-op can be expected even for an otherwise eligible texture.

### 6. The original source bytes were unavailable or unreadable

The detector reads the original source file to decide whether conversion should happen.

If the source bytes are missing, empty, truncated, or otherwise fail safe validation, the workflow keeps the texture unchanged rather than making an unsafe guess.

### 7. The automatic path was not observed in your environment

If you are relying on the automatic `Single Channel` path, remember that it is conditional.

When Unity does **not** resolve that case to automatic Standalone `BC4`, the texture is not eligible for this workflow.

## Exclusions and project-wide disable

These two settings are easy to mix up:

- `Enable BC4 linear import` turns the whole workflow on or off for the project
- `Excluded assets and folders` keeps the workflow enabled, but skips only the specific assets or folders you add

Use exclusions when you only want to leave a few textures alone.

Use the project-wide toggle when you want BC4LinearImport to stop acting on the project entirely.

## Diagnostics

If you are unsure whether Unity is exposing the conditional automatic path in your environment, use:

- `Tools > BC4 Linear Import > Diagnostics > Observe Targeting`

This command is most useful when:

- you want to check whether a `Single Channel` setup is being observed as automatic Standalone `BC4`
- you want extra evidence before assuming a texture should count as BC4-eligible

Think of it as an observation tool, not a repair tool. It helps answer “is Unity treating this setup as BC4-targeted here?”

## Compute fallback

BC4LinearImport prefers a compute-based conversion path when it is available and usable.

If that path is unavailable, unsupported for the imported texture, or fails during the attempt, the tool falls back to the CPU conversion path.

That means:

- missing compute support is **not** a hard failure for the feature
- conversion can still complete through the CPU path
- a compute-related warning does not automatically mean the texture was left unconverted

## When to read which page

- Start with the [README](../README.md) for the short overview.
- Use the [usage guide](usage.md) for the normal workflow, exclusions, and reimport steps.
- Stay on this page when you are asking “why didn’t anything happen?”