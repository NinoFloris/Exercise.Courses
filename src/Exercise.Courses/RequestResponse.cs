using System.Threading.Tasks;

namespace Exercise.Courses
{
    public interface IRequest<out TResponse>
    {
    }

    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest message);
    }
}