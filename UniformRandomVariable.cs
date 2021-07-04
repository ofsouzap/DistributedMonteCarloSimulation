using System;
using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.RandomVariables
{
    public class UniformRandomVariable : RandomVariable
    {

        public int lowerBound;
        public int upperBound;

        public UniformRandomVariable(string name, int lowerBound, int upperBound)
        {
            this.name = name;
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }

        public override double Evaluate(Dictionary<string, double> variableMappings, Random random)
        {

            return random.Next(lowerBound, upperBound + 1);

        }

    }
}
