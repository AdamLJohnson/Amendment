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

namespace Amendment.Server.Mediator.Handlers
{
    public sealed class AccountLoginHandler : IRequestHandler<AccountLoginCommand, IApiResult>
    {
        private readonly ILogger<AccountLoginHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IConfigurationSection _jwtSettings;

        public AccountLoginHandler(ILogger<AccountLoginHandler> logger, IConfiguration configuration, IUserService userService, IPasswordHashService passwordHashService)
        {
            _logger = logger;
            _configuration = configuration;
            _userService = userService;
            _passwordHashService = passwordHashService;
            _jwtSettings = _configuration.GetSection("JWTSettings");
        }

        public async Task<IApiResult> Handle(AccountLoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetAsync(request.Username);
            if (user == null || !_passwordHashService.VerifyHashedPassword(user.Password, request.Password))
                return new ApiFailedResult(HttpStatusCode.Unauthorized);
            var signingCredentials = GetSigningCredentials();
            var claims = GetClaims(user);
            var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return new ApiSuccessResult<AccountLoginResponse>(new AccountLoginResponse { Token = token });
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings["securityKey"] ?? throw new NullReferenceException("Security key can not be null"));
            var secret = new SymmetricSecurityKey(key);

            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private List<Claim> GetClaims(User user)
        {
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, user.Username),
                new ("id", user.Id.ToString())
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
                issuer: _jwtSettings["validIssuer"],
                audience: _jwtSettings["validAudience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings["expiryInMinutes"])),
                signingCredentials: signingCredentials);

            return tokenOptions;
        }
    }
}
