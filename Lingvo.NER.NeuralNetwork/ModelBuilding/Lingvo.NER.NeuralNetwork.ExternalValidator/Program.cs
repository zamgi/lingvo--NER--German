using System;

using Newtonsoft.Json;

namespace Lingvo.NER.NeuralNetwork.ExternalValidator
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                var opts = OptionsExtensions.ReadInputOptions( args, "valid.json" );

                var result = Validator.Run_Validate( opts );

                var json = JsonConvert.SerializeObject( result, Formatting.None );
                PipeIPC.Client__out.Send( PipeIPC.PIPE_NAME_1, json, connectMillisecondsTimeout: 5_000 );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();

                //Console.ReadLine();
            }
            //Console.ReadLine();
        }
    }
}
