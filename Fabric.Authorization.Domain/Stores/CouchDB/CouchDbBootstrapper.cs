﻿using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace Fabric.Authorization.Domain.Stores.CouchDB
{
    public class CouchDbBootstrapper : IDbBootstrapper
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;
        public CouchDbBootstrapper(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService ?? throw new ArgumentNullException(nameof(documentDbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public void Setup()
        {
            _documentDbService.Initialize().Wait();
            _documentDbService.SetupDefaultUser().Wait();
            _documentDbService.AddViews("roles", CouchDbRoleStore.GetViews()).Wait();
            _documentDbService.AddViews("permissions", CouchDbPermissionStore.GetViews()).Wait();
        }
    }
}