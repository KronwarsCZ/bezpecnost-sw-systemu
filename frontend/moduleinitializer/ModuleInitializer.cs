using System.Runtime.CompilerServices;

namespace frontend.moduleinitializer;

public static class MyModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
}
