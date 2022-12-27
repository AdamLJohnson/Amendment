using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Queries.SystemSettingQueries;

public sealed record GetAllSystemSettingsQuery : IRequest<IApiResult<List<SystemSettingResponse>>>;

public sealed record GetSingleSystemSettingQuery(int Id) : IRequest<IApiResult<SystemSettingResponse>>;