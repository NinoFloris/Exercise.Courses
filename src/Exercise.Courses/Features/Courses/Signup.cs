using System.Threading.Tasks;
using Exercise.Courses.Models;
using Exercise.Courses.Services;
using Microsoft.EntityFrameworkCore;

namespace Exercise.Courses.Features.Courses
{
    public class Signup
    {
        public struct Command : IRequest<Result<Person, ValidationError>>
        {
            public int CourseId { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<Person, ValidationError>>, IEventHandler<Command>
        {
            private readonly AppDbContext _context;
            private readonly PersonManager _personManager;
            private readonly CourseManager _courseManager;
            private readonly MailingService _mailingService;

            public Handler(AppDbContext context, PersonManager personManager, CourseManager courseManager, MailingService mailingService)
            {
                _context = context;
                _personManager = personManager;
                _courseManager = courseManager;
                _mailingService = mailingService;
            }

            public async Task<Result<Person, ValidationError>> Handle(Command signup)
            {
                var courseId = signup.CourseId;
                var course = await _context.Courses.Include(c => c.Students).SingleOrDefaultAsync(c => c.Id == courseId);
                if (course == null) { return Result.Error<Person>.Create(ValidationError.FromError("UnknownCourse", "Course could not be found.")); }

                var result = (await _personManager.FindOrCreateStudent(_context.Persons, _context.Identities, signup.FirstName, signup.LastName, signup.Email, signup.Age))
                    // Bind means, if result is Ok give me the value of Ok and I will produce another Result (here through AddToCourse)
                    // If there were errors Bind skips the lambda and just returns the result.
                    .Bind(person =>
                    {
                        if (!_context.IsAttached(person)) { _context.Add(person); }
                        return _courseManager.AddToCourse(person, course);
                    });

                await _context.SaveChangesAsync();
                return result;
            }

            public async Task<bool> HandleEvent(Command e, int queueDepth)
            {
                var result = await Handle(e);
                if (result.IsOk)
                {
                    await _mailingService.SendConfirmationEmail();
                    return true;
                }

                await _mailingService.SendDenyEmail();
                return false;
            }
        }
    }
}