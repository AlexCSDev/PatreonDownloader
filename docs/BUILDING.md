## Prerequisites
* All platforms: [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* Windows (optional): [Microsoft Visual Studio 2022](https://visualstudio.microsoft.com/en/vs/)

## IMPORTANT: Cloning PatreonDownloader properly
You need to clone submodules in order to build PatreonDownloader.
If you have not yet cloned the repository use **git clone --recurse-submodules**.
If you have cloned the repository without submodules use **git submodule update --init --recursive**.

## Running from source code on all platforms
1. Launch command line in **PatreonDownloader.App** folder
2. Execute **dotnet run**

## Building framework-dependent executable via Visual Studio on Windows
1. Open **PatreonDownloader.sln** solution
2. Select desired build configuration in build toolbar and build solution by pressing Build -> Build Solution
3. Refer to steps 3-4 of **Building framework-dependent executable via command line on all platforms** for further instructions.

The resulting executable will require .NET Core Runtime to be installed on the computer in order to run.

## Building framework-dependent executable via command line on all platforms
1. Launch command line in **PatreonDownloader.App** folder
2. Execute **dotnet build -c release** (replace **-c release** with **-c debug** to build debug build)
3. Compiled application will be placed into **PatreonDownloader.App\bin\\(Release/Debug)\net6.0**
4. Navigate to **PatreonDownloader.App\bin\\(Release/Debug)\net6.0** folder and run **dotnet PatreonDownloader.App.dll**

[Refer to official documentation to learn more about "dotnet build" command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore31)

The resulting executable will require .NET Core Runtime to be installed on the computer in order to run.

## Building standalone executable via Visual Studio on Windows
1. Open **PatreonDownloader.sln** solution
2. Right click on **PatreonDownloader.App** solution and click **Publish**
3. Select desired publish profile and click **Publish**. 

Application will be compiled and published in **PatreonDownloader.App\bin\publish\net6.0-(win/linux)-(x86/x64)-(release/debug)**. 

The application will be published as self-contained application and will not need .NET Runtime to function. To run the application use **PatreonDownloader.App(.exe)** executable.

## Building standalone executable via command line on all platforms
1. Launch command line in **PatreonDownloader.App** folder
2. Run the following command to build self-contained release build targeting .NET 6.0:

   Windows x64: **dotnet publish -c Release -r win-x64 --self-contained -f net6.0 -o bin\publish\net6.0-win-x64-release**

   Linux x64: **dotnet publish -c Release -r linux-x64 --self-contained -f net6.0 -o bin/publish/net6.0-linux-x64-release**

[Refer to official documentation to learn more about "dotnet publish" command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish?tabs=netcore31)

Application will be compiled and published in folder specified by the **-o** parameter.

The application will be published as self-contained application and will not need .NET Core Runtime to function. To run the application use **PatreonDownloader.App(.exe)** executable.

## Putting additional files into PatreonDownloader folder
PatreonDownloader uses additional plugins from Universal Download Platform which needs to be placed into appropriate folders in order to work correctly.

Google drive:
After building plugin binaries go to the output folder and copy **Google.Apis.Auth.dll, Google.Apis.Auth.PlatformServices.dll, Google.Apis.Core.dll, Google.Apis.dll, Google.Apis.Drive.v3.dll and UniversalDownloaderPlatform.GoogleDriveDownloader.dll** files into the **plugins** folder inside of PatreonDownloader folder.

Mega.nz:
After building plugin binaries go to the output folder and copy **MegaApiClient.dll and UniversalDownloaderPlatform.MegaDownloader.dll** files into the **plugins** folder inside of PatreonDownloader folder.