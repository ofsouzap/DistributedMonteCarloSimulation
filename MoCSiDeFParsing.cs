using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DistributedMonteCarloSimulation.ExpressionTrees;
using DistributedMonteCarloSimulation.RandomVariables;

namespace DistributedMonteCarloSimulation.SimulationDefinitions
{
    public partial struct SimulationDefinition
    {

        #region Regex Definitions

        //Regex for real number: [0-9]+(\\.[0-9]+)?
        //Regex for a term (variable or constant): ([0-9]+|[A-z]+)
        //Regex set for operators: [\\+\\*/\\-%]

        private static readonly Regex trialsDefinitionLineRegex = new Regex("^trials=[0-9]+$");

        private static readonly Regex randomVariableDefinitionLineRegex = new Regex("^random [A-z]+=[A-z]+\\(-?([0-9]+(\\.[0-9]+)?)(,-?([0-9]+(\\.[0-9]+)?))*\\)$");

        private static readonly Regex calculatedVariableDefinitionLineRegex = new Regex("^(record )?var [A-z]+=([0-9]+|[A-z]+)([\\+\\*\\/\\-%]([0-9]+|[A-z]+))*$");

        #endregion

        /// <summary>
        /// Parses a string into a SimulationDefinition according to the MoCSiDeF file specification (should be found in same directory as source code)
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <param name="result">The parsed result</param>
        /// <param name="errorMessage">An error message if the string fails to parse</param>
        /// <returns>Whether the string was successfully parsed</returns>
        public static bool TryParse(string s,
            out SimulationDefinition result,
            out string errorMessage)
        {

            //Removing uncessary characters
            s = s.Replace("\r","");

            string[] lines = s.Split('\n');

            //The values that will be read into as the string is parsed
            uint trialCount = 0;
            List<SimulationVariable> variables = new List<SimulationVariable>();

            //Checks to ensure the file is syntactically-correct
            bool trialCountDefined = false;
            List<string> declaredVariables = new List<string>();

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {

                string line = lines[lineIndex];

                if (line
                    .Replace(" ","")
                    .Replace("\t","")
                    == "") //If the line is an empty line
                {

                    continue;

                }
                else if (line[0] == '#') //Comment lines
                {

                    continue;

                }
                else if (trialsDefinitionLineRegex.IsMatch(line)) //Setting the number of trials for the simulation
                {

                    if (!trialCountDefined) //Beware of the number of trials being already-defined
                    {

                        string trialCountString = line.Split('=')[1];

                        if (uint.TryParse(trialCountString, out trialCount))
                        {

                            trialCountDefined = true;
                            continue;

                        }
                        else
                        {
                            result = default;
                            errorMessage = "Failed to parse trial count (" + trialCountString + ")";
                            return false;
                        }

                    }
                    else
                    {
                        result = default;
                        errorMessage = "Multiple trial count definition lines detected";
                        return false;
                    }

                }
                else if (randomVariableDefinitionLineRegex.IsMatch(line)) //Defining a random variable
                {

                    string definitionString = line.Split(' ')[1];
                    string[] definitionParts = definitionString.Split('=');

                    string name = definitionParts[0];
                    string distribution = definitionParts[1];

                    //Check if variable name already taken
                    if (declaredVariables.Contains(name))
                    {
                        errorMessage = "Variable name used twice (" + name + ")";
                        result = default;
                        return false;
                    }

                    if (!TryParseVariableDistribution(distribution, name, out RandomVariable randomVariable, out string parsingErrorMessage))
                    {

                        errorMessage = "Error occured when parsing distribution for " + name + ":\n" + parsingErrorMessage;
                        result = default;
                        return false;

                    }

                    variables.Add(randomVariable);
                    declaredVariables.Add(name);

                    continue;

                }
                else if (calculatedVariableDefinitionLineRegex.IsMatch(line)) //Defining a calculated variable
                {

                    string name;
                    bool recorded;
                    ExpressionTree expression;

                    string[] parts = line.Split(' ');

                    string definitionPart;
                    string namePart, expressionPart;

                    if (parts.Length == 3 && parts[0] == "record")
                    {
                        recorded = true;
                        definitionPart = parts[2];
                    }
                    else
                    {
                        recorded = false;
                        definitionPart = parts[1];
                    }

                    string[] definitionParts = definitionPart.Split('=');
                    namePart = definitionParts[0];
                    expressionPart = definitionParts[1];

                    //Check if variable name already taken
                    if (declaredVariables.Contains(namePart))
                    {
                        errorMessage = "Variable name used twice (" + namePart + ")";
                        result = default;
                        return false;
                    }
                    else
                    {
                        name = namePart;
                    }

                    if (!ExpressionTree.TryParse(expressionPart,
                        declaredVariables.ToArray(),
                        out expression,
                        out string parsingErrorMessage))
                    {

                        errorMessage = "Error occured when parsing expression for " + name + ":\n" + parsingErrorMessage;
                        result = default;
                        return false;

                    }

                    ExpressionVariable variable = new ExpressionVariable(name,
                        recorded,
                        expression);

                    variables.Add(variable);
                    declaredVariables.Add(name);

                    continue;

                }
                else
                {

                    errorMessage = "Invalid line found (line " + (lineIndex + 1).ToString() + ")";
                    result = default;
                    return false;

                }

            }

            //Check that trial count was defined
            if (!trialCountDefined)
            {
                errorMessage = "Trial count never defined";
                result = default;
                return false;
            }

            errorMessage = "";
            result = new SimulationDefinition()
            {
                trialCount = trialCount,
                variables = variables.ToArray()
            };
            return true;

        }

