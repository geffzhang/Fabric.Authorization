﻿using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers;
using Microsoft.AspNetCore.Hosting;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Serilog;
using Serilog.Core;

namespace Fabric.Authorization.IntegrationTests
{
    public class TestBootstrapper : Bootstrapper
    {
        private readonly ClaimsPrincipal _principal;
        private readonly IIdentityServiceProvider _identityServiceProvider;
        private readonly IIdPSearchProvider _idPSearchProvider;

        public TestBootstrapper(ILogger logger, 
            IAppConfiguration appConfig, 
            LoggingLevelSwitch levelSwitch,
            IHostingEnvironment env, 
            ClaimsPrincipal principal, 
            IIdentityServiceProvider identityServiceProvider = null,
            IIdPSearchProvider idPSearchProvider = null)
            : base(logger, appConfig, levelSwitch, env, null)
        {
            _principal = principal;
            _identityServiceProvider = identityServiceProvider;
            _idPSearchProvider = idPSearchProvider;
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            context.CurrentUser = _principal;
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            if (_identityServiceProvider != null)
            {
                container.Register(_identityServiceProvider);
            }

            if (_idPSearchProvider != null)
            {
                container.Register(_idPSearchProvider);
            }
        }
    }
}
