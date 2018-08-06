using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amendment.Repository;
using Amendment.Repository.Infrastructure;
using Amendment.Service;
using Autofac;
using Module = Autofac.Module;

namespace Amendment.Web.IoC
{
    public class RegisterDataServices : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(GenericRepository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
            builder.RegisterType<DbFactory>().As<IDbFactory>().InstancePerLifetimeScope();

            // Repositories
            builder.RegisterAssemblyTypes(typeof(GenericRepository<>).GetTypeInfo().Assembly)
                .Where(t => t.Name.EndsWith("Repository"))//.Except<InMemoryShapeRepository>().Except<CartoShapeRepository>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            // Services
            builder.RegisterAssemblyTypes(typeof(UserService).GetTypeInfo().Assembly)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
