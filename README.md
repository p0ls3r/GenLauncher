<p align="center">
  <img src="GenLauncherNet/fd.ico" width="100" alt="GenLauncher Icon">
</p>

<h1 align="center">GenLauncher</h1>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows">
  <a href="https://discord.gg/fFGpudz5hV">
    <img src="https://img.shields.io/badge/Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Discord">
  </a>
</p>

**GenLauncher** is a mod management utility for **Command & Conquer: Generals** and its expansion **Zero Hour** that simplifies the process of setting up and maintaining a modded game experience.

## Features

- **Multi-Game Support**: Compatible with both Command & Conquer: Generals and its expansion Zero Hour.
- **Repository Integration**: Easily download, install, and update mods from repositories.
- **Patch and Addon Management**: Download, install, and update patches and addons for both installed mods and the vanilla game.
- **Manually Import Mods, Patches, and Addons**: Import mods, patches, and addons manually from your local filesystem.
- **Multiple Mod Support**: Run multiple mods from a single game directory.
- **Game Launch**: Launch the game or world builder with specific mods and their corresponding patches/addons.
- **Command-Line Arguments**: Support for windowed mode, quickstart mode, as well as custom command-line arguments.
- **Game Directory Is Kept Clean**: Mods, patches, and addons are consolidated into their own directories and then linked to the game directory upon game launch and then unlinked when the game exits, keeping the game folder clean.
- **Built-in Graphics Options Menu**: Comprehensive options menu for adjusting all of the game's graphical settings as well as setting custom in-game camera height.
- **Modded Executable**: Installs and uses the [modded game executable](https://www.gentool.net/download/executables/) by xezon for enhanced features. (Optional)
- **GenTool Integration**: Install and update [GenTool](https://www.gentool.net/) by xezon. (Optional)

## Installation

### Prerequisites

- Either **Command & Conquer: Generals** or **Zero Hour** installed (or both)
- [.NET Framework 4.6 or higher](https://dotnet.microsoft.com/en-us/download/dotnet-framework) (Most likely already installed on your system if you're running Windows 7 or later).
- Game is installed on an NTFS file system (required for symbolic link support).
- System is running a Windows operating system (GenLauncher is not officially supported on non-Windows operating systems).
- Ability to run programs with administrative privileges (most users will have this).

### Steps

1. Download `GenLauncher.exe` from one of the sources listed in the [Download](#download) section.
2. Extract the executable to your game directory (where the game exe is located).
3. Run `GenLauncher.exe`.

## Download

- [ModDB](https://www.moddb.com/mods/genlauncher)
- [GenLauncher Discord](https://discord.gg/fFGpudz5hV)
- [Through GenPatcher](https://legi.cc/downloads/genpatcher/)

## Contributing

We welcome contributions! Please:

1. Use the project's [issue tracker](https://github.com/p0ls3r/GenLauncher/issues) to submit bug reports or feature requests.
2. Join the [GenLauncher Discord](https://discord.gg/fFGpudz5hV) and post in the appropriate channels.
3. Follow the project's coding standards when submitting pull requests.

## Support

If you encounter issues or need help:

- Check the [issue tracker](https://github.com/p0ls3r/GenLauncher/issues) for existing solutions.
- Join our [Discord community](https://discord.gg/fFGpudz5hV) for real-time support.
- Create a new issue with detailed information about your problem.

## Building from Source

For developers who want to build GenLauncher from source:

### Development Requirements

- Visual Studio 2017 or newer with C# and WPF support
- [.NET Framework 4.6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net46)

### Quick Start (Using Visual Studio 2017 or newer)

1. Clone the repository:

   ```bash
   git clone https://github.com/p0ls3r/GenLauncher.git
   ```
2. Open `GenLauncher.sln` in Visual Studio.

3. Restore NuGet packages (Right-click solution → "Restore NuGet Packages").

4. Set `GenLauncherNet` as the startup project.

5. Build and run with the project.

## Donate

Support the project and its development:

[![Donate via Boosty](https://img.shields.io/badge/Donate-Boosty-orange)](https://boosty.to/genlauncher/single-payment/donation/157147?share=target_link)
