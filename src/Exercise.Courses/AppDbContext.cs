using Microsoft.EntityFrameworkCore;
using Exercise.Courses.Models;
using System.Linq;
using System;

namespace Exercise.Courses
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Identity> Identities { get; set; }
        public DbSet<Person.Teacher> Teachers { get; set; }
        public DbSet<CourseStatistics> CourseStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Identity>(b =>
            {
                b.ToTable("Identities");
                b.HasOne<Person>()
                    .WithOne(p => p.Identity)
                    .HasForeignKey<Identity>(u => u.Id)
                    .IsRequired();
            });

            builder.Entity<CourseStudent>(b =>
            {
                b.Property(x => x.SignupAt).HasDefaultValueSql("now() at time zone 'utc'");
                b.HasKey(s => new { s.CourseId, s.StudentId });
            });
        }
    }

    public static class DbContextExtensions
    {
        public static bool IsAttached<TEntity>(this DbContext context, TEntity entity)
            => context.Entry(entity).State != EntityState.Detached;
    }
}