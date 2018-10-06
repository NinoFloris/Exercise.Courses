using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Exercise.Courses.Services
{
    public interface IEventQueue<in TEvent>
    {
        Task<bool> Enqueue(TEvent e);
    }

    public interface IEventHandler<in TEvent>
    {
        Task<bool> HandleEvent(TEvent message, int queueDepth);
    }

    public class EventQueue<TEvent> : IEventQueue<TEvent>
    {
        public ConcurrentQueue<TEvent> Queue { get; set; } = new ConcurrentQueue<TEvent>();

        public Task<bool> Enqueue(TEvent e)
        {
            Queue.Enqueue(e);
            return Task.FromResult(true);
        }
    }

    public class EventQueueService<TEvent> : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly EventQueue<TEvent> _queue;
        private readonly IServiceProvider _serviceProvider;


        public EventQueueService(ILogger<EventQueueService<TEvent>> logger, EventQueue<TEvent> queue, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"EventQueueService<{typeof(TEvent).ToString()}> is starting.");

            stoppingToken.Register(() =>
                    _logger.LogDebug($" EventQueueService<{typeof(TEvent).ToString()}> background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug($"EventQueueService<{typeof(TEvent).ToString()}> waking up to do background work.");

                if (_queue.Queue.TryDequeue(out var e))
                {
                    _logger.LogDebug($"EventQueueService<{typeof(TEvent).ToString()}> doing background work.");
                    try
                    {
                        using (var s = _serviceProvider.CreateScope())
                        {
                            var handler = s.ServiceProvider.GetRequiredService<IEventHandler<TEvent>>();
                            await handler.HandleEvent(e, _queue.Queue.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"IEventHandler<{typeof(TEvent).ToString()}> threw an unhandled exception.");
                    }
                }
                else
                {
                    // Wait for a second if no event in the queue.
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogDebug($"EventQueueService<{typeof(TEvent).ToString()}> background task is stopping.");
        }
    }
}