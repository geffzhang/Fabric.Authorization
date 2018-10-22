﻿using System.Threading.Tasks;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers;

namespace Fabric.Authorization.API.Services
{
    public class IdPSearchService
    {
        private readonly IIdPSearchProvider _idPSearchProvider;

        public IdPSearchService(IIdPSearchProvider idPSearchProvider)
        {
            _idPSearchProvider = idPSearchProvider;
        }

        /// <summary>
        /// TODO: need to specify exact search
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="tenant"></param>
        /// <returns></returns>
        public async Task<FabricIdPSearchResponse> GetGroup(string groupName, string tenant)
        {
            var result = await _idPSearchProvider.Search(new IdPPrincipalSearchRequest
            {
                SearchText = groupName,
                Type = SearchType.Group.ToString()
            });

            return result;
        }
    }

    public enum SearchType
    {
        User,
        Group,
        UserAndGroup
    }
}