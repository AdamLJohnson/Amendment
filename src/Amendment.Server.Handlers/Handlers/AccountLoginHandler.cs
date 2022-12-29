using Amendment.Service;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Shared;
using System.Net;
using Amendment.Server.Mediator.Commands;
using System.Security.Cryptography;

namespace Amendment.Server.Mediator.Handlers
{
    public sealed class AccountLoginHandler : IRequestHandler<AccountLoginCommand, IApiResult>
    {
        private readonly ILogger<AccountLoginHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly IPasswordHashService _passwordHashService;

        public AccountLoginHandler(ILogger<AccountLoginHandler> logger, ITokenService tokenService, IUserService userService, IPasswordHashService passwordHashService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _userService = userService;
            _passwordHashService = passwordHashService;
        }

        public async Task<IApiResult> Handle(AccountLoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetAsync(request.Username);
            if (user == null || !_passwordHashService.VerifyHashedPassword(user.Password, request.Password))
                return new ApiFailedResult(HttpStatusCode.Unauthorized);
            var signingCredentials = _tokenService.GetSigningCredentials();
            var claims = _tokenService.GetClaims(user);
            var tokenOptions = _tokenService.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            user.RefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userService.UpdateAsync(user, user.Id);

            return new ApiSuccessResult<AccountLoginResponse>(new AccountLoginResponse { Token = token, RefreshToken = user.RefreshToken });
        }
    }
}
