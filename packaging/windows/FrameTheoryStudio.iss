#define AppName "Frame Theory Studio AI"
#define AppPublisher "Frame Theory"
#define AppExeName "FrameTheory.Desktop.exe"
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "artifacts\installer-publish"
#endif
#ifndef OutputDir
  #define OutputDir "artifacts\installer"
#endif
#ifndef OutputBaseFilename
  #define OutputBaseFilename "FrameTheoryStudioAI-win-x64"
#endif

[Setup]
AppId={{D91E3794-DB26-4C66-9E7F-4CBF4BB4C51F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\FrameTheoryStudioAI
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
Compression=lzma
SolidCompression=yes
Uninstallable=yes
WizardStyle=modern

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
