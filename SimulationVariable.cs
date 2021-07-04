using System;
using System.Collections.Generic;

namespace DistributedMonteCarloSimulation.SimulationDefinitions
{
    public abstract class SimulationVariable
    {

        /// <summary>
        /// The name of this variable
        /// </summary>
        public string name;

        /// <summary>
        /// Whether the value of this variable should be recorded as an output of the simulation
        /// </summary>
        public bool recorded;

        /// <summary>
        /// Evaluates the value of this variable
        /// </summary>
        /// <param name="variableMappings">The dictionary object holding the current variable mapping</param>
        /// <returns>The calculated value of the variable</returns>
        public abstract double Evaluate(Dictionary<string, double> variableMappings,
            Random random);

    }
}
