; winC2D Inno Setup Script
; Build: iscc setup.iss (requires Inno Setup 6+: https://jrsoftware.org/isinfo.php)
; Output: installer\Output\winC2D-Setup.exe

#define AppName "winC2D"
; Fallback default -- CI overrides via /DAppVersion=<version>
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
Compression=lzma2/fast
SolidCompression=yes

; Output
OutputDir=Output
OutputBaseFilename=winC2D-Setup

; Icons
SetupIconFile=..\winc2d.ico
UninstallDisplayIcon={app}\{#AppExeName}

; Privileges — always require admin (migration needs Program Files access)
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut(&D)"; GroupDescription: "Shortcuts:"
Name: "agentskill"; Description: "Install AI agent skill"; GroupDescription: "AI Integration:"
Name: "agentskill\universal"; Description: "Universal agents (.config\agents)"; GroupDescription: "AI Integration:"
Name: "agentskill\legacyagents"; Description: "Shared / legacy agents (.agents)"; GroupDescription: "AI Integration:"
Name: "agentskill\codex"; Description: "Codex"; GroupDescription: "AI Integration:"
Name: "agentskill\claudecode"; Description: "Claude Code"; GroupDescription: "AI Integration:"
Name: "agentskill\antigravity"; Description: "Antigravity"; GroupDescription: "AI Integration:"
Name: "agentskill\opencode"; Description: "OpenCode"; GroupDescription: "AI Integration:"
Name: "agentskill\openclaw"; Description: "OpenClaw"; GroupDescription: "AI Integration:"
Name: "agentskill\cursor"; Description: "Cursor"; GroupDescription: "AI Integration:"
Name: "agentskill\copilot"; Description: "GitHub Copilot"; GroupDescription: "AI Integration:"
Name: "agentskill\gemini"; Description: "Gemini CLI"; GroupDescription: "AI Integration:"
Name: "agentskill\kilo"; Description: "Kilo Code"; GroupDescription: "AI Integration:"
Name: "agentskill\cline"; Description: "Cline"; GroupDescription: "AI Integration:"
Name: "agentskill\roo"; Description: "Roo Code"; GroupDescription: "AI Integration:"

[Files]
; Main executables (single-file self-contained publish)
Source: "..\publish\app\winC2D.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\cli\winC2D.Cli.exe"; DestDir: "{app}"; Flags: ignoreversion

; gsudo (bundled for inline elevation — MIT licensed)
Source: "..\publish\gsudo.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Documentation (copied to publish/ by CI)
Source: "..\publish\README.ai.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\publish\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

; Keep one canonical copy. Selected agent directories are junctions to this copy,
; so upgrades update every integration without duplicating files.
Source: "..\publish\winc2d-skill\SKILL.md"; DestDir: "{app}\skills\winc2d"; Flags: ignoreversion

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
const
  INVALID_FILE_ATTRIBUTES = $FFFFFFFF;

var
  AgentTaskDefaultsApplied: Boolean;

function GetFileAttributesW(lpFileName: string): LongWord;
  external 'GetFileAttributesW@kernel32.dll stdcall';

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

function UserHome: string;
begin
  Result := ExpandConstant('{userprofile}');
end;

function IsReparsePoint(Path: string): Boolean;
var
  Attributes: LongWord;
begin
  Attributes := GetFileAttributesW(Path);
  Result := (Attributes <> INVALID_FILE_ATTRIBUTES) and
            ((Attributes and FILE_ATTRIBUTE_REPARSE_POINT) <> 0);
end;

function RemoveSkillLink(LinkDir: string): Boolean;
var
  ResultCode: Integer;
begin
  // rd removes a junction without touching its target. It intentionally fails
  // for a non-empty normal directory, which protects unrelated user content.
  // Run it unconditionally so dangling junctions are removed during uninstall.
  Result := Exec(ExpandConstant('{cmd}'), '/C rd "' + LinkDir + '"', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

procedure DeleteManagedNormalDirectory(Path: string);
var
  Attributes: LongWord;
begin
  Attributes := GetFileAttributesW(Path);
  if Attributes = INVALID_FILE_ATTRIBUTES then
    Exit;
  if (Attributes and FILE_ATTRIBUTE_REPARSE_POINT) <> 0 then
    RaiseException('Refusing to recurse into AI skill reparse point: ' + Path);
  if (Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0 then
    RaiseException('AI skill path is not a directory: ' + Path);

  DelTree(Path, True, True, True);
end;

procedure CreateSkillJunction(LinkDir: string);
var
  ParentDir: string;
  ResultCode: Integer;
begin
  ParentDir := ExtractFileDir(LinkDir);
  ForceDirectories(ParentDir);

  RemoveSkillLink(LinkDir);
  // Never recurse into a surviving junction/symlink. If rd failed, abort this
  // integration instead of risking deletion through the reparse point.
  if IsReparsePoint(LinkDir) then
    RaiseException('Unable to replace AI skill junction: ' + LinkDir);

  // Upgrade the single-directory layout used by winC2D <= 4.3.0. This helper
  // verifies the path is a normal directory before any recursive deletion.
  DeleteManagedNormalDirectory(LinkDir);

  if not Exec(ExpandConstant('{cmd}'),
              '/C mklink /J "' + LinkDir + '" "' +
              ExpandConstant('{app}\skills\winc2d') + '"', '',
              SW_HIDE, ewWaitUntilTerminated, ResultCode) or
     (ResultCode <> 0) then
    RaiseException('Unable to create AI skill junction: ' + LinkDir);
end;

procedure SyncSkillJunction(TaskName, LinkDir: string;
                            RemoveLegacyDirectory: Boolean);
begin
  if WizardIsTaskSelected(TaskName) then
    CreateSkillJunction(LinkDir)
  else
  begin
    RemoveSkillLink(LinkDir);
    if RemoveLegacyDirectory then
    begin
      if IsReparsePoint(LinkDir) then
        RaiseException('Unable to remove AI skill junction: ' + LinkDir);
      DeleteManagedNormalDirectory(LinkDir);
    end;
  end;
end;

function HasExplicitTaskCommandLine: Boolean;
var
  I: Integer;
  Arg: string;
begin
  Result := False;
  for I := 1 to ParamCount do
  begin
    Arg := Uppercase(ParamStr(I));
    if (Pos('/TASKS=', Arg) = 1) or (Pos('/MERGETASKS=', Arg) = 1) then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

procedure ApplyAgentTaskDefaults;
var
  I: Integer;
begin
  if AgentTaskDefaultsApplied or HasExplicitTaskCommandLine then
    Exit;

  // desktopicon is item 0. Set only the AI integration subtree so upgrade
  // installs preserve the user's unrelated task choices.
  for I := 1 to WizardForm.TasksList.Items.Count - 1 do
    WizardForm.TasksList.Checked[I] := True;

  AgentTaskDefaultsApplied := True;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    ApplyAgentTaskDefaults;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  // Silent installs do not visit wpSelectTasks. Apply defaults here unless the
  // caller supplied /TASKS or /MERGETASKS explicitly.
  if CurStep = ssInstall then
  begin
    ApplyAgentTaskDefaults;
    Exit;
  end;

  if CurStep <> ssPostInstall then
    Exit;

  SyncSkillJunction('agentskill\universal',
    UserHome + '\.config\agents\skills\winc2d', False);
  // The final True removes the normal directory installed by winC2D <= 4.3.0
  // when a user explicitly opts out of this legacy target during an upgrade.
  SyncSkillJunction('agentskill\legacyagents',
    UserHome + '\.agents\skills\winc2d', True);
  SyncSkillJunction('agentskill\codex',
    UserHome + '\.codex\skills\winc2d', False);
  SyncSkillJunction('agentskill\claudecode',
    UserHome + '\.claude\skills\winc2d', False);
  SyncSkillJunction('agentskill\antigravity',
    UserHome + '\.gemini\antigravity\skills\winc2d', False);
  SyncSkillJunction('agentskill\opencode',
    UserHome + '\.config\opencode\skills\winc2d', False);
  SyncSkillJunction('agentskill\openclaw',
    UserHome + '\.openclaw\skills\winc2d', False);
  SyncSkillJunction('agentskill\cursor',
    UserHome + '\.cursor\skills\winc2d', False);
  SyncSkillJunction('agentskill\copilot',
    UserHome + '\.copilot\skills\winc2d', False);
  SyncSkillJunction('agentskill\gemini',
    UserHome + '\.gemini\skills\winc2d', False);
  SyncSkillJunction('agentskill\kilo',
    UserHome + '\.kilocode\skills\winc2d', False);
  SyncSkillJunction('agentskill\cline',
    UserHome + '\.cline\skills\winc2d', False);
  SyncSkillJunction('agentskill\roo',
    UserHome + '\.roo\skills\winc2d', False);
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

// ── Uninstall cleanup: remove app data + skill directory ───────────────────

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  CleanDir: string;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Remove only junctions before their canonical target under {app} disappears.
    RemoveSkillLink(UserHome + '\.config\agents\skills\winc2d');
    RemoveSkillLink(UserHome + '\.agents\skills\winc2d');
    RemoveSkillLink(UserHome + '\.codex\skills\winc2d');
    RemoveSkillLink(UserHome + '\.claude\skills\winc2d');
    RemoveSkillLink(UserHome + '\.gemini\antigravity\skills\winc2d');
    RemoveSkillLink(UserHome + '\.config\opencode\skills\winc2d');
    RemoveSkillLink(UserHome + '\.openclaw\skills\winc2d');
    RemoveSkillLink(UserHome + '\.cursor\skills\winc2d');
    RemoveSkillLink(UserHome + '\.copilot\skills\winc2d');
    RemoveSkillLink(UserHome + '\.gemini\skills\winc2d');
    RemoveSkillLink(UserHome + '\.kilocode\skills\winc2d');
    RemoveSkillLink(UserHome + '\.cline\skills\winc2d');
    RemoveSkillLink(UserHome + '\.roo\skills\winc2d');

    // App runtime data (tasks, rollback, size cache)
    CleanDir := GetEnv('APPDATA') + '\winC2D';
    if DirExists(CleanDir) then
      DelTree(CleanDir, True, True, True);

    // App local data (logs)
    CleanDir := GetEnv('LOCALAPPDATA') + '\winC2D';
    if DirExists(CleanDir) then
      DelTree(CleanDir, True, True, True);
  end;
end;
