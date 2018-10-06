using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Exercise.Courses
{
    public class UnknownServerProblemDetails : ProblemDetails
    {
        public UnknownServerProblemDetails()
        {
            Title = "Unknown server error occurred.";
            Status = 500;
            Type = "/unknown-error";
        }
    }

    public class ValidationProblemDetails : ProblemDetails
    {
        public ValidationProblemDetails()
        {
            Title = "One or more validation errors occurred.";
            Status = 400;
            Type = "/validation-error";
        }

        public IDictionary<string, string[]> Errors { get; set; }
    }

    public static class Web
    {
        public static ObjectResult UnknownServerProblem()
        {
            return new ObjectResult(new UnknownServerProblemDetails()) { StatusCode = 500 };
        }

        public static ObjectResult ValidationProblem(ValidationError errors, int status = 400)
        {
            var details = new ValidationProblemDetails { Errors = errors.Errors, Status = status };
            switch (status)
            {
                case 404: return new NotFoundObjectResult(details);
                default: return new BadRequestObjectResult(details);
            }
        }
    }
}