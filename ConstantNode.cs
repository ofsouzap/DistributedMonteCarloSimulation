using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.ExpressionTrees
{
    public class ConstantNode : ValueNode
    {

        public double value;

        public ConstantNode(double value)
        {
            this.value = value;
        }

        public override double Evaluate(Dictionary<string, double> variableMapping)
        {
            return value;
        }

    }
}
