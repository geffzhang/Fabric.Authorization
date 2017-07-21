﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class ClientService
    {
        private static readonly List<string> TopLevelGrains = new List<string>
        {
            "app",
            "patient",
            "user"
        };

        private readonly IClientStore _clientStore;

        public ClientService(IClientStore clientStore)
        {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        }

        public async Task<bool> DoesClientOwnItem(string clientId, string grain, string securableItem)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return false;
            }

            var client = await _clientStore.Get(clientId);
            var topLevelSecurableItem = client.TopLevelSecurableItem;

            if (topLevelSecurableItem == null)
            {
                return false;
            }

            if (TopLevelGrains.Contains(grain) && topLevelSecurableItem.Name == securableItem)
            {
                return true;
            }

            return this.HasRequestedSecurableItem(topLevelSecurableItem, grain, securableItem);
        }

        public async Task<IEnumerable<Client>> GetClients()
        {
            return await _clientStore.GetAll();
        }

        public async Task<Client> GetClient(string id)
        {
            return await _clientStore.Get(id);
        }

        public async Task<Client> AddClient(Client client)
        {
            client.TopLevelSecurableItem = new SecurableItem
            {
                Id = Guid.NewGuid(),
                Name = client.Id
            };

            return await _clientStore.Add(client);
        }

        public async Task DeleteClient(Client client) => await _clientStore.Delete(client);

        private bool HasRequestedSecurableItem(SecurableItem parentSecurableItem, string grain, string securableItem)
        {
            var childSecurableItems = parentSecurableItem.SecurableItems;

            if (childSecurableItems == null || childSecurableItems.Count == 0)
            {
                return false;
            }

            if (parentSecurableItem.Name == grain && childSecurableItems.Any(r => r.Name == securableItem))
            {
                return true;
            }

            return childSecurableItems.Any(childSecurableItem => HasRequestedSecurableItem(childSecurableItem, grain, securableItem));
        }
    }
}