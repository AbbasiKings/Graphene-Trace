namespace GrapheneTrace.Core.Constants;

public static class AppConstants
{
    public const string AppName = "Graphene Trace";

    // Security
    public const string DefaultJwtSecret = "graphene-trace-default-secret-change-me";
    public const int JwtExpiryDays = 7;
    public const int PasswordSaltSize = 16;
    public const int PasswordKeySize = 32;
    public const int PasswordIterations = 100_000;

    // Analysis defaults
    public const int CsvMatrixSize = 32;
    public const double PressureZeroForceValue = 5;
    public const double PressureHighThreshold = 60;
    public const double PressureCriticalThreshold = 75;
    public const int MinPixelAreaForAlert = 10;
}
