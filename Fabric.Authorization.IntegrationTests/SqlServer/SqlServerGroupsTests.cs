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
    public class SqlServerGroupsTests : GroupsTests
    {
        public SqlServerGroupsTests(IntegrationTestsFixture fixture, SqlServerIntegrationTestsFixture sqlFixture) : base(fixture, StorageProviders.SqlServer, sqlFixture.ConnectionStrings)
        {
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("PatchGroup_ValidRequest_SuccessAsync", "Source1", "Group Display Name 1", "Group Description 1")]
        public async Task PatchGroup_ValidRequest_SuccessAsync2(string groupName, string groupSource, string displayName, string description)
        {
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource,
                    DisplayName = displayName,
                    Description = description
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var patchResponse = await Browser.Patch($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    DisplayName = "Group Display Name 2",
                    Description = "Group Description 2"
                });
            });

            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(patchResponse.Body.AsString());
            Assert.Equal(groupName, group.GroupName);
            Assert.Equal(groupSource, group.GroupSource);
            Assert.Equal("Group Display Name 2", group.DisplayName);
            Assert.Equal("Group Description 2", group.Description);
        }
    }
}