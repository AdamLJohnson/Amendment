using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Mediator.Commands;
using Amendment.Shared;
using Amendment.Shared.Requests;
using AutoMapper;

namespace Amendment.Server.Mediator.Mapping
{
    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AccountLoginRequest, AccountLoginCommand>();
        }
    }
}
