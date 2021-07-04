using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.ExpressionTrees
{
    public class SubtractionNode : ExpressionTree
    {

        public ExpressionTree leftOperand;
        public ExpressionTree rightOperand;

        public SubtractionNode(ExpressionTree leftOperand, ExpressionTree rightOperand)
        {
            this.leftOperand = leftOperand;
            this.rightOperand = rightOperand;
        }

        public override double Evaluate(Dictionary<string, double> variableMapping)
        {
            return leftOperand.Evaluate(variableMapping) - rightOperand.Evaluate(variableMapping);
        }

    }
}
