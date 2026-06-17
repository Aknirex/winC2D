; winC2D Inno Setup Script
; Build: iscc setup.iss (requires Inno Setup 6+: https://jrsoftware.org/isinfo.php)
; Output: installer\Output\winC2D-Setup.exe

#define AppName "winC2D"
#define AppVersion "4.2.0"
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

; Privileges
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut(&D)"; GroupDescription: "Shortcuts:"

[Files]
; Main executables (CI publishes to ../publish/app and ../publish/cli)
Source: "..\publish\app\winC2D.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\cli\winC2D.Cli.exe"; DestDir: "{app}"; Flags: ignoreversion

; Elevation wrapper (copied to publish/cli by CI; also at source for local build)
Source: "..\publish\cli\run-elevated.ps1"; DestDir: "{app}"; Flags: ignoreversion

; gsudo (bundled for inline elevation — MIT licensed, may be missing if CI download failed)
Source: "..\publish\gsudo.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Documentation (copied to publish/ by CI)
Source: "..\publish\AGENTS.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\publish\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

; Kilo Code skill — installs to user's skill directory
Source: "..\publish\winc2d-skill\SKILL.md"; DestDir: "{code:GetSkillDir}"; Flags: ignoreversion uninsneveruninstall

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
  D: string;
  LargestDrive: string;
  LargestFree: Int64;
  Drive: Integer;
  Free: Int64;
begin
  ; Default to D:\Program Files\winC2D
  D := 'D:\Program Files\winC2D';

  ; If D: doesn't exist or has very low space, find the largest non-C drive
  if not DirExists('D:\') then
  begin
    LargestDrive := '';
    LargestFree := 0;
    for Drive := Ord('D') to Ord('Z') do
    begin
      if GetSpaceOnDisk(Chr(Drive) + ':\', Free) then
      begin
        if Free > LargestFree then
        begin
          LargestFree := Free;
          LargestDrive := Chr(Drive) + ':\';
        end;
      end;
    end;

    if LargestDrive <> '' then
      Result := LargestDrive + 'Program Files\winC2D'
    else
      Result := 'D:\Program Files\winC2D';
  end
  else
    Result := D;
end;

// ── Kilo Code skill installation ───────────────────────────────────────────

function GetSkillDir(Param: string): string;
begin
  Result := ExpandConstant('{userprofile}\.kilocode\skills\winc2d');
end;

// ── Warn if installing to C: ────────────────────────────────────────────────

function NextButtonClick(CurPageID: Integer): Boolean;
var
  DriveCount: Integer;
  D: Integer;
  Free: Int64;
begin
  Result := True;
  
  if CurPageID = wpSelectDir then
  begin
    // Warn if user chose C drive
    if Pos('C:\', Uppercase(ExpandConstant('{app}'))) = 1 then
    begin
      if MsgBox('不建议安装到 C 盘。winC2D 的目的就是释放 C 盘空间。' + #13#10 +
                '确定要继续安装到 C 盘吗？',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
      begin
        Result := False;
        Exit;
      end;
    end;

    // Warn if only one fixed drive exists (only C:)
    DriveCount := 0;
    for D := Ord('C') to Ord('Z') do
    begin
      if GetSpaceOnDisk(Chr(D) + ':\', Free) and (Free > 0) then
      begin
        DriveCount := DriveCount + 1;
        if DriveCount >= 2 then
          Break;
      end;
    end;

    if DriveCount < 2 then
    begin
      if MsgBox('⚠️  警告：您的系统只有一个磁盘卷（C 盘）。' + #13#10#13#10 +
                'winC2D 需要另一个磁盘作为迁移目标，否则无法释放 C 盘空间。' + #13#10 +
                '这个软件很可能帮不了您。' + #13#10#13#10 +
                '是否仍然继续安装？',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;

// ── Uninstall cleanup: remove skill directory ──────────────────────────────

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SkillDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    SkillDir := ExpandConstant('{userprofile}\.kilocode\skills\winc2d');
    if DirExists(SkillDir) then
      DelTree(SkillDir, True, True, True);
  end;
end;
