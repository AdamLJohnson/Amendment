using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Queries.RoleQueries;
using Amendment.Shared.Responses;
using Amendment.Shared;
using MediatR;
using Microsoft.Extensions.Logging;
using Amendment.Service;
using Mapster;

namespace Amendment.Server.Mediator.Handlers.RoleHandlers
{
    public sealed class GetAllRolesHandler : IRequestHandler<GetAllRolesQuery, IApiResult<List<RoleResponse>>>
    {
        private readonly ILogger<GetAllRolesHandler> _logger;
        private readonly IRoleService _roleService;

        public GetAllRolesHandler(ILogger<GetAllRolesHandler> logger, IRoleService RoleService)
        {
            _logger = logger;
            _roleService = RoleService;
        }

        public async Task<IApiResult<List<RoleResponse>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var results = await _roleService.GetAllAsync();
            var output = results.Adapt<List<RoleResponse>>();
            return new ApiSuccessResult<List<RoleResponse>>(output);
        }
    }
}
