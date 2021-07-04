using System;
using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.ExpressionTrees
{
    public abstract partial class ExpressionTree
    {

        public abstract double Evaluate(Dictionary<string, double> variableMapping);

    }
}
