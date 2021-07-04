# DistributedMonteCarloSimulation
A program to perform Monte Carlo simulations by distriuting the tasks between other machines running the program. Requires Windows due to the usage of the Windows file dialog

N.B. MoCSiDeF file should be distributed between machines prior to running the program

To run as server (once compiled):
  Run program with "server" as first argument (e.g. "DistributedMonteCarloSimulation.exe server")
  Select intended MoCSiDeF file once prompted
  Await connections and returned results
  Once all results have been returned, the results will be output to the console and you will also be prompted to provide a location to save the output (in the same format as in the console)

To run as client (once compiled):
  Run program with "client" as first argument (e.g. "DistributedMonteCarloSimulation.exe client")
  Optionally, provide server IP address as second argument (e.g. "DistributedMonteCarloSimulation.exe server 192.168.1.128")
    If server IP address isn't provided as second argument, you will be prompted to manually enter it later
  Select intended MoCSiDeF file once prompted
  Your machine will now perform the communications and calculations without any further action required
