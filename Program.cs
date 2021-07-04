using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using DistributedMonteCarloSimulation.SimulationDefinitions;
using DistributedMonteCarloSimulation.Simulation;

namespace DistributedMonteCarloSimulation
{
    public static class Program
    {

        public const int distributionPort = 54822;
        public const int collectionPort = 54823;

        private enum RunMode
        {
            Server,
            Client
        }

        private static readonly string[] serverModeArgumentOptions = new string[] { "server", "s", "0" };
        private static readonly string[] clientModeArgumentOptions = new string[] { "client", "c", "1" };

        private static void Main(string[] args)
        {

            //Establish client or server mode using first argument

            RunMode mode;
            string serverRemoteIP = null;

            if (args.Length < 1)
            {
                throw new Exception("Please provide an arguemnt to specify client- or server-mode");
            }
            else
            {

                if (serverModeArgumentOptions.Contains(args[0]))
                    mode = RunMode.Server;
                else if (clientModeArgumentOptions.Contains(args[0]))
                {

                    mode = RunMode.Client;

                    //Try to get server IP from arguments
                    if (args.Length >= 2)
                        serverRemoteIP = args[1];

                }
                else
                    throw new Exception("Invalid mode argument provided");

            }

            //Load simulation definition

            if (!TryGetSimulationDefinitionInput(out SimulationDefinition simulationDefinition))
            {
                return;
            }

            //Branch depending on mode

            switch (mode)
            {

                case RunMode.Server:
                    ServerProgram.Run(simulationDefinition);
                    break;

                case RunMode.Client:
                    ClientProgram.Run(simulationDefinition, serverRemoteIP);
                    break;

                default:
                    throw new Exception("Unknown RunMode");

            }

        }

        private static string selectedFileInputPath = null;

        /// <summary>
        /// Tries to get the user to select a file to use for a simulation definition.
        /// </summary>
        /// <param name="result">The loaded simulation definition</param>
        /// <returns>Whether the operation suceeded or not</returns>
        private static bool TryGetSimulationDefinitionInput(out SimulationDefinition result)
        {

            ThreadStart fileSelectionThreadStart = new ThreadStart(() => GetFilePathInput());
            Thread fileSelectionThread = new Thread(fileSelectionThreadStart);
            fileSelectionThread.SetApartmentState(ApartmentState.STA);
            fileSelectionThread.Start();
            fileSelectionThread.Join();

            string definitionFilePath = selectedFileInputPath;

            if (definitionFilePath == "" || definitionFilePath == null)
            {
                result = default;
                return false;
            }

            string definitionFileContents = File.ReadAllText(definitionFilePath);

            if (!SimulationDefinition.TryParse(definitionFileContents, out SimulationDefinition simulationDefinition, out string simDefParsingErrorMsg))
            {

                throw new Exception("Unable to parse simulation definition file:\n" + simDefParsingErrorMsg);

            }

            result = simulationDefinition;
            return true;

        }

        /// <summary>
        /// Uses the Windows file dialog to get the user to select a file. This should be called from an STA thread
        /// </summary>
        /// <returns>The path of the selected file</returns>
        private static void GetFilePathInput(string initialDirectory = "",
            string filter = "MoCSiDeF files (*.mocsidef)|*.mocsidef|All files (*.*)|*.*")
        {

            string filePath;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {

                openFileDialog.InitialDirectory = initialDirectory;
                openFileDialog.Filter = filter;

                DialogResult result = openFileDialog.ShowDialog();

                switch (result)
                {

                    case DialogResult.OK:
                        filePath = openFileDialog.FileName;
                        break;

                    case DialogResult.None:
                    case DialogResult.Cancel:
                    case DialogResult.Abort:
                    case DialogResult.Retry:
                    case DialogResult.Ignore:
                    default:
                        selectedFileInputPath = null;
                        return;

                }

            }

            selectedFileInputPath = filePath;

        }

    }
}