        /// <summary>
        /// Parses a definition for a random variable distribution into an instance of a subclass of RandomVariable
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <param name="variableName">The name of the variable</param>
        /// <param name="result">The RandomVariable subclass instance generated from the definition</param>
        /// <param name="errorMessage">The error message produced if the method fails</param>
        /// <returns>Whether the string was successfully parsed</returns>
        private static bool TryParseVariableDistribution(string s,
            string variableName,
            out RandomVariable result,
            out string errorMessage)
        {

            //Ensuring correct number of brackets

            if (s.Count(x => x == ')') > 1)
            {
                errorMessage = "Too many ')' characters found";
                result = default;
                return false;
            }

            if (s.Count(x => x == '(') > 1)
            {
                errorMessage = "Too many ')' characters found";
                result = default;
                return false;
            }

            //Remove final ')'
            s = s.Replace(")", "");

            string[] parts = s.Split('(');

            string distributionName = parts[0];

            //Takes the arguments for the distribution having removed the brackets
            string[] argumentStrings = parts[1].Split(',');
            double[] arguments = argumentStrings.Select(x => double.Parse(x)).ToArray();

            switch (distributionName)
            {

                case "Range":

                    //Ensure 2 arguments
                    if (arguments.Length != 2)
                    {
                        errorMessage = "Invalid number of arguments for uniform random distribution";
                        result = default;
                        return false;
                    }

                    //Parse bounds to integer values

                    int lowerBound, upperBound;

                    try
                    {
                        lowerBound = Convert.ToInt32(arguments[0]);
                        upperBound = Convert.ToInt32(arguments[1]);
                    }
                    catch (FormatException)
                    {
                        errorMessage = "Invalid bound value provided";
                        result = default;
                        return false;
                    }

                    //Return result

                    result = new UniformRandomVariable(variableName, lowerBound, upperBound);
                    errorMessage = "";
                    return true;

                case "B":

                    //Ensure 2 arguments
                    
                    if (arguments.Length != 2)
                    {
                        errorMessage = "Invalid number of arguments for binomial distribution";
                        result = default;
                        return false;
                    }

                    //Parse arguments

                    uint trials;
                    double successProbability;

                    try
                    {
                        trials = Convert.ToUInt32(arguments[0]);
                    }
                    catch (FormatException)
                    {
                        errorMessage = "Invalid trial count provided";
                        result = default;
                        return false;
                    }

                    successProbability = arguments[1];

                    if (successProbability < 0 || successProbability > 1)
                    {
                        errorMessage = "Success probability isn't in range [0.0,1.0]";
                        result = default;
                        return false;
                    }

                    //Return result

                    result = new BinomialVariable(variableName, trials, successProbability);
                    errorMessage = "";
                    return true;

                default:
                    errorMessage = "Unknown distribution name - " + distributionName;
                    result = default;
                    return false;

            }

        }

    }
}
