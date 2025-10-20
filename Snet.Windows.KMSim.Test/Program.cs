using Snet.Windows.KMSim.core;

namespace Snet.Windows.KMSim.Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            KMSimCore core = new KMSimCore();

            await core.ToggleCaseAsync(true);

            Console.WriteLine(await core.GetToggleCaseStatusAsync());

            await core.MouseMoveAsync(100, 100);

            await core.MouseClickLeftAsync();

            await core.DelayAsync(1000);

            await core.KeyboardPressCommonKeyAsync("!@#$%^&*()");

            await core.ToggleCaseAsync(false);

            Console.WriteLine(await core.GetToggleCaseStatusAsync());
        }
    }
}
