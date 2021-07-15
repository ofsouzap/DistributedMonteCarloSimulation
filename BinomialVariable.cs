using System;
using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.RandomVariables
{
    public class BinomialVariable : RandomVariable
    {

        public uint trials;
        public double successProbability;

        public BinomialVariable(string name, uint trials, double successProbability)
        {
            this.name = name;
            this.trials = trials;
            this.successProbability = successProbability;
        }

        public override double Evaluate(Dictionary<string, double> variableMappings, Random random)
        {

            double probabilityTotal = 0;
            double r = random.NextDouble();

            for (uint i = 0; i < trials; i++)
            {

                probabilityTotal += ProbabilityOfResultValue(i);

                if (r <= probabilityTotal)
                    return i;

            }

            return trials;

        }

        public double ProbabilityOfResultValue(uint value)
        {

            return Maths.Combinations(trials, value)
                * Math.Pow(successProbability, value)
                * Math.Pow(1 - successProbability, trials - value);

        }

    }
}
