using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Shared;
using Amendment.Shared.Responses;

namespace Amendment.Server.Mediator.Queries.UserQueries
{
    public sealed class GetAllUsersQuery : IRequest<IApiResult<List<UserResponse>>>
    {
    }
}
