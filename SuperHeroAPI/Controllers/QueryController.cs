using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SuperHeroAPI.Services.SuperHeroService;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Security.Claims;
using System.Text.RegularExpressions;

[Route("api/[controller]")]
[ApiController, Authorize]
public class QueryController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IRoleService _roleService;

    public QueryController(DataContext context, IRoleService roleService)
    {
        _context = context;
        _roleService = roleService;
    }

    [HttpPost]
 
    public async Task<IActionResult> Post([FromBody] QueryRequest queryRequest)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        try
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                // Set the role based on the user's role received from the claim
                command.CommandText = $"SET ROLE {userRole}; {queryRequest.Query}";
                command.CommandType = CommandType.Text;
                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Check if the result set contains any rows
                    if (!reader.HasRows)
                    {
                        return Ok("Пустой вывод."); // Return 404 Not Found if no rows are returned
                    }

                    var result = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }

                    // If only one row is returned, return it as a single object, not an array
                    if (result.Count == 1)
                    {
                        

                            return Ok(result[0]);
                        
                    }

                    return Ok(result);
                }
            }
        }
        catch (DbException ex)
        {
            return BadRequest(ex.Message);
        }
    }



    [HttpGet("{tableName}")]

    public async Task<IActionResult> Get(string tableName)
    {
        return await ExecuteQuery(tableName, "SELECT * FROM " + tableName + ";");
    }

    [HttpPost("{tableName}")]

    public async Task<IActionResult> Post(string tableName, [FromBody] QueryRequest queryRequest)
    {
        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        return await ExecuteQuery(tableName, $"INSERT INTO {tableName} {queryRequest.Query}");
    }

    [HttpPut("{tableName}")]
  
    public async Task<IActionResult> Put(string tableName, [FromBody] QueryRequest queryRequest)
    {
        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        return await ExecuteQuery(tableName, $"UPDATE {tableName} SET {queryRequest.Query}");
    }

    [HttpDelete("{tableName}")]

    public async Task<IActionResult> Delete(string tableName, [FromBody] QueryRequest queryRequest)
    {
        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        return await ExecuteQuery(tableName, $"DELETE FROM {tableName} WHERE {queryRequest.Query}");
    }


    private async Task<IActionResult> ExecuteQuery(string tableName, string query)
    {
        try
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                // Set the role based on the user's role received from the claim
                command.CommandText = $"SET ROLE {userRole}; {query}";
                command.CommandType = CommandType.Text;
                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var result = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }


           
                    return Ok(result);
                }
            }
        }
        catch (DbException ex)
        {
            return BadRequest(ex.Message);
        }
    }


}

    
