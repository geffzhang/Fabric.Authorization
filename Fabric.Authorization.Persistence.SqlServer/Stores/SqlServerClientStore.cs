﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : IClientStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;
        private readonly IGrainStore _grainStore;

        public SqlServerClientStore(IAuthorizationDbContext authorizationDbContext, IGrainStore grainStore)
        {   
            _authorizationDbContext = authorizationDbContext;
            _grainStore = grainStore;
        }

        public async Task<Client> Add(Client model)
        {
            Grain grain = null;
            if (model.TopLevelSecurableItem != null)
            {
                grain = await _grainStore.Get(model.TopLevelSecurableItem.Grain);
            }

            var clientEntity = model.ToEntity();
            clientEntity.TopLevelSecurableItem.GrainId = grain?.Id;

            _authorizationDbContext.Clients.Add(clientEntity);
            await _authorizationDbContext.SaveChangesAsync();
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">This is the unique client id of the client</param>
        /// <returns></returns>
        public async Task<Client> Get(string id)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);

            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {id}");
            }

            return client.ToModel();
        }

        public async Task<IEnumerable<Client>> GetAll()
        {
            var clients = await _authorizationDbContext.Clients
                .Where(c => !c.IsDeleted)
                .ToArrayAsync();

            return clients.Select(c => c.ToModel());
        }

        public async Task Delete(Client model)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(model.Id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);

            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {model.Id}");
            }

            client.IsDeleted = true;
            MarkSecurableItemsDeleted(client.TopLevelSecurableItem);

            await _authorizationDbContext.SaveChangesAsync();
        }

        private void MarkSecurableItemsDeleted(EntityModels.SecurableItem topLevelSecurableItem)
        {
            topLevelSecurableItem.IsDeleted = true;
            foreach (var securableItem in topLevelSecurableItem.SecurableItems)
            {
                MarkSecurableItemsDeleted(securableItem);
            }
        }

        public async Task Update(Client model)
        {
            var client = await _authorizationDbContext.Clients
                .Include(i => i.TopLevelSecurableItem)
                .SingleOrDefaultAsync(c => c.ClientId.Equals(model.Id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted);
            if (client == null)
            {
                throw new NotFoundException<Client>($"Could not find {typeof(Client).Name} entity with ID {model.Id}");
            }

            model.ToEntity(client);

            _authorizationDbContext.Clients.Update(client);
            await _authorizationDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">This is the unique client id of the client</param>
        /// <returns></returns>
        public async Task<bool> Exists(string id)
        {
            var client = await _authorizationDbContext.Clients                
                .SingleOrDefaultAsync(c => c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase)
                                           && !c.IsDeleted).ConfigureAwait(false);

            return client != null;
        }
    }
}
