using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Exercise.Courses
{
    public struct ValidationError : IEnumerable<KeyValuePair<string, string[]>>
    {
        public Dictionary<string, string[]> Errors { get; }

        public static ValidationError Empty => new ValidationError(new Dictionary<string, string[]>());

        public ValidationError(Dictionary<string, string[]> errors)
            => Errors = errors;

        public ValidationError(IEnumerable<KeyValuePair<string, string[]>> errors)
            : this(new Dictionary<string, string[]>(errors)) { }

        public ValidationError(IEnumerable<(string, string)> errors)
            : this(errors.Select(x => KeyValuePair.Create(x.Item1, new[] { x.Item2 }))) { }

        public ValidationError(IEnumerable<(string, string[])> errors)
            : this(errors.Select(x => KeyValuePair.Create(x.Item1, x.Item2))) { }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
            => Errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Errors.GetEnumerator();

        public static ValidationError FromError(string code, string message)
            => new ValidationError(new[] { (code, message) });
    }
}