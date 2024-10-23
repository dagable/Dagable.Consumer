using Dagable.Consumer.DataAccess.Repositories;
using Dagable.Consumer.Domain.Entities;
using Dagable.Consumer.Models;
using Dagable.Core;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Dagable.Consumer
{
    internal class Runner
    {
        private readonly IProcessor _processor;
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly AppOptions _options;

        public Runner(IProcessor processor, IRepository<Job> jobRepository, IRepository<Batch> batchRepository, IOptions<AppOptions> options)
        {
            _jobRepository = jobRepository;
            _processor = processor;
            _options = options.Value;
            _batchRepository = batchRepository;
        }

        public void Run(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.RabbitMq.HostName,
                UserName = _options.RabbitMq.Username,
                Password = _options.RabbitMq.Password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "task_queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var requestObject = JsonSerializer.Deserialize<JobRequest>(message);

                await ProcessJob(requestObject);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            channel.BasicConsume(queue: "task_queue",
                                 autoAck: false,
                                 consumer: consumer);

            Console.ReadLine();
        }

        /// <summary>
        /// Processes the job that has been taken from the queue.
        /// </summary>
        /// <param name="jobRequest">The settings and requirements for the job</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ProcessJob(JobRequest jobRequest)
        {
            var job = await CreateJob(jobRequest);
            await GenerateGraphsInBatches(jobRequest, job);
        }

        /// <summary>
        /// Creates a new job and inserts it into the repository.
        /// </summary>
        /// <param name="jobRequest">The job request containing necessary information.</param>
        /// <returns>A task representing the asynchronous operation, with the created <see cref="Job"/>.</returns>
        private async Task<Job> CreateJob(JobRequest jobRequest)
        {
            var savedJob = await _jobRepository.Insert(new Job(jobRequest.RequestGuid, jobRequest.UserGuid, jobRequest.GraphCount));
            return savedJob;
        }

        /// <summary>
        /// Generates graphs in batches and updates the job status accordingly.
        /// </summary>
        /// <param name="jobRequest">The job request containing settings for graph generation.</param>
        /// <param name="job">The job instance to be updated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task GenerateGraphsInBatches(JobRequest jobRequest, Job job)
        {
            var totalTasks = jobRequest.GraphCount;
            List<Task> tasks = new List<Task>();
            List<ICriticalPathTaskGraph> results = new List<ICriticalPathTaskGraph>();

            for (int i = 0; i < totalTasks; i++)
            {
                tasks.Add(Task.Run(() => GenerateGraphAsync(jobRequest, results, i)));

                if ((i + 1) % _options.BatchSize == 0)
                {
                    await ProcessBatch(tasks, results, job, i);
                }
            }

            await ProcessBatch(tasks, results, job, totalTasks - 1);
        }

        /// <summary>
        /// Generates a graph asynchronously and adds it to the results list.
        /// </summary>
        /// <param name="jobRequest">The job request containing settings for graph generation.</param>
        /// <param name="results">The list to store generated graphs.</param>
        /// <param name="taskId">The identifier for the task being processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private void GenerateGraphAsync(JobRequest jobRequest, List<ICriticalPathTaskGraph> results, int taskId)
        {
            var result =  _processor.GenerateGraph(jobRequest.GraphSettings);
            lock (results) 
            {
                results.Add(result);
            }
        }

        /// <summary>
        /// Processes a batch of tasks, updates the job status, and inserts the batch into the repository.
        /// </summary>
        /// <param name="tasks">The list of tasks to be awaited.</param>
        /// <param name="results">The list of generated graphs.</param>
        /// <param name="job">The job instance to be updated.</param>
        /// <param name="currentIndex">The current index of the task batch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessBatch(List<Task> tasks, List<ICriticalPathTaskGraph> results, Job job, int currentIndex)
        {
            await Task.WhenAll(tasks);
            job.CompletedGraphs = currentIndex + 1;

            var compressedData = CompressionServices.CompressBatch(results);
            var batch = new Batch((currentIndex + 1) / _options.BatchSize, compressedData, job.Id);

            await _batchRepository.Insert(batch);
            await _jobRepository.Update(job);

            tasks.Clear();
            results.Clear();
        }
    }
}


