namespace winC2D.Cli;

public enum CliExitCode
{
    Success = 0,
    BusinessFailure = 1,
    ArgumentError = 2,
    InsufficientPrivileges = 3,
    TaskNotFound = 4,
    UnhandledException = 5
}
