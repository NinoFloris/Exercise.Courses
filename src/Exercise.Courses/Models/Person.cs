
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Exercise.Courses.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTimeOffset? BirthYear { get; set; }

        public Identity Identity { get; private set; }

        public List<CourseStudent> AttendingCourses { get; set; }

        // Purely here as EF Core needs it for entity inheritance, just an algebraic datatype.
        public class Teacher : Person
        {
            public List<Course> GivenCourses { get; set; }
        }

        // We cannot have student as a disparate derived hierarchy as a Teacher could also be a student of a course.
        // So being a student is a purely data based concept in this app.
        [JsonIgnore]
        public bool IsStudent => BirthYear.HasValue && Identity != default;

        public Person LinkIdentity(Identity identity)
        {
            if (Identity != null) { throw new InvalidOperationException("Dev error: Person Identity cannot be relinked."); }
            if (Id == default) { throw new InvalidOperationException("Dev error: Person Id cannot be empty when linking an identity."); }
            identity.Id = Id;
            Identity = identity;
            return this;
        }

        public Person UpgradeToStudent(int age, Identity identity)
        {
            if (IsStudent) { throw new InvalidOperationException($"Dev error: Person {ToString()} is already a student."); }
            if (!BirthYear.HasValue) { BirthYear = AgeToBirthYear(age); }
            if (Identity == null) { LinkIdentity(identity); }
            return this;
        }

        internal static DateTimeOffset AgeToBirthYear(int age)
            => new DateTimeOffset(DateTimeOffset.UtcNow.Year - age, 1, 1, 0, 0, 0, default);

        internal static int BirthYearToAge(DateTimeOffset birthYear)
            => DateTimeOffset.UtcNow.Year - birthYear.Year;

        public override string ToString()
            => Id.ToString(); // Don't ToString PII.
    }
}