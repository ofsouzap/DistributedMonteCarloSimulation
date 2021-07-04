using System;
using System.Collections.Generic;
using DistributedMonteCarloSimulation.ExpressionTrees;

namespace DistributedMonteCarloSimulation.SimulationDefinitions
{
    public class ExpressionVariable : SimulationVariable
    {

        public ExpressionTree expressionTree;

        public ExpressionVariable(string name,
            bool recorded,
            ExpressionTree expressionTree)
        {
            this.name = name;
            this.recorded = recorded;
            this.expressionTree = expressionTree;
        }

        public override double Evaluate(Dictionary<string, double> variableMappings,
            Random random)
            => expressionTree.Evaluate(variableMappings);

    }
}
