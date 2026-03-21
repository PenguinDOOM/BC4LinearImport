# Common Questions

## Q: What is this?
A: BC4LinearImport is an editor extension for Unity that allows PNG, JPG, and JPEG textures to be imported as linear data instead of sRGB when Unity converts them to BC4.

## Q: How do I use it?
A: Roughly speaking, check the settings in Project Settings > BC4 Linear Import and perform a re-import if necessary. Please refer to the Usage Guide (usage.md) for detailed steps.

## Q: Where is it located?
A: You can find the main settings and the re-import button under Project Settings > BC4 Linear Import. Additionally, you can access re-import functions and diagnostic tools from the Tools > BC4 Linear Import menu.

## Q: What happens to textures already in my project?
A: BC4LinearImport works automatically when textures meeting the criteria are imported. Therefore, textures already in your project before changing these settings may require manual reprocessing. Please refer to the Usage Guide (usage.md) for how to perform a re-import.

## Q: How do I install it?
A: Please install it via ALCOM or VCC using this link: https://raw.githubusercontent.com/PenguinDOOM/lilToonMore/refs/heads/master/vpm.json

## Q: Which files are targeted?
A: PNG, JPG, or JPEG files are targeted.

## Q: Can I include and distribute this with my own products?
A: You can distribute it under the GPL-3.0 license, but it is not recommended. GPL-3.0 is not compatible with the VN3 license, which is widely used on Booth. Projects containing code distributed under GPL-3.0 must also be distributed under the GPL-3.0 license. This means your product must also be distributed under GPL-3.0. Since GPL-3.0 requires the disclosure of source code, your product must also have its source code made public. The source code must be hosted in a place accessible to anyone without needing to purchase it.

## Q: Why GPL-3.0?
A: To prevent bundling by creators. Bundling can cause unnecessary issues, such as overwriting with older versions or the presence of multiple conflicting versions. Because GPL-3.0 requires that any redistribution be under the same license, it effectively prevents bundling by creators who wish to use the VN3 license.

## Q: What if I bundle it anyway?
A: If you follow the GPL-3.0 license, there is no problem. Distribution under any other license would be a license violation. You might get roasted by the experts on X. If you don't get caught, well...

## Q: Someone sold a copy of this for a fee.
A: Since GPL-3.0 is a license that allows commercial use, there is no issue under the license as long as the conditions of GPL-3.0 are met.

## Q: Is there anything suspicious in it? Is it safe?
A: The source code is public, so anyone can verify it. If you have any concerns, please check the source code. Furthermore, the installation zip file is automatically generated via GitHub Actions, and there is no external interference during the build process. The build includes a commit signature check, so it cannot be triggered by anyone other than the author.