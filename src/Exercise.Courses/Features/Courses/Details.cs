using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exercise.Courses.Models;
using Exercise.Courses.Services;
using Microsoft.EntityFrameworkCore;

namespace Exercise.Courses.Features.Courses
{
    public class Details
    {
        public struct Query : IRequest<Result<Model, ValidationError>>
        {
            public int CourseId { get; set; }
        }

        public class Model
        {
            public CourseStatistics Statistics { get; set; }
            public Person.Teacher Teacher { get; set; }
            public List<Person> Students { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<Model, ValidationError>>
        {
            private readonly AppDbContext _dbContext;
            private readonly StatisticsService _statisticsService;

            public Handler(AppDbContext dbContext, StatisticsService statisticsService)
            {
                _dbContext = dbContext;
                _statisticsService = statisticsService;
            }

            private ValidationError MissingCourse => ValidationError.FromError(nameof(MissingCourse), "Could not find course.");

            public async Task<Result<Model, ValidationError>> Handle(Query message)
            {
                var course = await _dbContext.Courses
                    .Include(c => c.Students)
                    .ThenInclude(s => s.Student)
                    .SingleOrDefaultAsync(x => x.Id == message.CourseId);
                if (course == null) { return Result.Error<Model>.Create(MissingCourse); }

                return Result.Ok<ValidationError>.Create(new Model
                {
                    Statistics = await _statisticsService.Get(message.CourseId),
                    Teacher = course.Teacher,
                    Students = course.Students.Select(x => x.Student).ToList()
                });
            }
        }
    }

}
