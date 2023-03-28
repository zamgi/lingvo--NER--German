using System;
using System.Threading.Tasks;

namespace Lingvo.NER.Rules.TestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            try
            {
                using var config = await Config.Inst.CreateNerProcessorConfig_AsyncEx().CAX();

                await TestRunner_1.Run( config ).CAX();
                await TestRunner_2.Run( config ).CAX();
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "\r\n\r\n[.......finita.......]" );
        }
    }
}
