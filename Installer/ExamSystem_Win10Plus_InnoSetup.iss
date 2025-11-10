; Inno Setup script for ExamSystem (Win10/11, current architecture: net8.0-windows + WebView2)
; - Publishes WPF app to {app}
; - Detects WebView2 runtime and installs silently if missing
; - Optionally detects .NET 8 Desktop Runtime and installs silently if missing (if you choose non self-contained)
; - Creates Start Menu and Desktop shortcuts
; - Grants write permissions to Logs/Backups under Program Files

#define AppName "消防救援考试系统"
#define AppVersion "1.0.0"
#define AppPublisher "ExamSystem Team"
#define AppExeName "ExamSystem.WPF.exe"

[Setup]
AppId={{2E2F77A1-5A6A-4C65-8B55-6E8F1F2B4A0F}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={pf}\{#AppName}
DefaultGroupName={#AppName}
OutputBaseFilename={#AppName}_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
DisableDirPage=no
DisableProgramGroupPage=no
SetupIconFile=Prerequisites\logo.ico
UninstallDisplayIcon={app}\logo.ico

; Set installer language
SetupLogging=yes

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Files]
; Publish output (adjust path to your publish folder)
Source: "..\ExamSystem.WPF\bin\Release\net8.0-windows10.0.19041\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
; App icon copied to install directory
Source: "Prerequisites\logo.ico"; DestDir: "{app}"; Flags: ignoreversion

; Prerequisites: place files under Installer\Prerequisites before building the installer
; WebView2 Evergreen Bootstrapper (MicrosoftEdgeWebview2Setup.exe)
Source: "Prerequisites\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: (IsWin10OrAbove()) and (not WebView2Installed())

; Optional: .NET 8 Windows Desktop Runtime installer (adjust filename)
; Example filename: dotnet-runtime-8.0.9-win-x64.exe
Source: "Prerequisites\dotnet-runtime-8.0.x-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: (not DotNetDesktopRuntimeInstalled())

[Dirs]
Name: "{app}\Logs"; Permissions: users-modify
Name: "{app}\Backups"; Permissions: users-modify

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\logo.ico"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; IconFilename: "{app}\logo.ico"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "其他选项："; Flags: unchecked

[Run]
; Install WebView2 runtime if not present (Win10+)
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "正在安装 WebView2 运行时..."; Check: (IsWin10OrAbove()) and (not WebView2Installed()); Flags: runhidden

; Optional: install .NET 8 Desktop Runtime if not present
Filename: "{tmp}\dotnet-runtime-8.0.x-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "正在安装 .NET 8 桌面运行时..."; Check: (not DotNetDesktopRuntimeInstalled()); Flags: runhidden

; Launch app after install
Filename: "{app}\{#AppExeName}"; Description: "运行应用"; Flags: nowait postinstall skipifsilent

[Code]
function IsWin10OrAbove(): Boolean;
begin
  { Windows 10 major version = 10 }
  Result := (GetWindowsVersion >= $0A000000);
end;

function QueryWebView2Version(): String;
var
  ver: String;
begin
  Result := '';
  { Check 64-bit view }
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeWebView', 'pv', ver) then
  begin
    Result := ver;
    exit;
  end;
  { Check 32-bit view via WOW6432Node }
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeWebView', 'pv', ver) then
  begin
    Result := ver;
    exit;
  end;
end;

function WebView2Installed(): Boolean;
begin
  Result := QueryWebView2Version() <> '';
end;

function DotNetDesktopRuntimeInstalled(): Boolean;
begin
  { Check .NET Desktop Runtime 8 presence in both x64 and x86 registrations }
  if RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\8.0') then
  begin
    Result := True;
    exit;
  end;
  if RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App\8.0') then
  begin
    Result := True;
    exit;
  end;
  Result := False;
end;