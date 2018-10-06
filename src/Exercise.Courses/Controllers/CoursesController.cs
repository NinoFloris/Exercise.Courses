using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Exercise.Courses.Features.Courses;
using Exercise.Courses.Models;
using Exercise.Courses.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exercise.Courses.Controllers
{
    [ApiController, Route("/api/[controller]")]
    public class CoursesController
    {
        public struct SignupModel
        {
            [EmailAddress]
            public string Email { get; set; }
            [MaxLength(255)]
            public string FirstName { get; set; }
            [MaxLength(255)]
            public string LastName { get; set; }
            [Range(1, 255)]
            public int Age { get; set; }

            public Signup.Command ToSignupRequest(int courseId)
                => new Signup.Command
                {
                    CourseId = courseId,
                    Age = Age,
                    Email = Email,
                    FirstName = FirstName,
                    LastName = LastName
                };
        }

        [HttpPost("{courseId:int}/signup")]
        public async Task<ActionResult<Person>> SignupBlocking(int courseId, SignupModel signup, [FromServices]IRequestHandler<Signup.Command, Result<Person, ValidationError>> handler)
        {
            var result = await handler.Handle(signup.ToSignupRequest(courseId));
            return result.IsOk ? new OkObjectResult(result.Value()) : Web.ValidationProblem(result.Errors());
        }

        [HttpPost("{courseId:int}/signup-queued")]
        public async Task<ActionResult<Person>> SignupQueued(int courseId, SignupModel signup, [FromServices]IEventQueue<Signup.Command> queue)
        {
            return await queue.Enqueue(signup.ToSignupRequest(courseId)) ? new OkResult() : new StatusCodeResult(500);
        }

        [HttpGet]
        public Task<List<Overview.Model>> GetList([FromServices]IRequestHandler<Overview.Query, List<Overview.Model>> handler)
            => handler.Handle(default);

        [HttpGet("{courseId:int}")]
        public async Task<ActionResult<Details.Model>> GetDetails(int courseId, [FromServices]IRequestHandler<Details.Query, Result<Details.Model, ValidationError>> handler)
        {
            var result = await handler.Handle(new Details.Query { CourseId = courseId });
            return result.IsOk ? new OkObjectResult(result.Value()) : Web.ValidationProblem(result.Errors());
        }
    }
}