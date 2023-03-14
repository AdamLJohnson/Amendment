using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amendment.Server.Mediator.Commands;
using Amendment.Service;
using Amendment.Shared;
using Amendment.Shared.Responses;
using MediatR;

namespace Amendment.Server.Mediator.Handlers
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, IApiResult>
    {
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public RefreshTokenCommandHandler(ITokenService tokenService, IUserService userService)
        {
            _tokenService = tokenService;
            _userService = userService;
        }

        public async Task<IApiResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
                return new ApiFailedResult<AccountLoginResponse>(HttpStatusCode.BadRequest);

            var principal = _tokenService.GetPrincipalFromExpiredToken(request.Token);
            var username = principal.Identity.Name;

            var user = await _userService.GetAsync(username);

            if (user == null)// || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return new ApiFailedResult<AccountLoginResponse>(HttpStatusCode.BadRequest);

            var signingCredentials = _tokenService.GetSigningCredentials();
            var claims = _tokenService.GetClaims(user);
            var tokenOptions = _tokenService.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            user.RefreshToken = _tokenService.GenerateRefreshToken();

            await _userService.UpdateAsync(user, user.Id);

            return new ApiSuccessResult<AccountLoginResponse>(new AccountLoginResponse { Token = token, RefreshToken = user.RefreshToken });
        }
    }
}
