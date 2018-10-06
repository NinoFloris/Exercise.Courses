using System;

namespace Exercise.Courses
{
    // Some helpers for C#s piss poor generic inference, eases working with Result.
    // This is all a bit tedious to setup once in C# but after that quite pleasant to use.
    public static class Result
    {
        public static class Ok<TError>
        {
            public static Result<T, TError> Create<T>(T value)
                => new Result<T, TError>.Ok(value);
        }

        public static class Error<TOk>
        {
            public static Result<TOk, TError> Create<TError>(TError value)
                => new Result<TOk, TError>.Error(value);
        }

        public static TError Errors<T, TError>(this Result<T, TError> result)
        {
            switch (result)
            {
                case Result<T, TError>.Error x: return x.Errors;
                default: throw new InvalidOperationException("Dev error: there is no error value in this result.");
            }
        }

        public static T Value<T, TError>(this Result<T, TError> result)
        {
            switch (result)
            {
                case Result<T, TError>.Ok x: return x.Value;
                default: throw new InvalidOperationException("Dev error: there is no ok value in this result.");
            }
        }
    }

    public abstract class Result<T, TError>
    {
        private Result() { }

        internal sealed class Ok : Result<T, TError>
        {
            public T Value { get; }
            public Ok(T value) => Value = value;
        }

        internal sealed class Error : Result<T, TError>
        {
            public TError Errors { get; }
            public Error(TError value) => Errors = value;
        }

        public bool IsOk => this is Ok;

        public Result<U, TError> Bind<U>(Func<T, Result<U, TError>> binder)
        {
            switch (this)
            {
                case Ok o:
                    return binder(o.Value);
                case Error e:
                    return new Result<U, TError>.Error(e.Errors);
                default:
                    throw new NotImplementedException("Dev error: should never be hit if we just have Ok and Error cases.");
            }
        }

        public Result<U, TError> Map<U>(Func<T, U> mapper)
            => Bind(x => new Result<U, TError>.Ok(mapper(x)));
    }
}