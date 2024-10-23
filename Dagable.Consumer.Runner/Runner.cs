using System.Collections.Concurrent;
using Dagable.Consumer.DataAccess.Repositories;
using Dagable.Consumer.Domain.Entities;
using Dagable.Consumer.Runner.Models;
using Dagable.Core;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace Dagable.Consumer.Runner
{
    internal class Runner : BackgroundService
    {
        private const string QueueName = "task_queue";

        private readonly IProcessor _processor;
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<Batch> _batchRepository;
        private readonly AppOptions _options;

        private IConnection _connection;
        private IModel _channel;

        public Runner(IProcessor processor, IRepository<Job> jobRepository, IRepository<Batch> batchRepository,
            IOptions<AppOptions> options)
        {
            _jobRepository = jobRepository;
            _processor = processor;
            _options = options.Value;
            _batchRepository = batchRepository;
            Init();
        }

        private void Init()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.RabbitMq.HostName,
                UserName = _options.RabbitMq.Username,
                Password = _options.RabbitMq.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var requestObject = JsonSerializer.Deserialize<JobRequest>(message);

                Console.WriteLine(
                    $"[*] Message received: {requestObject.GraphCount} graphs for user {requestObject.UserGuid}");

                await ProcessJob(requestObject);

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            _channel.BasicConsume(queue: QueueName,
                autoAck: false,
                consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Processes the job that has been taken from the queue.
        /// </summary>
        /// <param name="jobRequest">The settings and requirements for the job</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ProcessJob(JobRequest jobRequest)
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
            var savedJob =
                await _jobRepository.Insert(new Job(jobRequest.RequestGuid, jobRequest.UserGuid,
                    jobRequest.GraphCount));
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
            var tasks = new List<Task>();
            var results = new ConcurrentBag<ICriticalPathTaskGraph>();

            for (var i = 0; i < totalTasks; i++)
            {
                tasks.Add(Task.Run(() => GenerateGraphAsync(jobRequest, results)));

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
        /// <returns>A task representing the asynchronous operation.</returns>
        private void GenerateGraphAsync(JobRequest jobRequest, ConcurrentBag<ICriticalPathTaskGraph> results)
        {
            var result = _processor.GenerateGraph(jobRequest.GraphSettings);
            results.Add(result);
        }

        /// <summary>
        /// Processes a batch of tasks, updates the job status, and inserts the batch into the repository.
        /// </summary>
        /// <param name="tasks">The list of tasks to be awaited.</param>
        /// <param name="results">The list of generated graphs.</param>
        /// <param name="job">The job instance to be updated.</param>
        /// <param name="currentIndex">The current index of the task batch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessBatch(List<Task> tasks, ConcurrentBag<ICriticalPathTaskGraph> results, Job job,
            int currentIndex)
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

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}