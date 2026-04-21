using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using System.IO;
using System.Threading.Tasks;

public class EditCharacterModel : PageModel
{
    [BindProperty]
    public Character CharacterData { get; set; }

    public string ImagePath { get; set; }

    public void OnGet(int id)
    {
        string connectionString = "server=localhost;user=bitviduser;password=sunshine1!;database=bitdb;";
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        string query = "SELECT Id, Name, Origin FROM Characters WHERE Id=@Id";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            CharacterData = new Character
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name"),
                Origin = reader["Origin"] != DBNull.Value ? reader.GetString("Origin") : ""
            };
        }

        ImagePath = $"/images/{CharacterData.Name}.png";
    }

    public async Task<IActionResult> OnPostAsync(IFormFile NewImage, IFormFile NewVoice)
    {
        string connectionString = "server=localhost;user=bitviduser;password=sunshine1!;database=bitdb;";
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        // Get the old character name for image/audio renaming
        string oldNameQuery = "SELECT Name FROM Characters WHERE Id=@Id";
        string oldName;
        using (var cmd = new MySqlCommand(oldNameQuery, connection))
        {
            cmd.Parameters.AddWithValue("@Id", CharacterData.Id);
            oldName = cmd.ExecuteScalar()?.ToString() ?? CharacterData.Name;
        }

        // Update Name + Origin in DB
        string updateQuery = "UPDATE Characters SET Name=@Name, Origin=@Origin WHERE Id=@Id";
        using (var cmd = new MySqlCommand(updateQuery, connection))
        {
            cmd.Parameters.AddWithValue("@Id", CharacterData.Id);
            cmd.Parameters.AddWithValue("@Name", CharacterData.Name);
            cmd.Parameters.AddWithValue("@Origin", CharacterData.Origin ?? "");
            cmd.ExecuteNonQuery();
        }

        // Handle new image upload
        if (NewImage != null && NewImage.Length > 0)
        {
            var formattedName = char.ToUpper(CharacterData.Name[0]) + CharacterData.Name.Substring(1).ToLower();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", formattedName + Path.GetExtension(NewImage.FileName));

            using var stream = new FileStream(filePath, FileMode.Create);
            await NewImage.CopyToAsync(stream);
        }
        else if (oldName != CharacterData.Name)
        {
            // Rename existing image if name changed
            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", oldName + ".png");
            var newImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", CharacterData.Name + ".png");

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Move(oldImagePath, newImagePath, overwrite: true);
            }
        }

        // Handle new voice upload
        if (NewVoice != null && NewVoice.Length > 0)
        {
            var voiceFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/audio");
            if (!Directory.Exists(voiceFolder))
                Directory.CreateDirectory(voiceFolder);

            var voiceFileName = CharacterData.Name + Path.GetExtension(NewVoice.FileName);
            var voicePath = Path.Combine(voiceFolder, voiceFileName);

            using var voiceStream = new FileStream(voicePath, FileMode.Create);
            await NewVoice.CopyToAsync(voiceStream);

            // Save only the file name in the database
            string updateVoiceQuery = "UPDATE Characters SET VoiceUrl=@VoiceUrl WHERE Id=@Id";
            using var cmdVoice = new MySqlCommand(updateVoiceQuery, connection);
            cmdVoice.Parameters.AddWithValue("@VoiceUrl", voiceFileName); // <--- only file name
            cmdVoice.Parameters.AddWithValue("@Id", CharacterData.Id);
            cmdVoice.ExecuteNonQuery();
        }
        else if (oldName != CharacterData.Name)
        {
            // Rename existing audio if name changed
            var oldVoicePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/audio", oldName + ".wav");
            var newVoicePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/audio", CharacterData.Name + ".wav");

            if (System.IO.File.Exists(oldVoicePath))
            {
                System.IO.File.Move(oldVoicePath, newVoicePath, overwrite: true);

                // Update VoiceUrl in DB
                string updateVoiceQuery = "UPDATE Characters SET VoiceUrl=@VoiceUrl WHERE Id=@Id";
                using var cmdVoice = new MySqlCommand(updateVoiceQuery, connection);
                cmdVoice.Parameters.AddWithValue("@VoiceUrl", "/audio/" + CharacterData.Name + ".wav");
                cmdVoice.Parameters.AddWithValue("@Id", CharacterData.Id);
                cmdVoice.ExecuteNonQuery();
            }
        }

        return RedirectToPage("/ChatAdmin/AdminTiles");
    }


    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Origin { get; set; }
    }
}
