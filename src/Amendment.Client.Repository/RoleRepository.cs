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
    public interface IRoleRepository : IHttpRepository<RoleRequest, RoleResponse>
    {

    }
    public class RoleRepository : HttpRepository<RoleRequest, RoleResponse>, IRoleRepository
    {
        protected override string _baseUrl { get; set; } = "api/Role";
        private IEnumerable<RoleResponse>? _roles;
        public RoleRepository(ILogger<RoleRepository> logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
        {
        }

        public override async Task<IEnumerable<RoleResponse>> GetAsync()
        {
            if (_roles != null)
                return _roles;

            var dbRoles = await base.GetAsync();
            _roles = dbRoles;
            return _roles;
        }
    }
}
