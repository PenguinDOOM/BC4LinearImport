## Overview

> This document defines the agents used in the BC4LinearImport project, including their roles, responsibilities, and interactions. Each agent is designed to perform specific tasks related to the development of the BC4 Linear Import feature for Unity, ensuring efficient collaboration and progress towards project goals.

- Git repository: Available
- Unity version: 2022.3.22f1
- Render pipeline: Built-in
- Target platform: Editor
- Target user: VRChat users who modify avatars and create worlds. However, their understanding of Unity itself is limited.
- C# version: 9.0
- Direct X version: 11 (Shader Model 5.0)

## Document commenting

- At the beginning of the file, you write what the file does.
- In C#, you write XML documentation comments for classes, structures, methods, and other things that can become symbols.
- In HLSL, you write XML documentation comments for things that can be symbols, such as functions.

### File Path

```
./BC4LinearImport/plans/ - Directory containing project plans and documentation
./BC4LinearImport/completes/ - Directory containing completed tasks and implementations
./BC4LinearImport/guardrails/ - Directory containing each agent's guardrails
./BC4LinearImport/docs/ - Directory containing project documentation and resources
./BC4LinearImport/.github/AGENTS.md - This file, containing agent definitions and configurations
./BC4LinearImport/Assets/BC4LinearImport/ - The main directory for the BC4 Linear Import feature, containing all related assets and scripts
./BC4LinearImport/Assets/BC4LinearImport/Editor/ - Contains editor scripts for the BC4 Linear Import targeting and reimport utility
./BC4LinearImport/Assets/BC4LinearImport/Tests/EditMode/ - Contains EditMode tests for the BC4 Linear Import feature, organized by related functionality. Not included in the release

./plans ./completes ./guardrails are for internal use and should not be included in the release. The main deliverables for the release are the files in `Assets/BC4LinearImport/` and its subdirectories, which contain the implementation of the BC4 Linear Import feature and its associated tests.
```

### About BC4LinearImport

BC4LinearImport is an editor extension that automatically converts the color space of textures from sRGB to Linear when importing BC4 files.

### Info
- The front-end should be easy to understand and clean, even for users with limited technical knowledge.
- The backend doesn't need to consider users with limited technical knowledge, but it should be designed to be maintainable rather than complex.
