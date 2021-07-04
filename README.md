# DistributedMonteCarloSimulation
A program to perform Monte Carlo simulations by distriuting the tasks between other machines running the program. Requires Windows due to the usage of the Windows file dialog  
  
N.B. MoCSiDeF file should be distributed between machines prior to running the program  
  
To run as server (once compiled):  
1. Run program with "server" as first argument (e.g. "DistributedMonteCarloSimulation.exe server")  
2. Select intended MoCSiDeF file once prompted  
3. Await connections and returned results  
4. Once all results have been returned, the results will be output to the console and you will also be prompted to provide a location to save the output (in the same format as in the console)  
  
To run as client (once compiled):  
1. Run program with "client" as first argument (e.g. "DistributedMonteCarloSimulation.exe client")  
2. Optionally, provide server IP address as second argument (e.g. "DistributedMonteCarloSimulation.exe server 192.168.1.128")  
3. Select intended MoCSiDeF file once prompted  
4. If server IP address wasn't provided as second argument, you will be prompted to manually enter it now  
5. Your machine will now perform the communications and calculations without any further action required  
