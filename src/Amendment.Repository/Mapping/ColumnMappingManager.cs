using System;
using System.Collections.Generic;
using System.Text;
//using Amendment.Repository.Mapping.Map;
using Dapper.FluentMap;

namespace Amendment.Repository.Mapping
{
    public static class ColumnMappingManager
    {
        public static void Register()
        {
            FluentMapper.Initialize(config =>
            {
                //config.AddMap(new UserMap());
                //config.AddMap(new RoleMap());
            });
        }
    }
}
