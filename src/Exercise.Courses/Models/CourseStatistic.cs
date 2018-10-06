namespace Exercise.Courses.Models
{
    public class CourseStatistics
    {
        public int Id { get; set; }
        public int StudentMinimumAge { get; set; }
        public int StudentMaximumAge { get; set; }
        public int StudentAverageAge { get; set; }
        public int MaxCapacity { get; set; }
        public int Capacity { get; set; }
    }
}