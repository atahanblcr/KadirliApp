using System.Data;

namespace KadirliApp.Application.Common.Interfaces;

public interface IDapperContext
{
    IDbConnection CreateConnection();
}
