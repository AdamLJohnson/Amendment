using Amendment.Model.DataModel;
using Amendment.Server.Mediator.Commands;
using Amendment.Service;
using Amendment.Shared;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Server.Mediator.Handlers
{
    public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, IApiResult>
    {
        private readonly ILogger<ChangePasswordHandler> _logger;
        private readonly IUserService _userService;
        private readonly IPasswordHashService _passwordHashService;

        public ChangePasswordHandler(
            ILogger<ChangePasswordHandler> logger,
            IUserService userService,
            IPasswordHashService passwordHashService)
        {
            _logger = logger;
            _userService = userService;
            _passwordHashService = passwordHashService;
        }

        public async Task<IApiResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetAsync(request.UserId);
            if (user == null)
            {
                return new ApiFailedResult(HttpStatusCode.NotFound);
            }

            // Verify current password if not a first-time login
            if (!request.IsFirstTimeLogin)
            {
                if (!_passwordHashService.VerifyHashedPassword(user.Password, request.CurrentPassword))
                {
                    return new ApiFailedResult(new[] { new ValidationError { Message = "Current password is incorrect" } }, HttpStatusCode.BadRequest);
                }

                // Check if new password is the same as current password
                if (_passwordHashService.VerifyHashedPassword(user.Password, request.NewPassword))
                {
                    return new ApiFailedResult(new[] { new ValidationError { Message = "New password must be different from current password" } }, HttpStatusCode.BadRequest);
                }
            }

            // Update the password
            user.Password = _passwordHashService.HashPassword(request.NewPassword);
            user.RequirePasswordChange = false;
            user.LastUpdated = DateTime.UtcNow;
            user.LastUpdatedBy = request.UserId;

            var updateResult = await _userService.UpdateAsync(user, request.UserId);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to update user password: {Errors}", string.Join(", ", updateResult.Errors));
                return new ApiFailedResult(HttpStatusCode.InternalServerError);
            }

            return new ApiCommandSuccessResult();
        }
    }
}
