﻿using System;

using Lingvo.NER.NeuralNetwork.Utils;

namespace Lingvo.NER.NeuralNetwork
{
    public class DecayLearningRate : ILearningRate
    {
        private readonly float m_startLearningRate = 0.001f;
        private int m_weightsUpdateCount = 0;
        private readonly int m_warmupSteps = 8000;

        public DecayLearningRate( float startLearningRate, int warmupSteps, int weightsUpdatesCount )
        {
            Logger.WriteLine( $"Creating decay learning rate. StartLearningRate = '{startLearningRate}', WarmupSteps = '{warmupSteps}', WeightsUpdatesCount = '{weightsUpdatesCount}'" );
            m_startLearningRate = startLearningRate;
            m_warmupSteps = warmupSteps;
            m_weightsUpdateCount = weightsUpdatesCount;
        }

        public float GetCurrentLearningRate()
        {
            m_weightsUpdateCount++;
            float lr = m_startLearningRate * (float) (Math.Min( Math.Pow( m_weightsUpdateCount, -0.5 ), Math.Pow( m_warmupSteps, -1.5 ) * m_weightsUpdateCount ) / Math.Pow( m_warmupSteps, -0.5 ));
            return lr;
        }
    }
}
