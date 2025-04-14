using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PostgreSQL.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SuperHeroAPI.Services
{
    public class DatabaseSetupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSetupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a scope to get scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                try
                {
                    // Check if the RequestLogs table already exists - use lowercase table name
                    bool tableExists = false;
                    var connection = dbContext.Database.GetDbConnection();
                    await connection.OpenAsync(cancellationToken);
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE c.relname='request_logs');";
                        tableExists = (bool)await command.ExecuteScalarAsync(cancellationToken);
                    }
                    await connection.CloseAsync();

                    if (!tableExists)
                    {
                        // Read the SQL script
                        string sqlScript = await File.ReadAllTextAsync("CreateRequestLogsTable.sql", cancellationToken);

                        // Execute the SQL script
                        await dbContext.Database.ExecuteSqlRawAsync(sqlScript, cancellationToken);
                        Console.WriteLine("RequestLogs table created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("RequestLogs table already exists.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating RequestLogs table: {ex.Message}");
                }
            }

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
} 