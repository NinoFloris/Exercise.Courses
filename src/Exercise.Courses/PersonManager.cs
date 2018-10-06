using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Exercise.Courses.Result;
using Microsoft.EntityFrameworkCore;
using Exercise.Courses.Models;

namespace Exercise.Courses
{
    public static class PersonExtensions
    {
        public static IQueryable<Person> FindPerson(this IQueryable<Person> persons, string firstName, string lastName, string normalizedEmail = null)
        {
            var q = persons.Where(p =>
                p.FirstName == firstName && p.LastName == lastName
            );
            // Include email in search if there is any identity on the person.
            if (normalizedEmail != null)
            {
                q = q.Where(p => p.Identity != null ? p.Identity.NormalizedEmail == normalizedEmail : true);
            }
            return q.Include(x => x.Identity);
        }
    }

    public class PersonManager
    {
        private readonly IdentityManager _identityManager;

        public PersonManager(IdentityManager identityManager)
        {
            _identityManager = identityManager;
        }

        // Normally you would pull these strings from somewhere.
        private static ValidationError MissingFirstName => ValidationError.FromError(nameof(MissingFirstName), "FirstName cannot be empty.");
        private static ValidationError MissingLastName => ValidationError.FromError(nameof(MissingLastName), "LastName cannot be empty.");
        private static ValidationError MissingAge => ValidationError.FromError(nameof(MissingAge), "Age cannot be empty if person is attending any courses.");

        private Result<Person, ValidationError> ValidateIdentity(Person person)
        {
            var errors = Enumerable.Empty<KeyValuePair<string, string[]>>();
            if (string.IsNullOrWhiteSpace(person.FirstName)) { errors = errors.Concat(MissingFirstName); }
            if (string.IsNullOrWhiteSpace(person.LastName)) { errors = errors.Concat(MissingLastName); }
            if (person.AttendingCourses?.Any() == true && !person.BirthYear.HasValue) { errors = errors.Concat(MissingAge); }

            return errors.Any() ? Error<Person>.Create(new ValidationError(errors)) : Ok<ValidationError>.Create(person);
        }

        private Task<Result<Person, ValidationError>> ApplyChanges(Person person)
        {
            var result = ValidateIdentity(person);
            return Task.FromResult(result);
        }

        public Task<Result<Person, ValidationError>> Create(Person person)
            => ApplyChanges(person);

        public async Task<Result<Person, ValidationError>> CreateWithIdentity(IQueryable<Person> persons, IQueryable<Identity> identities, Person person, string email)
        {
            var identity = new Identity { Id = person.Id, Email = email };
            var identityResult = await _identityManager.Create(identities, identity);

            var linkResult = identityResult.Map(i => person.LinkIdentity(i));

            return linkResult.IsOk ? await Create(person) : linkResult;
        }

        public async Task<Result<Person, ValidationError>> CreateStudent(IQueryable<Person> persons, IQueryable<Identity> identities, string firstName, string lastName, int age, string email, int? id = null)
        {
            var result = (await _identityManager.Create(identities, new Identity { Email = email }))
                .Map(identity => (new Person
                {
                    Id = id.HasValue ? id.Value : new Random().Next(1, int.MaxValue),
                    FirstName = firstName,
                    LastName = lastName,
                }).UpgradeToStudent(age, identity));

            if (!result.IsOk) { return result; }

            return await Create(result.Value());
        }

        public async Task<Result<Person, ValidationError>> FindOrCreateStudent(IQueryable<Person> persons, IQueryable<Identity> identities, string firstName, string lastName, string email, int age)
        {
            var person = await persons.FindPerson(firstName, lastName, _identityManager.NormalizeKey(email)).SingleOrDefaultAsync();
            if (person != null)
            {
                if (!person.IsStudent) { person.UpgradeToStudent(age, new Identity { Email = email }); }
                return Result.Ok<ValidationError>.Create(person);
            }

            return await CreateStudent(persons, identities, firstName, lastName, age, email);
        }
    }
}