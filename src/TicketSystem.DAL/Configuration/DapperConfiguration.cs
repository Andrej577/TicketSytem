using Dapper;

namespace TicketSystem.DAL.Configuration;

public static class DapperConfiguration
{
    public static void Configure()
    {
        SqlMapper.AddTypeHandler(new PostgreSqlDateTimeOffsetTypeHandler());
    }
}
