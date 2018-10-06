using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exercise.Courses.Models;
using Microsoft.EntityFrameworkCore;

namespace Exercise.Courses.Services
{
    public class StatisticsService
    {
        private readonly AppDbContext _dbContext;
        private readonly CourseManager _courseManager;

        public StatisticsService(AppDbContext dbContext, CourseManager courseManager)
        {
            _dbContext = dbContext;
            _courseManager = courseManager;
        }

        public async Task<CourseStatistics> Get(int courseId, CancellationToken token = default)
        {
            var stats = await _dbContext.CourseStatistics.SingleOrDefaultAsync(x => x.Id == courseId);
            if (stats == null)
            {
                // Ad-hoc calculation, in case a course was created after last statistic job.
                stats = await _courseManager.CreateStatistics(courseId, _dbContext.Courses, token);
            }

            return stats;
        }

        public Task<List<CourseStatistics>> GetList(int offset = 0, int limit = 20)
        {
            return _dbContext.CourseStatistics.Skip(offset).Take(limit).ToListAsync();
        }
    }
}