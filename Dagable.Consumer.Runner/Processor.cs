using Dagable.Consumer.Models;
using Dagable.Core;

namespace Dagable.Consumer
{
    public class Processor : IProcessor
    {
        private readonly IDagCreationService _service;

        public Processor(IDagCreationService dagCreationService)
        {
            _service = dagCreationService;
        }

        ///<inheritdoc cref="IProcessor.GenerateGraph(GraphSettings)"/>
        public ICriticalPathTaskGraph GenerateGraph(GraphSettings settings)
        {
            var nodes = GenerateRandomValue(settings.MinNodes, settings.MaxNodes);
            var layers = GenerateRandomValue(settings.MinLayer, settings.MaxLayer);
            var probability = GenerateRandomValue(0.01, 1);
            return _service.GenerateCriticalPathTaskGraph(layers, nodes, probability);
        }

        /// <summary>
        /// Generates a random integer value between the specified minimum and maximum values, inclusive.
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random number to be generated.</param>
        /// <param name="max">The inclusive upper bound of the random number to be generated.</param>
        /// <returns>A random integer between <paramref name="min"/> and <paramref name="max"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        private static int GenerateRandomValue(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max + 1);
        }

        /// <summary>
        /// Generates a random double value between the specified minimum and maximum values, rounded to two decimal places.
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random number to be generated.</param>
        /// <param name="max">The inclusive upper bound of the random number to be generated.</param>
        /// <returns>A random double between <paramref name="min"/> and <paramref name="max"/> rounded to two decimal places.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
        private static double GenerateRandomValue(double min, double max)
        {
            var random = new Random();
            double randomDouble = random.NextDouble();

            double randomDecimal = Math.Round(randomDouble, 2);

            return randomDecimal;
        }
    }
}
