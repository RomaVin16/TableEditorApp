using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TableEditor.Repository.Repository;

namespace TableEditor.Repository
{
    public class DataRepository : DbContext
    {
        public DataRepository(DbContextOptions<DataRepository> options) : base(options)
        {
        }

        /// <summary>
        /// Создание экземпляра БД
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static DataRepository Create(IConfiguration configuration)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataRepository>();
            var connectionString = configuration.GetConnectionString("PostgreSQLConnection");
            optionsBuilder.UseNpgsql(connectionString);
            return new DataRepository(optionsBuilder.Options);
        }

        /// <summary>
        /// Асинхронно создает таблицу в БД
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public async Task CreateTableAsync(string tableName, List<Column> columns)
        {
            var sql = BuildCreateTableSql(tableName, columns);
            await Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Удаление таблицы 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task DeleteTable(string tableName)
        {
            await Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS \"{tableName}\";");
        }

        /// <summary>
        /// Создание SQL запроса
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        protected string BuildCreateTableSql(string tableName, List<Column> columns)
        {
            var columnDefinitions = columns.Select(c =>
            {
                var columnType = c.DataType switch
                {
                    "Целочисленный" => "INTEGER",
                    "Вещественный" => "FLOAT",
                    "Текстовый" => "VARCHAR(255)",
                    "Дата/Время" => "TIMESTAMP"
                };

                var primaryKey = c.IsPrimaryKey ? " PRIMARY KEY" : string.Empty;

                return $"\"{c.FieldName}\" {columnType}{primaryKey}";
            });

            return $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", columnDefinitions)});";
        }

        /// <summary>
        /// Получение имен таблиц в БД
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetTableNamesAsync()
        {
            var tableNames = new List<string>();

            await using var connection = this.Database.GetDbConnection();
            await connection.OpenAsync();

            var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";

            await using var command = connection.CreateCommand();
            command.CommandText = query;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }

            return tableNames;
        }

        /// <summary>
        /// Асинхронное получение полей таблицы 
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<List<Column>> GetColumnsAsync(string tableName, DataRepository context)
        {
            var columns = new List<Column>();

            var query = $@"
        SELECT column_name, data_type
        FROM information_schema.columns 
        WHERE table_name = '{tableName}'";

            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;

            await context.Database.OpenConnectionAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var column = new Column
                {
                    FieldName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsPrimaryKey = false
                };
                columns.Add(column);
            }
            await context.Database.CloseConnectionAsync();

            return columns;
        }

    }
}



