using Amendment.Shared.Responses;
using Amendment.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Server.Mediator.Commands.SystemSettingCommands;

public sealed class CreateSystemSettingCommand : IRequest<IApiResult>
{
    public int SavingUserId { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
}

public sealed class UpdateSystemSettingCommand : IRequest<IApiResult>
{
    public int SavingUserId { get; set; }
    public int Id { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
}

public sealed record DeleteSystemSettingCommand(int Id, int SavingUserId) : IRequest<IApiResult>;
