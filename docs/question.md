# FAQ

## Q: What is this?
A: BC4LinearImport is an editor extension for Unity that automatically converts PNG, JPG, and JPEG textures from sRGB color space to linear color space upon import, specifically for when Unity converts them to BC4. BC4 only supports the linear color space, and without this pre-conversion, the textures may look incorrect.

## Q: How do I use it?
A: Roughly speaking, check the settings in Project Settings > BC4 Linear Import and perform a re-import if necessary. Please refer to the [Usage Guide](usage.md) for detailed steps.

## Q: Where can I find it?
A: The main settings and the re-import button are located in Project Settings > BC4 Linear Import. Additionally, you can access re-import functions and diagnostic tools from the Tools > BC4 Linear Import menu.

## Q: What happens to textures already in my project?
A: BC4LinearImport operates automatically when textures matching the criteria are imported. Therefore, textures that were already in the project before changing these settings may require manual reprocessing. Please see the [Usage Guide](usage.md) for how to perform a re-import.

## Q: How do I install it?
A: Please install it via ALCOM or VCC using the following URL: https://raw.githubusercontent.com/PenguinDOOM/lilToonMore/refs/heads/master/vpm.json

## Q: What file types are affected?
A: PNG, JPG, and JPEG files are targeted.

## Q: Can I include and distribute it with my own products?
A: It can be distributed under the GPL-3.0 license. However, this is not recommended. GPL-3.0 is not compatible with the VN3 license, which is widely used on Booth. Projects containing code distributed under GPL-3.0 must also be distributed under GPL-3.0. In other words, your product must also be distributed under GPL-3.0. GPL-3.0 requires the disclosure of source code; for binary distributions, it requires the disclosure of configuration files necessary for binary generation in addition to the source code. Disclosure is only required for users who receive the product, not for those who do not.

## Does the converted image become GPL-3.0?
A: No, it does not. Unless the GPL program code is embedded into the converted data, the license of the generated content does not need to be GPL. If the converted data were also forced to be GPL, models created in Blender or profiles for Mochifitter would become GPL, making them impossible to distribute under the VN3 license.

## What is GPL-3.0 in the first place?
A: Please look it up. You will find the answer immediately. We have prepared a version forcibly reproduced under VN3 [Reproduction](GPLUnderVN3.md), but honestly, it is not very interesting to look at since all individual conditions are permitted.

## Q: Why GPL-3.0?
A: To prevent bundling by creators. Bundling can cause unnecessary issues, such as overwriting with older versions or the coexistence of multiple versions. Since GPL-3.0 requires that redistributed work be distributed under the same license, it has the effect of preventing bundling by creators who wish to use the VN3 license.

## Q: What if I bundle it anyway?
A: If it is under the GPL-3.0 license, there is no problem. Distribution under other licenses would be a license violation. You might get "beat up" by knowledgeable people on X (formerly Twitter). Well, if you don't get caught...

## Q: A copycat product was being sold for a fee.
A: GPL-3.0 is a license that allows commercial use, so there is no problem under the license as long as the conditions of GPL-3.0 are met.

## Q: Is there anything suspicious in it? Is it safe?
A: The source code is public, so anyone can check it. If you have any concerns, please review the source code. Furthermore, the installation zip is automatically generated via GitHub Actions, with no external intervention during the build process. The build includes a commit signature check, so it cannot be triggered by anyone other than the author.
