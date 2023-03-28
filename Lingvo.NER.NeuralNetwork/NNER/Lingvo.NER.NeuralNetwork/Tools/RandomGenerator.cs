using System;

namespace Lingvo.NER.NeuralNetwork.Tools
{
    /// <summary>
    /// 
    /// </summary>
    public static class RandomGenerator
    {
        public static bool Return_V { get; set; }
        public static float V_Val { get; set; }

        private static readonly Random _Rnd = new Random( DateTime.Now.Millisecond );
        public static float GaussRandom()
        {
            if ( Return_V )
            {
                Return_V = false;
                return V_Val;
            }
            double u = 2 * _Rnd.NextDouble() - 1;
            double v = 2 * _Rnd.NextDouble() - 1;
            double r = (u * u) + (v * v);

            if ( r == 0 || r > 1 )
            {
                return GaussRandom();
            }

            double c = Math.Sqrt( -2 * Math.Log( r ) / r );
            V_Val = (float) (v * c);
            Return_V = true;
            return (float) (u * c);
        }

        public static float FloatRandom( float a, float b ) => (float) (_Rnd.NextDouble() * (b - a) + a);
        public static float IntegarRandom( float a, float b ) => (float) (Math.Floor( _Rnd.NextDouble() * (b - a) + a ));
        public static float NormalRandom( float mu, float std ) => mu + FloatRandom( -1.0f, 1.0f ) * std;
    }
}
