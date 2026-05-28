#define MyAppVersion "1.0.1"

[Setup]
AppId={{b779f78f-2393-481c-93d0-b49b2df5b1a1}
AppName=FolMinder2
AppVersion={#MyAppVersion}
AppPublisher=Hernian <hernianrunner@gmail.com>
AppPublisherURL=https://github.com/hernian/FolMinder2
VersionInfoVersion={#MyAppVersion}
DefaultDirName={localappdata}\Hernian\FolMinder2
DefaultGroupName=FolMinder2
OutputDir=Output
OutputBaseFilename=FolMinder2_setup{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\FolMinder2.exe
LicenseFile=..\LICENSE.txt
WizardStyle=modern
SetupIconFile=folminder.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\FolMinder2\bin\Release\net10.0-windows\*"; \
        DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FolMinder2"; Filename: "{app}\FolMinder2.exe"
Name: "{userstartup}\FolMinder2"; Filename: "{app}\FolMinder2.exe"; Tasks: startupicon

[Tasks]
Name: "startupicon"; Description: "Start FolMinder2 at &Windows startup"; GroupDescription: "Additional icons:"

[InstallDelete]
; 古いバージョンのファイルを削除
Type: filesandordirs; Name: "{app}\*"

[UninstallDelete]
; ユーザー設定ファイルを削除するか確認
Type: filesandordirs; Name: "{localappdata}\FolMinder2"

[Run]
Filename: "{app}\FolMinder2.exe"; Description: "Launch FolMinder2"; Flags: nowait postinstall skipifsilent
