// WixSharp MSI Installer Build Script
// Usage: dotnet-script installer/BuildInstaller.csx (or csi.exe)
// Pre-requisite: dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true

#r "nuget: WixSharp, 1.25.0"
using System;
using System.IO;
using WixSharp;
using WixSharp.CommonTasks;

var publishExePath = @"src\ShootingKeyboard\bin\Release\net8.0-windows\win-x64\publish\ShootingKeyboard.exe";

if (!System.IO.File.Exists(publishExePath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ERROR] Publish output executable not found at: {publishExePath}");
    Console.WriteLine("Please run the following publish command first:");
    Console.WriteLine("dotnet publish src/ShootingKeyboard/ShootingKeyboard.csproj -c Release -r win-x64 --self-contained true");
    Console.ResetColor();
    Environment.Exit(1);
}

var project = new Project("ShootingKeyboard",
    new Dir(@"%ProgramFiles%\ShootingKeyboard",
        new File(publishExePath),
        new Dir("Resources",
            new DirFiles(@"src\ShootingKeyboard\Resources\DefaultSounds\*.*")
        ),
        new WixSharp.Shortcut("Shooting Keyboard", @"%ProgramMenu%\ShootingKeyboard")
    ),
    new RegValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run",
        "ShootingKeyboard", "[INSTALLDIR]ShootingKeyboard.exe", new WixSharp.Condition("STARTWITHWINDOWS=\"1\""))
);

project.GUID = new Guid("98A3F29B-C57E-4E6C-81B5-9B2EF91A20D1");
project.Version = new Version("1.1.0");
if (System.IO.File.Exists("LICENSE.txt"))
{
    project.LicenceFile = "LICENSE.txt";
}
project.ControlPanelInfo.Manufacturer = "ShootingKeyboard";
project.ControlPanelInfo.HelpLink = "https://github.com/sunnydev07/ShootingKeyboard";

Compiler.BuildMsi(project, "ShootingKeyboard-1.1.0.msi");
Console.WriteLine("Installer built successfully: ShootingKeyboard-1.1.0.msi");
