using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.IntegrationTests.Modules;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerRolesTests : RolesTests
    {
        public SqlServerRolesTests(IntegrationTestsFixture fixture, SqlServerIntegrationTestsFixture sqlFixture) : base(fixture, StorageProviders.SqlServer, sqlFixture.ConnectionStrings)
        {
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("EA318378-CCA3-42B4-93E2-F2FBF12E679A", "Role Display Name 1", "Role Description 1")]
        public async Task PatchRole_ValidRequest_SuccessAsync2(string name, string displayName, string description)
        {
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name,
                    DisplayName = displayName,
                    Description = description
                });
            });

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roles = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString());

            Assert.Single(roles);
            var role = roles.First();

            var patchResponse = await _browser.Patch($"/roles/{role.Id}", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    DisplayName = "Role Display Name 2",
                    Description = "Role Description 2"
                });
            });

            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            role = JsonConvert.DeserializeObject<RoleApiModel>(patchResponse.Body.AsString());
            Assert.Equal(name, role.Name);
            Assert.Equal("Role Display Name 2", role.DisplayName);
            Assert.Equal("Role Description 2", role.Description);
        }
    }
}
