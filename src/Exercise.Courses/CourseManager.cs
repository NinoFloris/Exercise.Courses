using Exercise.Courses.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exercise.Courses
{
    public class CourseManager
    {
        // Normally you would pull these strings from somewhere.
        private static ValidationError MaxCapacity => ValidationError.FromError(nameof(MaxCapacity), "Cannot signup to this course, maximum capacity reached.");

        public Result<Person, ValidationError> AddToCourse(Person student, Course course)
        {
            if (!student.IsStudent) { throw new ArgumentException(nameof(student), "Dev error: Given person is not a student."); }
            if (course.Students == null) { throw new ArgumentException(nameof(course), "Dev error: Given course doesn't have Students navigation property loaded."); }

            if (course.MaxCapacity == course.Students.Count)
            {
                return Result.Error<Person>.Create(MaxCapacity);
            }

            student.AttendingCourses = student.AttendingCourses ?? new List<CourseStudent>();
            if (!student.AttendingCourses.Exists(x => x.StudentId == student.Id && x.CourseId == course.Id))
            {
                student.AttendingCourses.Add(new CourseStudent { Course = course, Student = student });
            }
            return Result.Ok<ValidationError>.Create(student);
        }

        // processStats "hack" until we finally have AsyncEnumerable in C# 8
        private async Task CreateStatisticsImpl(IQueryable<Course> courses, Func<CourseStatistics, Task> processStats, int? courseId = null, CancellationToken token = default)
        {
            // Honestly I'd do this completely in SQL if I would drop down to raw sql.
            var query = courses.Select(c => new
            {
                c.Id,
                c.MaxCapacity,
                Capacity = c.Students.Count,
                BirthYears = c.Students.Select(s => s.Student.BirthYear.Value)
            });
            if (courseId.HasValue) { query = query.Where(x => x.Id == courseId.Value); }
            await query.ForEachAsync(async x =>
            {
                var (min, max, avg) = CalculateMinMaxAvg(x.BirthYears.ToList());
                await processStats(new CourseStatistics
                {
                    Id = x.Id,
                    Capacity = x.Capacity,
                    MaxCapacity = x.MaxCapacity,
                    StudentMinimumAge = min,
                    StudentMaximumAge = max,
                    StudentAverageAge = avg,
                });
            }, token);

            (int min, int max, int avg) CalculateMinMaxAvg(List<DateTimeOffset> birthYears)
            {
                if (birthYears.Count == 0) { return (0, 0, 0); }
                var (min, max, avg) = (0, 0, 0);
                foreach (var birthYear in birthYears)
                {
                    var age = Person.BirthYearToAge(birthYear);
                    avg = avg + age;
                    if (age < min) { min = age; }
                    if (age > max) { max = age; }
                }
                avg = avg / birthYears.Count;
                return (min, max, avg);
            }
        }

        public async Task<CourseStatistics> CreateStatistics(int courseId, IQueryable<Course> courses, CancellationToken token = default)
        {
            CourseStatistics stats = null;
            await CreateStatisticsImpl(courses, s => { stats = s; return Task.CompletedTask; }, courseId, token);
            return stats;
        }

        public Task CreateAllStatistics(IQueryable<Course> courses, Func<CourseStatistics, Task> processStats, CancellationToken token = default)
            => CreateStatisticsImpl(courses, processStats, null, token);

    }
}