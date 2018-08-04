using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.Infrastructure;

namespace Amendment.Model.DataModel
{
    public class Role : BaseModel
    {
        public int RoleId { get; set; }
        public string Name { get; set; }
    }
}
