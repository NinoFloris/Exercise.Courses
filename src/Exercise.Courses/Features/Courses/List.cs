using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exercise.Courses.Models;
using Exercise.Courses.Services;

namespace Exercise.Courses.Features.Courses
{
    public class Overview
    {
        public class Query : IRequest<List<Model>>
        {
            public int Offset { get; set; } = 0;
            public int Limit { get; set; } = 20;
        }

        public struct Model
        {
            public CourseStatistics Statistics { get; set; }
        }

        public class Handler : IRequestHandler<Query, List<Model>>
        {
            private readonly StatisticsService _statisticsService;

            public Handler(StatisticsService statisticsService)
            {
                _statisticsService = statisticsService;
            }

            public async Task<List<Model>> Handle(Query message)
            {
                var stats = await _statisticsService.GetList(message.Offset, message.Limit);
                return stats.Select(x => new Model { Statistics = x }).ToList();
            }
        }
    }

}
