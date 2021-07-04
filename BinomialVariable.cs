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

            //TODO
            throw new NotImplementedException();

        }

    }
}
