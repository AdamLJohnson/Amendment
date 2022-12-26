using Amendment.Model.DataModel;
using Mapster;

namespace Amendment.Server
{
    public static class MapsterConfig
    {
        public static void RegisterMapsterConfiguration(this IServiceCollection services)
        {
            TypeAdapterConfig<Role, int>.NewConfig()
                .MapWith(converterFactory: r => r.Id);
        }
    }
}
