using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DistributedMonteCarloSimulation.SimulationDefinitions;

namespace DistributedMonteCarloSimulation
{
    public static class ServerProgram
    {

        /// <summary>
        /// The maximum number of results (arrays of doubles) that can be returned in one connection by a client
        /// </summary>
        public const byte maxReturnedResultCount = byte.MaxValue;

        /// <summary>
        /// The number of bytes to be received when determining how many results a client is returning.
        /// 1 is used as the value since a single byte should be used for this value resulting in a maximum returned result count of 255
        /// </summary>
        public const int responseResultCountByteLength = 1;

        /// <summary>
        /// How many bytes a double consists of
        /// </summary>
        public const int doubleByteSize = 8;

        public static void Run(SimulationDefinition simulationDefinition)
        {

            ThreadStart responseCollectionThreadStart = new ThreadStart(
                () => CollectResponses(simulationDefinition.trialCount,
                    simulationDefinition.variables.Where(x => x.recorded).Count(),
                    (double[][] r) => OnAllResponsesCollected(simulationDefinition, r)
                    )
                );
            Thread responseCollectionThread = new Thread(responseCollectionThreadStart);
            responseCollectionThread.Start();

            DistributeJobsToClients(simulationDefinition);

            responseCollectionThread.Join();

        }

        private static void OnAllResponsesCollected(SimulationDefinition simulationDefinition,
            double[][] results)
        {

            //Get names of each variable

            int resultsValueIndex = 0;

            Dictionary<string, double[]> recordedVariablesResults = new Dictionary<string, double[]>();

            foreach (SimulationVariable simVar in simulationDefinition.variables)
                if (simVar.recorded)
                {

                    double[] simVarValues = new double[results.Length];

                    //Take the nth value from each result array where n is the resultsValueIndex
                    for (int i = 0; i < results.Length; i++)
                        simVarValues[i] = results[i][resultsValueIndex];

                    recordedVariablesResults.Add(simVar.name, simVarValues);

                    resultsValueIndex++;

                }

            //Output results (to console and file)

            string outputFileContents = "";

            Console.WriteLine("Results collected. Outputting...");

            foreach (string varName in recordedVariablesResults.Keys)
            {

                ProcessedResultSet resultSet = new ProcessedResultSet(recordedVariablesResults[varName]);

                string outputLine = "\n";
                outputLine += $"{varName}:\n";
                outputLine += $"Mean\t\t{resultSet.mean}\n";
                outputLine += $"Std Dev.\t{resultSet.standardDeviation}\n";
                outputLine += $"Variance\t{resultSet.variance}\n";
                outputLine += $"Mode\t\t{resultSet.mode}\n";
                outputLine += $"Q1\t\t{resultSet.q1}\n";
                outputLine += $"Median\t\t{resultSet.median}\n";
                outputLine += $"Q3\t\t{resultSet.q3}\n";
                outputLine += $"IQR\t\t{resultSet.iqr}\n";

                Console.WriteLine(outputLine);
                outputFileContents += outputLine;

            }

            //Saving results to output file

            ThreadStart fileSelectionThreadStart = new ThreadStart(() => GetFilePathOutput());
            Thread fileSelectionThread = new Thread(fileSelectionThreadStart);
            fileSelectionThread.SetApartmentState(ApartmentState.STA);
            fileSelectionThread.Start();
            fileSelectionThread.Join();

            string outputFileName = selectedFileOutputPath;

            if (outputFileName != "" && outputFileName != null)
                File.WriteAllText(outputFileName, outputFileContents);

            Console.WriteLine("Press ENTER to continue...");
            Console.ReadLine();

        }

        private static string selectedFileOutputPath = null;

        /// <summary>
        /// Uses the Windows file dialog to get the user to select a file saving location and name. This should be called from an STA thread
        /// </summary>
        /// <returns>The path of the selected file</returns>
        private static void GetFilePathOutput(string initialDirectory = "",
            string filter = "Text file|*.txt")
        {

            string filePath;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {

                saveFileDialog.InitialDirectory = initialDirectory;
                saveFileDialog.Filter = filter;

                DialogResult result = saveFileDialog.ShowDialog();

                switch (result)
                {

                    case DialogResult.OK:
                        filePath = saveFileDialog.FileName;
                        break;

                    case DialogResult.None:
                    case DialogResult.Cancel:
                    case DialogResult.Abort:
                    case DialogResult.Retry:
                    case DialogResult.Ignore:
                    default:
                        selectedFileOutputPath = null;
                        return;

                }

            }

            selectedFileOutputPath = filePath;

        }

