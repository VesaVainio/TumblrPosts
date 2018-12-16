using QueueInterface;
using TableInterface;

namespace Functions
{
    public static class Startup
    {
        static Startup()
        {
            FunctionUtilities.ConfigureBindingRedirects();

            TableInterfaceStartup.Init();

            QueueInterfaceStartup.Init();
        }

        public static void Init() {
            FunctionUtilities.ConfigureBindingRedirects();
        }
    }
}
