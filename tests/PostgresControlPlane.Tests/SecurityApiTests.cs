using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Models.Contracts;
using Xunit;

namespace PostgresControlPlane.Tests;

public sealed class SecurityApiTests
{
    [Fact]
    public async Task AnonymousRequestsAreDeniedWhileHealthRemainsPublic()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var healthResponse = await client.GetAsync("/health/live");
        var deploymentResponse = await client.GetAsync("/api/deployments");
        var databaseResponse = await client.GetAsync("/api/database/schemas");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deploymentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, databaseResponse.StatusCode);
    }

    [Fact]
    public async Task LoginStoresOnlySessionIdAndAuthorizesCurrentUser()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("operator", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "operator",
            password
        });
        var authentication = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.True(
            loginResponse.StatusCode == HttpStatusCode.OK,
            $"Login returned {(int)loginResponse.StatusCode}: {await loginResponse.Content.ReadAsStringAsync()}");
        Assert.NotNull(authentication);
        Assert.Equal("operator", authentication.User.Username);
        Assert.Contains(ApplicationRoles.SuperAdministrator, authentication.User.Roles);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var session = await context.AuthSessions.AsNoTracking().SingleAsync();
            Assert.NotEqual(authentication.AccessToken, session.TokenId);
            Assert.Equal(32, session.TokenId.Length);
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task NonAdministratorCannotUseAdministrativeEndpoints()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("viewer", password, ApplicationRoles.DefaultUser);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "viewer",
            password
        });
        var authentication = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
        var protectedRoutes = new[]
        {
            "/api/users",
            "/api/roles",
            "/api/auth/sessions",
            "/api/deployments",
            "/api/database/schemas",
            "/api/files",
            "/api/audit"
        };

        foreach (var route in protectedRoutes)
        {
            var response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        var invocationResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/example/invoke",
            new { arguments = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Forbidden, invocationResponse.StatusCode);
    }

    [Fact]
    public async Task DatabaseBrowserRejectsInjectedIdentifiersBeforeQueryExecution()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("admin", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password
        });
        var authentication = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

        var response = await client.GetAsync("/api/database/tables?schema=public%3BDROP%20SCHEMA%20ums");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DatabaseBrowserNeverExposesTheControlPlaneSchema()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("schema-admin", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "schema-admin", password);

        var response = await client.GetAsync("/api/database/tables?schema=control");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RevokedSessionCannotAuthenticateAgain()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("revoked-user", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "revoked-user",
            password
        });
        var authentication = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuditApiReturnsMetadataWithoutSensitivePayloadFields()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("auditor", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "auditor",
            password
        });
        var authentication = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        var auditResponse = await client.GetAsync("/api/audit?page=1&pageSize=10");
        using var document = JsonDocument.Parse(await auditResponse.Content.ReadAsStreamAsync());
        var firstEvent = document.RootElement.GetProperty("items").EnumerateArray().First();

        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        Assert.True(firstEvent.TryGetProperty("correlationId", out _));
        Assert.False(firstEvent.TryGetProperty("queryString", out _));
        Assert.False(firstEvent.TryGetProperty("requestBody", out _));
        Assert.False(firstEvent.TryGetProperty("responseBody", out _));
        Assert.False(firstEvent.TryGetProperty("ipAddress", out _));
    }

    [Fact]
    public async Task AdministratorCannotRemoveOwnAccessOrDeleteOwnAccount()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        var userId = await factory.SeedUserAsync(
            "protected-admin",
            password,
            ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "protected-admin", password);

        var removeRoleResponse = await client.PutAsJsonAsync($"/api/users/{userId}", new
        {
            roleIds = Array.Empty<int>()
        });
        var deleteResponse = await client.DeleteAsync($"/api/users/{userId}");

        Assert.Equal(HttpStatusCode.Conflict, removeRoleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AdministrativeIdentityChangesRevokeTheUsersExistingSessions()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        var targetUserId = await factory.SeedUserAsync(
            "role-target",
            password,
            ApplicationRoles.DefaultUser);
        await factory.SeedUserAsync(
            "role-admin",
            password,
            ApplicationRoles.SuperAdministrator);

        using var targetClient = factory.CreateClient();
        await AuthenticateAsync(targetClient, "role-target", password);
        using var adminClient = factory.CreateClient();
        await AuthenticateAsync(adminClient, "role-admin", password);

        int defaultRoleId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            defaultRoleId = await context.Roles
                .Where(role => role.Name == ApplicationRoles.DefaultUser)
                .Select(role => role.Id)
                .SingleAsync();
        }

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/users/{targetUserId}", new
        {
            roleIds = new[] { defaultRoleId }
        });
        var targetResponse = await targetClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, targetResponse.StatusCode);
    }

    [Fact]
    public async Task FileUploadRejectsFakeOfficeArchiveAndListsSafeTextFile()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("file-admin", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "file-admin", password);

        using var fakeOfficeContent = new MultipartFormDataContent();
        using var fakeOfficeFile = new ByteArrayContent([0x50, 0x4B, 0x03, 0x04, 0x00]);
        fakeOfficeFile.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        fakeOfficeContent.Add(fakeOfficeFile, "file", "fake.docx");
        var rejectedResponse = await client.PostAsync("/api/files", fakeOfficeContent);

        using var invalidTextContent = new MultipartFormDataContent();
        using var invalidTextFile = new ByteArrayContent([0xFF, 0xFE, 0xFD]);
        invalidTextFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        invalidTextContent.Add(invalidTextFile, "file", "invalid.txt");
        var invalidTextResponse = await client.PostAsync("/api/files", invalidTextContent);

        using var textContent = new MultipartFormDataContent();
        using var textFile = new ByteArrayContent(Encoding.UTF8.GetBytes("safe test file"));
        textFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        textContent.Add(textFile, "file", "notes.txt");
        var uploadResponse = await client.PostAsync("/api/files", textContent);
        var uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<StoredFileResponse>();
        Assert.NotNull(uploadedFile);
        var listResponse = await client.GetFromJsonAsync<StoredFilePageResponse>(
            "/api/files?page=1&pageSize=10");
        Assert.NotNull(listResponse);
        var downloadResponse = await client.GetAsync(uploadedFile.DownloadUrl);

        Assert.Equal(HttpStatusCode.BadRequest, rejectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidTextResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.Equal(1, listResponse.Total);
        Assert.Single(listResponse.Items);
        Assert.Matches("^[a-f0-9]{32}\\.txt$", uploadedFile.Id);
        Assert.Equal("attachment", downloadResponse.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", downloadResponse.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task DeploymentResponsesNeverContainSubmittedPasswords()
    {
        await using var factory = new ApiFactory();
        const string password = "integration-password";
        await factory.SeedUserAsync("deployment-admin", password, ApplicationRoles.SuperAdministrator);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, "deployment-admin", password);

        var request = new
        {
            name = "safe-demo",
            databaseHost = "db.internal",
            databasePort = 5432,
            databaseName = "demo",
            databaseUser = "app_user",
            databasePassword = "database-secret",
            applicationAdminUsername = "operator",
            applicationAdminPassword = "application-secret"
        };
        var response = await client.PostAsJsonAsync("/api/deployments", request);
        var body = await response.Content.ReadAsStringAsync();
        var duplicateResponse = await client.PostAsJsonAsync("/api/deployments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.DoesNotContain("database-secret", body, StringComparison.Ordinal);
        Assert.DoesNotContain("application-secret", body, StringComparison.Ordinal);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
    }
}
