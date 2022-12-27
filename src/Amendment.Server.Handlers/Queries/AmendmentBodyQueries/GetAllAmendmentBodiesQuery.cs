using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Queries.AmendmentBodyQueries
{
    public sealed record GetAllAmendmentBodiesQuery(int AmendmentId) : IRequest<IApiResult<List<AmendmentBodyResponse>>>;
}
