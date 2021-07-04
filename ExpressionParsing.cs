using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DistributedMonteCarloSimulation.ExpressionTrees
{
    public abstract partial class ExpressionTree
    {

        public static readonly Regex alphabeticRegex = new Regex("^[A-z]+$");
        public static readonly Regex numericalRegex = new Regex("^\\-?[0-9]+(\\.[0-9]+)?$");

        private static readonly char[] parsableOperators = new char[] { '+', '-', '*', '/', '%' };

        /// <summary>
        /// Parses a string into an ExpressionTree. Operations are ALWAYS evaluated in the order they appear and so operator precedence and brackets don't affect the operation order
        /// To use operators in different orders, define new variables
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <param name="definedVariableNames">An array of the names of all variables that are already defined</param>
        public static bool TryParse(string s,
            string[] validVariableNames,
            out ExpressionTree result,
            out string errorMessage)
        {

            string currentTerm = "";

            ExpressionTree prevNode = null;
            char? currentOperator = null;

            //The string is read in reverse order so that the expression tree can be built in the correct order
            foreach (char c in s)
            {

                if (parsableOperators.Contains(c))
                {

                    if (prevNode == null) //If this is the first term then create a ValueNode for it
                    {

                        if (!TryParseExpressionTerm(currentTerm, validVariableNames, out ValueNode valueNode, out string initialParsingErrorMessage))
                        {

                            errorMessage = "Error occured when parsing expression term:\n" + initialParsingErrorMessage;
                            result = default;
                            return false;

                        }
                        else
                        {

                            prevNode = valueNode;
                            currentTerm = "";
                            currentOperator = c;

                        }

                    }
                    else
                    {

                        if (!TryParseExpressionTerm(currentTerm, validVariableNames, out ValueNode newNode, out string parsingErrorMessage))
                        {

                            errorMessage = "Error occured when parsing expression term:\n" + parsingErrorMessage;
                            result = default;
                            return false;

                        }
                        else
                        {

                            prevNode = GenerateExpressionTreePartFromOperatorAndNodes(prevNode, newNode, (char)currentOperator);
                            currentOperator = c;
                            currentTerm = "";

                        }

                    }

                }
                else
                {

                    currentTerm += c;

                }

            }

            if (!TryParseExpressionTerm(currentTerm, validVariableNames, out ValueNode finalNode, out string termParsingErrorMessage))
            {

                errorMessage = "Error occured when parsing expression term:\n" + termParsingErrorMessage;
                result = default;
                return false;

            }

            if (currentOperator != null)
                result = GenerateExpressionTreePartFromOperatorAndNodes(prevNode, finalNode, (char)currentOperator);
            else //If no operators are used in the expression
                result = finalNode;

            errorMessage = "";
            return true;

        }

        private static ExpressionTree GenerateExpressionTreePartFromOperatorAndNodes(ExpressionTree leftNode,
            ExpressionTree rightNode,
            char operatorChar)
        {

            switch (operatorChar)
            {

                case '+':
                    return new SumNode(leftNode, rightNode);

                case '-':
                    return new SubtractionNode(leftNode, rightNode);

                case '*':
                    return new ProductNode(leftNode, rightNode);

                case '/':
                    return new QuotientNode(leftNode, rightNode);

                case '%':
                    return new ModuloNode(leftNode, rightNode);

                default:
                    throw new Exception("Unknown currentOperator");

            }

        }

        /// <summary>
        /// Parses either a constant or variable-name term for an expression tree
        /// </summary>
        private static bool TryParseExpressionTerm(string s,
            string[] validVariableNames,
            out ValueNode result,
            out string errorMessage)
        {

            if (alphabeticRegex.IsMatch(s)) //Variable term (specified as the variable name)
            {

                if (validVariableNames.Contains(s))
                {
                    result = new VariableNode(s);
                    errorMessage = "";
                    return true;
                }
                else
                {

                    errorMessage = "Use of undefined variable name (" + s + ")";
                    result = default;
                    return false;

                }

            }
            else if (numericalRegex.IsMatch(s)) //Constant term
            {

                if (double.TryParse(s, out double value))
                {

                    result = new ConstantNode(value);
                    errorMessage = "";
                    return true;

                }
                else
                {
                    result = default;
                    errorMessage = "Invalid number provided";
                    return false;
                }

            }
            else
            {
                errorMessage = "Invalid term provided";
                result = default;
                return false;
            }

        }

    }
}
