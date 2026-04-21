using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

public class TilesModel : PageModel
{
    public List<Character> Characters { get; set; } = new();

    public void OnGet()
    {
        string connectionString = "server=localhost;user=bitviduser;password=sunshine1!;database=bitdb;";
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        var query = "SELECT Id, Name FROM Characters";
        using var command = new MySqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            Characters.Add(new Character
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name")
            });
        }
    }

    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
