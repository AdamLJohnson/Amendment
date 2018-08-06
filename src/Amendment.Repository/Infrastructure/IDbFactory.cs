using System;

namespace Amendment.Repository.Infrastructure
{
    public interface IDbFactory : IDisposable
    {
        AmendmentContext Init();
    }
}
