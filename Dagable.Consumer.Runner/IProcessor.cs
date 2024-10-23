using Dagable.Consumer.Runner.Models;
using Dagable.Core;

namespace Dagable.Consumer.Runner
{
    internal interface IProcessor
    {
        /// <summary>
        ///  generates a critical path task graph based on the specified settings.
        /// </summary>
        /// <param name="settings">An instance of <see cref="GraphSettings"/> that specifies the parameters for graph generation.</param>
        /// <returns>A task that represents the asynchronous operation, with a result of type <see cref="ICriticalPathTaskGraph"/> that represents the generated graph.</returns>
        ICriticalPathTaskGraph GenerateGraph(GraphSettings settings);
    }
}