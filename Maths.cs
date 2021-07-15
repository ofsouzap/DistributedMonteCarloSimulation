using System.Linq;

namespace DistributedMonteCarloSimulation
{
    public static class Maths
    {

        public static uint Combinations(uint n, uint r)
        {

            return (Factorial(n)) / (Factorial(r) * Factorial(n - r));

        }

        public static uint Factorial(uint x)
        {

            if (x == 0)
                return 1;
            else if (x == 1)
                return 1;
            else
                return x * Factorial(x - 1);

        }

    }
}
