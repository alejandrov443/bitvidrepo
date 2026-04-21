using System;
using System.IO;
using System.Threading.Tasks;

public class ImageSaver
{
    public async Task SaveBase64ImageAsync(string base64String, string filePath)
    {
        // If the string contains "data:image/png;base64,", remove that prefix
        var base64Data = base64String.Contains(",")
            ? base64String.Split(',')[1]
            : base64String;

        // Convert Base64 to byte array
        byte[] imageBytes = Convert.FromBase64String(base64Data);

        // Save to file
        await File.WriteAllBytesAsync(filePath, imageBytes);
    }
}
