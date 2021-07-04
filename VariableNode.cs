using System;
using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.ExpressionTrees
{
    public class VariableNode : ValueNode
    {

        public string name;

        public VariableNode(string name)
        {
            this.name = name;
        }

        public override double Evaluate(Dictionary<string, double> variableMapping)
        {
            if (variableMapping.ContainsKey(name))
                return variableMapping[name];
            else
                throw new ArgumentException("Provided variable mapping doesn't contain mapping for variable node's name (" + name + ")");
        }

    }
}
