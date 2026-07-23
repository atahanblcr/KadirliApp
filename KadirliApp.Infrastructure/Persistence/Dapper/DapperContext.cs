using System.Data;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace KadirliApp.Infrastructure.Persistence.Dapper;

public class DapperContext : IDapperContext
{
    private readonly string _conn;

    public DapperContext(IConfiguration cfg)
    {
        _conn = cfg.GetConnectionString("Postgres")!;
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_conn);
}
