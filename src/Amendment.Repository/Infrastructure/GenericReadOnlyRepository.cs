using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    public sealed class GenericReadOnlyRepository<T> : BaseReadOnlyRepository<T> where T : class, IReadOnlyTable
    {
        public GenericReadOnlyRepository(IDbFactory dbFactory) : base(dbFactory)
        {
        }
    }
}