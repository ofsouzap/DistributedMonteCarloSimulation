namespace DistributedMonteCarloSimulation.SimulationDefinitions
{
    public partial struct SimulationDefinition
    {

        /// <summary>
        /// The size of a single job sent to a client
        /// </summary>
        public const uint clientJobSize = 1024;

        public uint trialCount;

        /// <summary>
        /// The variables that constitute the simulation.
        /// The order of the array is the order in which the variables' values should be evaluated
        /// </summary>
        public SimulationVariable[] variables;

        /// <summary>
        /// Generates a value that can be used to compare the equality of two simulation definitions. Comparisons won't be fully accurate but should be effective
        /// </summary>
        public byte GenerateChecksum()
        {

            //N.B. values changing due to casting isn't a problem as this is only a checksum value

            byte value = 0;

            value += (byte)trialCount;

            foreach (SimulationVariable simVar in variables)
            {

                foreach (char c in simVar.name)
                    value += (byte)c;

                if (simVar.recorded)
                    value += 1;

            }

            return value;

        }

        public int[] GetSeedsFromJobIndex(uint index)
        {

            int minSeed = (int)(clientJobSize * index);

            int maxSeed;

            //If this is the final job index (and so won't have clientJobSize seeds but will have less) then set max seed accordingly
            if (trialCount < clientJobSize
                || (trialCount - clientJobSize) / (double)clientJobSize < index)
            {
                maxSeed = (int)trialCount - 1;
            }
            else
            {
                maxSeed = (int)(clientJobSize * (index + 1)) - 1;
            }

            int outputIndex = 0;
            int[] output = new int[maxSeed - minSeed + 1];

            for (int i = minSeed; i <= maxSeed; i++)
            {
                output[outputIndex] = i;
                outputIndex++;
            }

            return output;

        }

    }
}
