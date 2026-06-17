; winC2D Inno Setup Script
; Build: iscc setup.iss (requires Inno Setup 6+: https://jrsoftware.org/isinfo.php)
; Output: installer\Output\winC2D-Setup.exe

#define AppName "winC2D"
#define AppVersion "4.3.0"
#define AppPublisher "Aknirex"
#define AppURL "https://github.com/Aknirex/winC2D"
#define AppExeName "winC2D.App.exe"

[Setup]
AppId={{B8F4A3D2-7E1C-4A5B-9D6E-1F8C3A2B7D4E}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}

; Default to D:\Program Files — NEVER install to C: by default
DefaultDirName={code:GetDefaultDir}

; Don't allow "C:\Program Files" as default (but user can override)
DisableDirPage=no
DirExistsWarning=no

; Use modern wizard style
WizardStyle=modern

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; Output
OutputDir=Output
OutputBaseFilename=winC2D-Setup-{#AppVersion}

; Icons
SetupIconFile=..\winc2d.ico
UninstallDisplayIcon={app}\{#AppExeName}

; Privileges — always require admin (migration needs Program Files access)
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut(&D)"; GroupDescription: "Shortcuts:"
Name: "agentskill"; Description: "Install AI agent skill (for Kilo Code / Claude Code)"; GroupDescription: "AI Integration:"; Flags: checkedonce

[Files]
; Main executables (single-file self-contained publish)
Source: "..\publish\app\winC2D.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\cli\winC2D.Cli.exe"; DestDir: "{app}"; Flags: ignoreversion

; gsudo (bundled for inline elevation — MIT licensed)
Source: "..\publish\gsudo.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Documentation (copied to publish/ by CI)
Source: "..\publish\AGENTS.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\publish\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

; AI agent skill — installs to %USERPROFILE%\.agents\skills\winc2d\
Source: "..\publish\winc2d-skill\SKILL.md"; DestDir: "{code:GetSkillDir}"; Flags: ignoreversion uninsneveruninstall; Tasks: agentskill

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{autoprograms}\{#AppName} CLI"; Filename: "{app}\winC2D.Cli.exe"; Parameters: "help"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Clean removal of installed files
Filename: "{cmd}"; Parameters: "/c rd /s /q ""{app}"""; Flags: runhidden; RunOnceId: "CleanAppDir"

[Code]
// ── Default install directory: prefer D:\Program Files\winC2D ──────────────

function GetDefaultDir(Param: string): string;
var
  Drive: Integer;
  Found: string;
begin
  // Default: D:\Program Files\winC2D
  if DirExists('D:\') then
  begin
    Result := 'D:\Program Files\winC2D';
    Exit;
  end;

  // D: not found, try E: through Z:
  Found := '';
  for Drive := Ord('E') to Ord('Z') do
  begin
    if DirExists(Chr(Drive) + ':\') then
    begin
      Found := Chr(Drive) + ':\';
      Break;
    end;
  end;

  if Found <> '' then
    Result := Found + 'Program Files\winC2D'
  else
    Result := 'C:\Program Files\winC2D';
end;

// ── AI Agent skill installation ────────────────────────────────────────────

function GetSkillDir(Param: string): string;
begin
  Result := GetEnv('USERPROFILE') + '\.agents\skills\winc2d';
end;

// ── Warn if installing to C: ────────────────────────────────────────────────

function NextButtonClick(CurPageID: Integer): Boolean;
var
  HasOtherDrive: Boolean;
  D: Integer;
begin
  Result := True;
  
  if CurPageID = wpSelectDir then
  begin
    // Warn if user chose C drive
    if Pos('C:\', Uppercase(ExpandConstant('{app}'))) = 1 then
    begin
      if MsgBox('Not recommended to install on C drive. winC2D is designed to free up C drive space.' + #13#10 +
                'Continue installing to C drive anyway?',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
      begin
        Result := False;
        Exit;
      end;
    end;

    // Warn if only C: drive exists (no other drives to migrate to)
    HasOtherDrive := False;
    for D := Ord('D') to Ord('Z') do
    begin
      if DirExists(Chr(D) + ':\') then
      begin
        HasOtherDrive := True;
        Break;
      end;
    end;

    if not HasOtherDrive then
    begin
      if MsgBox('WARNING: Your system only has a C: drive.' + #13#10#13#10 +
                'winC2D needs another drive as a migration target to free up C drive space.' + #13#10 +
                'This software may not be useful for you.' + #13#10#13#10 +
                'Continue installation anyway?',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;

// ── Uninstall cleanup: remove skill directories ────────────────────────────

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SkillDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    SkillDir := GetEnv('USERPROFILE') + '\.agents\skills\winc2d';
    if DirExists(SkillDir) then
      DelTree(SkillDir, True, True, True);
  end;
end;
