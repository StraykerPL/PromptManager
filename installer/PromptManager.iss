#define MyAppName "Prompt Manager"
#define MyAppExeName "PromptManager.exe"
#define MyAppPublisher "Strayker Software"
#define MyAppVersion "1.0"
#define MyAppTargetFramework "net10.0-windows10.0.19041.0"
#define MyAppRuntime "win-x64"
#define MyAppPublishDir "..\PromptManager\bin\Release\" + MyAppTargetFramework + "\" + MyAppRuntime + "\publish"

; From the repository root, build the publish folder before compiling this installer:
;   dotnet publish .\PromptManager\PromptManager.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained true
;   ISCC.exe .\installer\PromptManager.iss

[Setup]
AppId={{F36C805E-7CF1-4BDF-8E1C-04606A42A1F9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\build
OutputBaseFilename=PromptManagerSetup-{#MyAppVersion}-x64
SetupIconFile=..\PromptManager\Resources\Images\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
