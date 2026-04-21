using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

public class CharactersCreateModel : PageModel
{
    [BindProperty]
    public string Name { get; set; }

    [BindProperty]
    public string Context { get; set; }

    [BindProperty]
    public IFormFile Photo { get; set; }


    [BindProperty]
    public IFormFile? Voice { get; set; }

    public string SuccessMessage { get; set; }
    public string ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Photo == null)
        {
            ErrorMessage = "Please provide valid input.";
            return Page();
        }

        try
        {
            string trimmedName = Regex.Replace(Name.Trim(), @"\s+", " ");
            string formattedName = FormatName(trimmedName);

            // Upload photo
            string photoExtension = Path.GetExtension(Photo.FileName).ToLowerInvariant();
            string photoFileName = BuildFileName(formattedName, photoExtension);

            string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(imagesPath))
                Directory.CreateDirectory(imagesPath);

            string photoPath = Path.Combine(imagesPath, photoFileName);
            using (var stream = new FileStream(photoPath, FileMode.Create))
            {
                await Photo.CopyToAsync(stream);
            }

            // Upload and trim voice if provided
            string voiceFileName = "";
            if (Voice != null)
            {
                string voiceExtension = Path.GetExtension(Voice.FileName).ToLowerInvariant();
                voiceFileName = BuildSafeVoiceFileName(formattedName, voiceExtension);

                string audioPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio");
                if (!Directory.Exists(audioPath))
                    Directory.CreateDirectory(audioPath);

                string voicePath = Path.Combine(audioPath, voiceFileName);
                using (var stream = new FileStream(voicePath, FileMode.Create))
                {
                    await Voice.CopyToAsync(stream);
                }

                // --- Trim to 11 seconds using FFmpeg ---
                string tempTrimmedPath = Path.Combine(audioPath, "trimmed_" + voiceFileName);

                // Ensure FFmpeg is installed and in PATH
                var ffmpegArgs = $"-i \"{voicePath}\" -t 11 -c copy \"{tempTrimmedPath}\"";
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new System.Diagnostics.Process { StartInfo = processInfo })
                {
                    process.Start();
                    await process.WaitForExitAsync();
                }

                // Replace the original file if the trimmed version was created
                if (System.IO.File.Exists(tempTrimmedPath))
                {
                    System.IO.File.Delete(voicePath);
                    System.IO.File.Move(tempTrimmedPath, voicePath);
                }
            }

            // Prepare message
            string message = string.IsNullOrWhiteSpace(Context)
                ? $"Roleplay as {formattedName}"
                : $"Roleplay as {formattedName} (from {Context.Trim()})";

            // Insert into database
            string connectionString = "Server=localhost;Database=bitdb;User=bitviduser;Password=sunshine1!;";
            using (var conn = new MySqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string sql = "INSERT INTO Characters (Name, Message, voiceurl, Origin) VALUES (@Name, @Message, @VoiceUrl, @Origin)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", formattedName);
                    cmd.Parameters.AddWithValue("@Message", message);
                    cmd.Parameters.AddWithValue("@VoiceUrl", string.IsNullOrEmpty(voiceFileName) ? "" : voiceFileName);
                    cmd.Parameters.AddWithValue("@Origin", Context);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            SuccessMessage = "Character created successfully!";
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }

        return Page();
    }



    private string FormatName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var parts = input.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        string firstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0].ToLower());
        string rest = parts.Length > 1 ? string.Join(" ", parts.Skip(1).Select(p => p.ToLower())) : "";

        return string.IsNullOrEmpty(rest) ? firstName : $"{firstName} {rest}";
    }

    private string BuildFileName(string name, string extension)
    {
        var parts = name.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "character" + extension;

        string first = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0].ToLower());
        string rest = string.Join(" ", parts.Skip(1).Select(p => p.ToLower()));
        return string.IsNullOrEmpty(rest) ? $"{first}{extension}" : $"{first} {rest}{extension}";
    }

    // Voice naming method can stay for future use
    private string BuildSafeVoiceFileName(string name, string extension)
    {
        var parts = name.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "Character" + extension;

        string first = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0].ToLower());
        string rest = parts.Length > 1 ? string.Join("_", parts.Skip(1).Select(p => p.ToLower())) : "";

        string combined = string.IsNullOrEmpty(rest) ? first : $"{first}_{rest}";
        combined = Regex.Replace(combined, @"[^a-zA-Z0-9_-]", "");

        return combined + extension;
    }
}
