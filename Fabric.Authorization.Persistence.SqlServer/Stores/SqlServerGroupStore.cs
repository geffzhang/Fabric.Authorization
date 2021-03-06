﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Group = Fabric.Authorization.Domain.Models.Group;
using Role = Fabric.Authorization.Domain.Models.Role;
using User = Fabric.Authorization.Domain.Models.User;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerGroupStore : IGroupStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerGroupStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public async Task<Group> Add(Group model)
        {
            var alreadyExists = await Exists(model.Name);
            if (alreadyExists)
            {
                throw new AlreadyExistsException<Group>($"Group {model.Name} already exists. Please use a different GroupName.");
            }

            model.Id = Guid.NewGuid().ToString();
            var groupEntity = model.ToEntity();
            _authorizationDbContext.Groups.Add(groupEntity);
            await _authorizationDbContext.SaveChangesAsync();
            return groupEntity.ToModel();
        }

        public async Task<Group> Get(string name)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ThenInclude(p => p.SecurableItem)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .ThenInclude(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.SecurableItem)
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                                           && !g.IsDeleted);

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {name}");
            }

            group.GroupRoles = group.GroupRoles.Where(gr => !gr.IsDeleted).ToList();
            foreach (var groupRole in group.GroupRoles)
            {
                groupRole.Role.RolePermissions = groupRole.Role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
            }

            group.GroupUsers = group.GroupUsers.Where(gu => !gu.IsDeleted).ToList();
            foreach (var groupUser in group.GroupUsers)
            {
                groupUser.User.UserPermissions = groupUser.User.UserPermissions.Where(up => !up.IsDeleted).ToList();
            }

            return group.ToModel();
        }

        public async Task<IEnumerable<Group>> GetAll()
        {
            var groups = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .Where(g => !g.IsDeleted)
                .ToArrayAsync();

            return groups.Select(g => g.ToModel());
        }

        public async Task Delete(Group model)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId == Guid.Parse(model.Id));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Name}");
            }

            group.IsDeleted = true;

            foreach (var groupRole in group.GroupRoles)
            {
                if (!groupRole.IsDeleted)
                {
                    groupRole.IsDeleted = true;
                }
            }
            foreach (var groupUser in group.GroupUsers)
            {
                if (!groupUser.IsDeleted)
                {
                    groupUser.IsDeleted = true;
                }
            }

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Group model)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId == Guid.Parse(model.Id));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Name}");
            }

            model.ToEntity(group);

            _authorizationDbContext.Groups.Update(group);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string name)
        {
            var group = await _authorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                                           && !g.IsDeleted).ConfigureAwait(false);

            return group != null;
        }

        public async Task<Group> AddRolesToGroup(Group group, IEnumerable<Role> rolesToAdd)
        {
            var groupEntity = await _authorizationDbContext.Groups.SingleOrDefaultAsync(g =>
                g.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase)
                && !g.IsDeleted);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            foreach (var role in rolesToAdd)
            {
                group.Roles.Add(role);
                _authorizationDbContext.GroupRoles.Add(new GroupRole
                {
                    GroupId = groupEntity.GroupId,
                    RoleId = role.Id
                });
            }
            await _authorizationDbContext.SaveChangesAsync();
            return group;
        }

        public async Task<Group> DeleteRolesFromGroup(Group group, IEnumerable<Guid> roleIdsToDelete)
        {
            var groupRolesToRemove = _authorizationDbContext.GroupRoles
                .Where(gr => roleIdsToDelete.Contains(gr.RoleId) &&
                             gr.GroupId == Guid.Parse(group.Id)).ToList();

            if (groupRolesToRemove.Count == 0)
            {
                throw new NotFoundException<Role>($"No role mappings found for group {group.Name} with the supplied role IDs");
            }

            var missingRoleMappings = new List<Guid>();

            foreach (var groupRole in groupRolesToRemove)
            {
                // remove the role from the domain model
                var roleToRemove = group.Roles.FirstOrDefault(r => r.Id == groupRole.RoleId);

                if (roleToRemove == null)
                {
                    missingRoleMappings.Add(groupRole.RoleId);
                }

                group.Roles.Remove(roleToRemove);

                // mark the many-to-many DB entity as deleted
                groupRole.IsDeleted = true;
                _authorizationDbContext.GroupRoles.Update(groupRole);
            }

            if (missingRoleMappings.Any())
            {
                throw new NotFoundException<Role>($"No role mapping(s) found for group {group.Name} with the following role IDs: {missingRoleMappings.ToString(", ")}");
            }

            await _authorizationDbContext.SaveChangesAsync();
            return group;
        }

        public async Task<Group> AddUserToGroup(Group group, User user)
        {
            var groupUser = new GroupUser
            {
                GroupId = Guid.Parse(group.Id),
                SubjectId = user.SubjectId,
                IdentityProvider = user.IdentityProvider
            };

            _authorizationDbContext.GroupUsers.Add(groupUser);
            await _authorizationDbContext.SaveChangesAsync();

            return group;
        }

        public async Task<Group> AddUsersToGroup(Group group, IEnumerable<User> usersToAdd)
        {
            var groupEntity = await _authorizationDbContext.Groups.SingleOrDefaultAsync(g =>
                g.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase)
                && !g.IsDeleted);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            foreach (var user in usersToAdd)
            {
                group.Users.Add(user);
                _authorizationDbContext.GroupUsers.Add(new GroupUser
                {
                    GroupId = groupEntity.GroupId,
                    IdentityProvider = user.IdentityProvider,
                    SubjectId = user.SubjectId
                });
            }
            await _authorizationDbContext.SaveChangesAsync();
            return group;
        }

        public async Task<Group> DeleteUserFromGroup(Group group, User user)
        {
            var userInGroup = await _authorizationDbContext.GroupUsers
                .SingleOrDefaultAsync(gu =>
                    gu.SubjectId.Equals(user.SubjectId, StringComparison.OrdinalIgnoreCase) &&
                    gu.IdentityProvider.Equals(user.IdentityProvider, StringComparison.OrdinalIgnoreCase) &&
                    gu.GroupId == Guid.Parse(group.Id));

            if (userInGroup != null)
            {
                userInGroup.IsDeleted = true;
                _authorizationDbContext.GroupUsers.Update(userInGroup);
                await _authorizationDbContext.SaveChangesAsync();
            }

            return group;
        }
    }
}
