using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exercise.Courses.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Exercise.Courses.Services
{
    public class StatisticsBackgroundServiceOptions
    {
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(30);
        public int BatchSize { get; set; } = 100;
    }

    public class StatisticsBackgroundService : BackgroundService
    {
        private readonly StatisticsBackgroundServiceOptions _options;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CourseManager _courseManager;

        public StatisticsBackgroundService(IOptions<StatisticsBackgroundServiceOptions> options, ILogger<StatisticsBackgroundService> logger, IServiceProvider serviceProvider, CourseManager courseManager)
        {
            _options = options.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _courseManager = courseManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"StatisticsService is starting.");

            stoppingToken.Register(() =>
                    _logger.LogDebug($" StatisticsService background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                using (var scope2 = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var dbContext2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    _logger.LogDebug($"StatisticsService task doing background work.");

                    try
                    {
                        await _courseManager.CreateAllStatistics(dbContext.Courses, async stats =>
                        {
                            dbContext2.CourseStatistics.Add(stats);

                            // Sadly no upsert in EF Core, so we do batch checking of insert/update and save batch afterwards
                            if (dbContext2.ChangeTracker.Entries().Count() >= _options.BatchSize)
                            {
                                await SaveBatch(dbContext2, stoppingToken);
                            }
                        }, stoppingToken);
                        await SaveBatch(dbContext2, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CreateAllStatistics threw an unhandled exception.");
                    }
                    finally
                    {
                        await Task.Delay(_options.CheckInterval, stoppingToken);
                    }
                }
            }

            _logger.LogDebug($"StatisticsService background task is stopping.");
        }

        private async Task SaveBatch(AppDbContext dbContext, CancellationToken stoppingToken)
        {
            var ids = dbContext.ChangeTracker.Entries()
                                   .Select(x => (x.Entity as CourseStatistics)?.Id)
                                   .Where(x => x.HasValue).Select(x => x.Value).ToList();

            var updateNotInsert = await dbContext.CourseStatistics
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(stoppingToken);

            foreach (var stats in dbContext.CourseStatistics.Local)
            {
                if (updateNotInsert.Contains(stats.Id))
                {
                    dbContext.Update(stats);
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }

        private string CreateCacheKey(int courseId)
        {
            return courseId.ToString();
        }
    }

}