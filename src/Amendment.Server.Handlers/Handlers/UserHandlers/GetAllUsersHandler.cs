using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Queries.UserQueries;
using Amendment.Shared.Responses;
using Amendment.Shared;
using MediatR;
using Microsoft.Extensions.Logging;
using Amendment.Service;
using Mapster;

namespace Amendment.Server.Mediator.Handlers.UserHandlers
{
    public sealed class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, IApiResult<List<UserResponse>>>
    {
        private readonly ILogger<GetAllUsersHandler> _logger;
        private readonly IUserService _userService;

        public GetAllUsersHandler(ILogger<GetAllUsersHandler> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public async Task<IApiResult<List<UserResponse>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var results = await _userService.GetAllAsync();
            var output = results.Adapt<List<UserResponse>>();
            return new ApiSuccessResult<List<UserResponse>>(output);
        }
    }
}
