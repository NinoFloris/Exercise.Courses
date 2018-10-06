using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Exercise.Courses.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public int MaxCapacity { get; set; }
        public int TeacherId { get; set; }
        public Person.Teacher Teacher { get; set; }
        public List<CourseStudent> Students { get; set; }
    }

    public class CourseStudent
    {
        public int CourseId { get; set; }
        // Normally I'd make outbound DTOs
        [JsonIgnore]
        public Course Course { get; set; }

        public int StudentId { get; set; }
        // Normally I'd make outbound DTOs
        [JsonIgnore]
        public Person Student { get; set; }
        public DateTimeOffset SignupAt { get; set; }
    }
}