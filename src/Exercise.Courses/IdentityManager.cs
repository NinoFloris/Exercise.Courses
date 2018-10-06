using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using static Exercise.Courses.Result;
using System;
using Exercise.Courses.Models;
using System.Linq;

namespace Exercise.Courses
{
    public class IdentityManager
    {
        private readonly ILookupNormalizer _keyNormalizer;

        // Normally you would pull these strings from somewhere.
        private static ValidationError MissingEmail => ValidationError.FromError(nameof(MissingEmail), "Email cannot be empty.");

        public IdentityManager(ILookupNormalizer keyNormalizer)
        {
            _keyNormalizer = keyNormalizer;
        }

        private async Task<Result<Identity, ValidationError>> ValidateIdentity(IQueryable<Identity> identities, Identity identity)
        {
            await Task.CompletedTask; // Suppress warning
            if (string.IsNullOrWhiteSpace(identity.Email)) { return Error<Identity>.Create(MissingEmail); }
            return Ok<ValidationError>.Create(identity);
        }

        private Identity UpdateNormalizedProperties(Identity identity)
        {
            identity.NormalizedEmail = _keyNormalizer.Normalize(identity.Email);
            return identity;
        }

        private async Task<Result<Identity, ValidationError>> ApplyChanges(IQueryable<Identity> identities, Identity identity)
        {
            // If and only if ValidateIdentity returns Ok we can "Map the Ok value (identity)" into the lambda which does the next work.
            return (await ValidateIdentity(identities, identity)).Map(i => UpdateNormalizedProperties(i));
        }

        public Task<Result<Identity, ValidationError>> UpdateEmail(IQueryable<Identity> identities, Identity identity, string newEmail)
        {
            identity.Email = newEmail;
            return ApplyChanges(identities, identity);
        }

        public string NormalizeKey(string value)
            => _keyNormalizer.Normalize(value);

        public Task<Result<Identity, ValidationError>> Create(IQueryable<Identity> identities, Identity identity)
            => ApplyChanges(identities, identity);
    }

}