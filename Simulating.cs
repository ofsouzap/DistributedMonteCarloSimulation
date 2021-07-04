using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedMonteCarloSimulation.SimulationDefinitions;

namespace DistributedMonteCarloSimulation.Simulation
{
    public static class Simulating
    {

        /// <summary>
        /// Method to be called when a simulation is completed
        /// </summary>
        /// <param name="recordedVariableValues">A dictionary of all the recorded variables' values after having run the simulation calculations</param>
        public delegate void SimulationOutput(Dictionary<string, double> recordedVariableValues);

        /// <summary>
        /// Starts a simulation using the provided values as calls an action when the simulation is complete. Do not expect this to run synchronously.
        /// </summary>
        /// <param name="simulationDefinition">The simulation definition to simulate</param>
        /// <param name="seed">The random seed to use for the simulation</param>
        /// <param name="onComplete">The SimulationOutput method to call when the simulation is complete</param>
        public static async void StartSimulation(SimulationDefinition simulationDefinition,
            int seed,
            SimulationOutput onComplete)
        {

            Dictionary<string, double> results = RunSimulation(simulationDefinition, seed);

            onComplete?.Invoke(results);

            //TODO - problems occuring whilst multithreading. Once able to fix, use below

            //await Task.Run(() =>
            //{

            //    Dictionary<string, double> recordedVariableValues;

            //    recordedVariableValues = RunSimulation(simulationDefinition, seed);

            //    onComplete?.Invoke(recordedVariableValues);

            //});

        }

        /// <summary>
        /// Synchronously run a simulation
        /// </summary>
        /// <param name="simulationDefinition">The simulation definition to use</param>
        /// <param name="seed">The random seed to use</param>
        /// <returns>A dictionary of all the recorded variables' values after having run the simulation calculations</returns>
        private static Dictionary<string, double> RunSimulation(SimulationDefinition simulationDefinition,
            int seed)
        {

            //Set up values dictionary by initialising all values to 0

            Dictionary<string, double> values = new Dictionary<string, double>();

            foreach (SimulationVariable simVar in simulationDefinition.variables)
                values.Add(simVar.name, 0);

            //Create the random generator
            Random random = new Random(seed);

            //Run the simulation calculations

            foreach (SimulationVariable simVar in simulationDefinition.variables)
            {

                values[simVar.name] = simVar.Evaluate(values, random);

            }

            //Find values to be recorded

            Dictionary<string, double> output = new Dictionary<string, double>();

            foreach (SimulationVariable simVar in simulationDefinition.variables)
                if (simVar.recorded)
                    output.Add(simVar.name, values[simVar.name]);

            //Return output
            return output;

        }

    }
}
