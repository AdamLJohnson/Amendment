using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Microsoft.Extensions.Logging;

namespace Amendment.Client.Repository
{
    public interface IUserRepository : IHttpRepository<UserRequest, UserResponse>
    {

    }
    public class UserRepository : HttpRepository<UserRequest, UserResponse>, IUserRepository
    {
        protected override string _baseUrl { get; set; } = "api/User";
        public UserRepository(ILogger<UserRepository> logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
        {
        }
    }
}
