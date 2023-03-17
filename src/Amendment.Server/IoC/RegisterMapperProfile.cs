using Amendment.Server.Mediator.Mapping;
using Amendment.Shared;
using Autofac;
using AutoMapper;
using MediatR;

namespace Amendment.Server.IoC
{
    public class RegisterMapperProfile : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            //    .Where(a => !a.FullName.Contains("Autofac", StringComparison.OrdinalIgnoreCase)).ToArray();
            //builder.RegisterAssemblyTypes(typeof(UserProfile).Assembly)
            //    .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic && t.IsClass)
            //    .As<Profile>();

            var config = new MapperConfiguration(cfg =>
            {
                //cfg.AddProfiles(typeof(Program), typeof(UserProfile));
                cfg.CreateMap(typeof(IRequest<>), typeof(IRequest<IApiResult>));
                cfg.AddProfile(typeof(MappingProfile));
            });
            builder.Register(c => config).AsSelf().SingleInstance();

            builder.Register(c => c.Resolve<MapperConfiguration>()
                .CreateMapper(c.Resolve))
                .As<IMapper>()
                .InstancePerLifetimeScope();
        }
    }
}
