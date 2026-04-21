using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

public class AdminTilesModel : PageModel
{
    public List<Character> Characters { get; set; } = new();

    public void OnGet() => LoadCharacters();

    public IActionResult OnPostDelete(int id)
    {
        string connectionString = "server=localhost;user=bitviduser;password=sunshine1!;database=bitdb;";
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        string query = "DELETE FROM Characters WHERE Id=@Id";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();

        return RedirectToPage();
    }

    private void LoadCharacters()
    {
        string connectionString = "server=localhost;user=bitviduser;password=sunshine1!;database=bitdb;";
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        var query = "SELECT Id, Name FROM Characters";
        using var command = new MySqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        Characters.Clear();
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
