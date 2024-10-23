using Dagable.Core;
using System.IO.Compression;
using System.Text.Json;

namespace Dagable.Consumer
{
    public static class CompressionServices
    {
        /// <summary>
        /// Compresses a collection of critical path task graphs into a compressed byte array.
        /// </summary>
        /// <param name="taskGraphs">An enumerable collection of <see cref="ICriticalPathTaskGraph"/> instances to be compressed.</param>
        /// <returns>A byte array containing the compressed data representing the serialized JSON of the task graphs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="taskGraphs"/> is null.</exception>
        /// <remarks>
        /// The method first serializes the provided task graphs to a JSON string using the <see cref="JsonSerializer"/>.
        /// It then converts the JSON string into a byte array using UTF-8 encoding.
        /// Finally, it compresses the byte array using GZip compression and returns the compressed byte array.
        /// </remarks>
        public static byte[] CompressBatch(IEnumerable<ICriticalPathTaskGraph> taskGraphs)
        {
            var input = JsonSerializer.SerializeToUtf8Bytes(taskGraphs);

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(input, 0, input.Length);
                }

                return memoryStream.ToArray();
            }
        }
    }
}
