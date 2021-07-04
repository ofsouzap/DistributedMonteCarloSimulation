using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DistributedMonteCarloSimulation.SimulationDefinitions;
using DistributedMonteCarloSimulation.Simulation;

namespace DistributedMonteCarloSimulation
{
    public static class ClientProgram
    {

        public static void Run(SimulationDefinition simulationDefinition,
            string serverIPString = null)
        {

            //Set up server end point

            IPEndPoint distributionEndPoint, collectionEndPoint;

            IPAddress serverIPAddress;

            if (serverIPString == null)
                serverIPAddress = GetServerIPAddressInput();
            else
                serverIPAddress = IPAddress.Parse(serverIPString);

            distributionEndPoint = new IPEndPoint(serverIPAddress, Program.distributionPort);
            collectionEndPoint = new IPEndPoint(serverIPAddress, Program.collectionPort);

            //Get job index

            uint jobIndex = GetJobIndexFromServer(distributionEndPoint, simulationDefinition.GenerateChecksum());

            //Determine recorded variable compressed order

            int nextOrderIndex = 0;
            Dictionary<string, int> recordedVariableOrders = new Dictionary<string, int>();

            foreach (SimulationVariable simVar in simulationDefinition.variables)
                if (simVar.recorded)
                {
                    recordedVariableOrders.Add(simVar.name, nextOrderIndex);
                    nextOrderIndex++;
                }

            //Run simulations

            List<double[]> recordedResults = new List<double[]>();
            object recordedResultsListLock = new object();

            int[] seeds = simulationDefinition.GetSeedsFromJobIndex(jobIndex);

            bool resultsHaveBeenSentToServer = false;

            foreach (int seed in seeds)
                Simulating.StartSimulation(simulationDefinition, seed, results =>
                {
                    OnResultComplete(results,
                        recordedVariableOrders,
                        ref recordedResults,
                        ref recordedResultsListLock,
                        ref resultsHaveBeenSentToServer,
                        seeds.Length,
                        collectionEndPoint);
                });

        }

        private static void OnResultComplete(Dictionary<string, double> results,
            Dictionary<string, int> recordedVariableOrder,
            ref List<double[]> recordedResultsList,
            ref object recordedResultsListLock,
            ref bool resultsHaveBeenSentToServer,
            int targetResultCount,
            IPEndPoint serverEndPoint)
        {

            //Process the received results

            double[] resultsArray = ResultsDictionaryToResultArray(results, recordedVariableOrder);

            //Record the results
            //N.B. this method may be called in different threads

            lock (recordedResultsListLock)
            {

                recordedResultsList.Add(resultsArray);

                //Check if all results have now been collected

                if (recordedResultsList.Count >= targetResultCount)
                {
                    if (!resultsHaveBeenSentToServer)
                    {
                        ReturnResultsToServer(serverEndPoint, recordedResultsList.ToArray());
                        resultsHaveBeenSentToServer = true;
                    }
                }

            }


        }

        private static void ReturnResultsToServer(IPEndPoint serverEndPoint,
            double[][] results)
        {

            //N.B. only 255 results can be sent in one connection to the server

            if (results.Length <= ServerProgram.maxReturnedResultCount)
                ReturnSingleResultsBatchToServer(serverEndPoint, results);
            else
            {

                int resultArrayIndex = 0;

                do
                {

                    bool isFinalBatch = resultArrayIndex + ServerProgram.maxReturnedResultCount >= results.Length;

                    int batchLength = isFinalBatch
                        ? results.Length % ServerProgram.maxReturnedResultCount
                        : ServerProgram.maxReturnedResultCount;

                    double[][] resultArrayBatch = new double[batchLength][];

                    Array.Copy(results, resultArrayIndex, resultArrayBatch, 0, batchLength);

                    ReturnSingleResultsBatchToServer(serverEndPoint,
                        resultArrayBatch);

                    resultArrayIndex += ServerProgram.maxReturnedResultCount;

                }
                while (resultArrayIndex <= results.Length);
                
            }

        }
        
        private static void ReturnSingleResultsBatchToServer(IPEndPoint serverEndPoint,
            double[][] results)
        {

            if (results.Length > 255)
                throw new ArgumentException("Too many results provided to send in a single batch");

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(serverEndPoint);

            socket.Send(BitConverter.GetBytes((byte)results.Length));

            foreach (double[] resultArray in results)
                foreach (double value in resultArray)
                    socket.Send(BitConverter.GetBytes(value));

            socket.Close();

        }

        /// <summary>
        /// Converts a dictionary of recorded variables' values into an array based on a dictionary specifying the order which each variable should be placed referencing them by name
        /// </summary>
        private static double[] ResultsDictionaryToResultArray(Dictionary<string, double> results, Dictionary<string, int> recordedVariableOrder)
        {

            double[] output = new double[results.Count];

            foreach (string varName in results.Keys)
                if (recordedVariableOrder.ContainsKey(varName))
                    output[recordedVariableOrder[varName]] = results[varName];
                else
                    throw new ArgumentException("recordedVariableOrder doesn't contain value for results variable name - " + varName);

            return output;

        }

        private static IPAddress GetServerIPAddressInput()
        {

            IPAddress serverAddress;

            while (true)
            {

                Console.Write("Server IP> ");
                string ipInput = Console.ReadLine();

                try
                {
                    serverAddress = IPAddress.Parse(ipInput);
                    break;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid address format");
                }

            }

            return serverAddress;

        }

        private static uint GetJobIndexFromServer(IPEndPoint serverEndPoint,
            byte simulationDefinitionChecksum)
        {

            //Set up socket

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(serverEndPoint);

            //Server-client checksum comparison

            socket.Send(BitConverter.GetBytes(simulationDefinitionChecksum));

            byte[] successByteBuffer = new byte[1];

            socket.Receive(successByteBuffer, 1, SocketFlags.None);

            bool checksumMatch = BitConverter.ToBoolean(successByteBuffer, 0);

            if (!checksumMatch)
            {
                Console.WriteLine("Server simulation definition checksum inconsistent with provided simulation definition checksum.");
                Console.WriteLine("Exiting program...");
            }

            //Receiving job index

            byte[] jobIndexBytes = new byte[4];

            socket.Receive(jobIndexBytes, 4, SocketFlags.None);

            uint jobIndex = BitConverter.ToUInt32(jobIndexBytes, 0);

            //Close connection

            socket.Close();

            //Return value

            return jobIndex;

        }

    }
}
