using AIProductSearch.DAO;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace AIProductSearch.DAL;

public class Products
{
    private string ConnectionString { get; set; }

    public Products(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(CompleteProductsList(), new JsonSerializerOptions { WriteIndented = true });
    }

    public List<Product> CompleteProductsList()
    {
        List<Product> products = new();

        using SqliteConnection connection = new(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "select Id, Name, Description, Price, CurrencyISOCode from Products";
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            Product product = new()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Price = reader.GetDecimal(3),
                CurrencyISOCode = reader.GetInt32(4)
            };

            products.Add(product);
        }

        return products;
    }
}