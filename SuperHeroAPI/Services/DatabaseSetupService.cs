using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PostgreSQL.Data;
using SuperHeroAPI.Models;
using SuperHeroAPI.Services.SuperHeroService;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;

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
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

                try
                {
                    // Check if the UMS schema exists
                    bool schemaExists = false;
                    var connection = dbContext.Database.GetDbConnection();
                    await connection.OpenAsync(cancellationToken);
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'ums');";
                        schemaExists = (bool)await command.ExecuteScalarAsync(cancellationToken);
                    }

                    if (!schemaExists)
                    {
                        Console.WriteLine("UMS schema does not exist. Creating schema and tables...");
                        
                        try
                        {
                            // Create the UMS schema
                            await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS ums;", cancellationToken);
                            
                            // Check if the creation script file exists
                            string scriptPath = "creationscript.txt";
                            if (!File.Exists(scriptPath))
                            {
                                Console.WriteLine($"Creation script not found at: {Path.GetFullPath(scriptPath)}");
                                // Try to find the file in different locations
                                var possiblePaths = new[] { 
                                    "creationscript.txt", 
                                    "/app/creationscript.txt",
                                    "SuperHeroAPI/creationscript.txt",
                                    "../creationscript.txt",
                                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "creationscript.txt"),
                                    "/src/SuperHeroAPI/creationscript.txt",
                                    "../../creationscript.txt"
                                };
                                
                                foreach (var path in possiblePaths)
                                {
                                    Console.WriteLine($"Checking for script at: {Path.GetFullPath(path)}");
                                    if (File.Exists(path))
                                    {
                                        scriptPath = path;
                                        Console.WriteLine($"Found creation script at: {Path.GetFullPath(path)}");
                                        break;
                                    }
                                }
                                
                                if (!File.Exists(scriptPath))
                                {
                                    throw new FileNotFoundException($"Creation script file not found. Searched paths: {string.Join(", ", possiblePaths.Select(p => Path.GetFullPath(p)))}");
                                }
                            }
                            
                            // Read the creation script
                            string creationScript = await File.ReadAllTextAsync(scriptPath, cancellationToken);
                            
                            // Replace the hardcoded database name with the one from .env
                            string dbName = Env.GetString("DB_NAME");
                            Console.WriteLine($"Replacing 'superherodb' with actual database name '{dbName}' in creation script");
                            
                            // Replace database name in all SQL statements that refer to the database
                            creationScript = Regex.Replace(
                                creationScript, 
                                @"superherodb", 
                                $"{dbName}", 
                                RegexOptions.IgnoreCase
                            );
                            
                            // Execute the modified creation script
                            await dbContext.Database.ExecuteSqlRawAsync(creationScript, cancellationToken);
                            
                            Console.WriteLine("UMS schema and tables created successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating UMS schema and tables: {ex.Message}");
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                    else
                    {
                        Console.WriteLine("UMS schema already exists.");
                    }

                    // Check if we need to create a superuser
                    // Create superuser role if it doesn't exist
                    var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "superadmin", cancellationToken);
                    if (adminRole == null)
                    {
                        adminRole = new Role { RoleName = "superadmin" };
                        await roleService.AddRole(adminRole);
                        Console.WriteLine("Created superadmin role");
                    }

                    // Create guest role if it doesn't exist
                    var guestRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "guests", cancellationToken);
                    if (guestRole == null)
                    {
                        guestRole = new Role { RoleName = "guests" };
                        await roleService.AddRole(guestRole);
                        Console.WriteLine("Created guest role");
                    }

                    // Check if database superuser exists (using DB_USER from .env)
                    string dbUserName = Env.GetString("DB_USERNAME");
                    var superUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == dbUserName, cancellationToken);

                    if (superUser == null)
                    {
                        // Create superuser with credentials from .env
                        var userDto = new UserDto
                        {
                            Username = dbUserName,
                            Password = Env.GetString("DB_PASSWORD_USER")
                        };

                        // Add the user
                        var users = await userService.AddUser(userDto);
                        superUser = users.FirstOrDefault(u => u.Username == dbUserName);
                        
                        if (superUser != null)
                        {
                            Console.WriteLine($"Created superuser '{dbUserName}'");

                            // Assign SuperAdmin role to the user
                            adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "superadmin", cancellationToken);
                            if (adminRole != null)
                            {
                                // Check if the user already has this role
                                var existingUserRole = await dbContext.UserRoles.FirstOrDefaultAsync(
                                    ur => ur.UserId == superUser.Id && ur.RoleId == adminRole.RoleId, 
                                    cancellationToken
                                );

                                if (existingUserRole == null)
                                {
                                    // Add SuperAdmin role to the user
                                    var userRole = new UserRole
                                    {
                                        UserId = superUser.Id,
                                        RoleId = adminRole.RoleId
                                    };
                                    dbContext.UserRoles.Add(userRole);
                                    await dbContext.SaveChangesAsync(cancellationToken);
                                    Console.WriteLine($"Assigned superadmin role to user '{dbUserName}'");
                                }
                            }

                            // Grant PostgreSQL superuser rights
                            try
                            {
                                using (var command = connection.CreateCommand())
                                {
                                    // Format the SQL command
                                    command.CommandText = $"DO $$ BEGIN IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'superadmin') THEN EXECUTE 'ALTER ROLE \"superadmin\" WITH SUPERUSER'; END IF; END $$;";
                                    await command.ExecuteNonQueryAsync(cancellationToken);
                                    Console.WriteLine($"Granted PostgreSQL superuser rights to '{dbUserName}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error granting superuser rights: {ex.Message}");
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Superuser '{dbUserName}' already exists");
                    }

                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in database setup: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
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