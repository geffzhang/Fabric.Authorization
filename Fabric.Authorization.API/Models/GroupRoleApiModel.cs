﻿using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class GroupRoleApiModel : ApiModelBase
    {
        public string GroupName { get; set; }
        public IEnumerable<RoleApiModel> Roles { get; set; }
    }
}
