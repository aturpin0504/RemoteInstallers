# RemoteInstallersLibrary

RemoteInstallersLibrary is a .NET class library that provides functionalities for remote file copying and script execution. This library is designed to facilitate operations on remote computers, such as copying files and directories, running scripts, and managing remote installations using PowerShell.

## Features

- Asynchronous file and directory copying to remote computers.
- Remote execution of MSI, MSU, PowerShell scripts, VBScript, batch files, and registry file imports.
- Utilities to check if a remote computer is online, perform DNS lookups for IPv4 addresses, and validate computer names in Active Directory.

## Installation

### Via NuGet Package

1. **Install from a Local Source**:

   Store the `.nupkg` file on a shared drive and configure Visual Studio to use that shared drive as a NuGet source:

   - Go to `Tools` > `Options` in Visual Studio.
   - Expand `NuGet Package Manager` and select `Package Sources`.
   - Click the `+` button to add a new package source.
   - Set the `Name` to `SharedDrivePackages`.
   - Set the `Source` to the path of the shared drive folder, e.g., `\\SharedDrive\NuGetPackages`.
   - Click `OK` to save the changes.

2. **Install the Package**:

   - Right-click your project in Solution Explorer and select `Manage NuGet Packages`.
   - Select the `SharedDrivePackages` source from the drop-down list.
   - Find and install `RemoteInstallersLibrary`.

## Usage

Add the following using directives to your code files:

```csharp
using RemoteInstallers;
```

### Example Usage

#### Copy a Single File

```csharp
public static async Task Main(string[] args)
{
    var fileResult = await RemoteFileCopier.CopyFileAsync(@"C:\localpath\file.txt", "RemotePCName", @"D:\remotepath\file.txt");
    Console.WriteLine(fileResult.Message);
}
```

#### Copy Multiple Files

```csharp
public static async Task Main(string[] args)
{
    List<string> files = new List<string> { @"C:\localpath\file1.txt", @"C:\localpath\file2.txt" };
    var filesResult = await RemoteFileCopier.CopyFilesAsync(files, "RemotePCName", @"D:\remotepath");
    Console.WriteLine(filesResult.Message);
}
```

#### Copy a Directory

```csharp
public static async Task Main(string[] args)
{
    var directoryResult = await RemoteFileCopier.CopyDirectoryAsync(@"C:\localpath\directory", "RemotePCName", @"D:\remotepath\directory");
    Console.WriteLine(directoryResult.Message);
}
```

#### Check if Remote Computer is Online

```csharp
public static async Task Main(string[] args)
{
    bool isOnline = await RemoteInstaller.IsRemoteComputerOnlineAsync("RemotePCName");
    Console.WriteLine($"Is online: {isOnline}");
}
```

#### Get Computer Name from IP

```csharp
public static async Task Main(string[] args)
{
    string computerName = await RemoteInstaller.GetComputerNameFromIpAsync("192.168.1.1");
    Console.WriteLine($"Computer name: {computerName}");
}
```

#### Validate Computer Name in Active Directory

```csharp
public static async Task Main(string[] args)
{
    bool isValidInAD = await RemoteInstaller.IsComputerNameValidInActiveDirectoryAsync("RemotePCName");
    Console.WriteLine($"Is valid in AD: {isValidInAD}");
}
```

#### Run an MSI Remotely

```csharp
public static async Task Main(string[] args)
{
    var installResult = await RemoteInstaller.RunMsiRemotelyAsync("RemotePCName", "/i \"path\\to\\installer.msi\" /quiet", 10);
    Console.WriteLine($"Install Result: {installResult.StandardOutput}");
}
```

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