        public struct ProcessedResultSet
        {

            public ProcessedResultSet(double[] results)
            {

                this.results = results;

                sum = results.Sum(x => x);
                sqrSum = results.Sum(x => x * x);

                mean = sum / results.Length;

                q1 = results[results.Length / 4];
                median = results[results.Length / 2];
                q3 = results[(int)Math.Floor(results.Length * (double)(3/4))];

                iqr = q3 - q1;

                variance = Math.Abs((sqrSum / results.Length) - mean);
                standardDeviation = Math.Sqrt(variance);

                mode = results.OrderByDescending(x => results.Count(ele => ele == x)).ToArray()[0];

            }

            public readonly double[] results;
            public readonly double sum;
            public readonly double sqrSum;
            public readonly double mean;
            public readonly double q1;
            public readonly double median;
            public readonly double q3;
            public readonly double iqr;
            public readonly double standardDeviation;
            public readonly double variance;
            public readonly double mode;

        }

        private static void DistributeJobsToClients(SimulationDefinition simulationDefinition)
        {

            byte definitionChecksum = simulationDefinition.GenerateChecksum();

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Program.distributionPort);
            listener.Bind(endPoint);

            listener.Listen(16);

            uint jobIndex = 0;
            while (true)
            {

                Socket clientHandler = listener.Accept();

                //Verify checksum

                byte[] checksumBuffer = new byte[1];

                clientHandler.Receive(checksumBuffer, 1, SocketFlags.None);

                byte clientChecksum = checksumBuffer[0];

                if (clientChecksum == definitionChecksum)
                {
                    clientHandler.Send(BitConverter.GetBytes(true));
                }
                else
                {
                    clientHandler.Send(BitConverter.GetBytes(false));
                    clientHandler.Close();
                    continue;
                }

                //Send job index then increment

                clientHandler.Send(BitConverter.GetBytes(jobIndex));

                jobIndex++;

                //Close connection once complete

                clientHandler.Close();

                //Check if can stop loop

                if (jobIndex > simulationDefinition.trialCount / SimulationDefinition.clientJobSize)
                    break;

            }

        }

        /// <summary>
        /// Collects the responses from clients that have been processing
        /// </summary>
        /// <param name="targetCount">The number of responses to collect before stopping</param>
        /// <param name="singleResultsLength">How many values each returned result should contain</param>
        /// <param name="OnAllCollected">An action to execute when all the responses have been collected.
        /// A parameter is provided for the values of each recorded variable of the simulation in order of their calculation</param>
        private static void CollectResponses(uint targetCount,
            int singleResultsLength,
            Action<double[][]> OnAllCollected)
        {

            List<double[]> results = new List<double[]>();

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Program.collectionPort);
            listener.Bind(endPoint);

            listener.Listen(32);

            while (true)
            {

                Socket clientHandler = listener.Accept();

                byte[] resultCountBuffer = new byte[responseResultCountByteLength];
                clientHandler.Receive(resultCountBuffer, responseResultCountByteLength, SocketFlags.None);

                byte responseValuesCount = resultCountBuffer[0];

                List<double[]> clientResults = new List<double[]>();

                for (byte resultIndex = 0; resultIndex < responseValuesCount; resultIndex++)
                {

                    double[] singleResultsBuffer = new double[singleResultsLength];

                    byte[] responseValueBuffer = new byte[doubleByteSize];

                    for (int resultValueIndex = 0; resultValueIndex < singleResultsLength; resultValueIndex++)
                    {

                        clientHandler.Receive(responseValueBuffer, doubleByteSize, SocketFlags.None);
                        singleResultsBuffer[resultValueIndex] = BitConverter.ToDouble(responseValueBuffer, 0);

                    }

                    clientResults.Add(singleResultsBuffer);

                }

                clientHandler.Close();

                results.AddRange(clientResults);

                if ((ulong)results.Count == targetCount)
                    break;

            }

            OnAllCollected?.Invoke(results.ToArray());

        }

    }
}
