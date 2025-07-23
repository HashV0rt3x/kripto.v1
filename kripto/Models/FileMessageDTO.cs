namespace kripto.Models;

public class FileMessageDto
{
    public string FromUserId { get; set; }
    public string ToUserId { get; set; }
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }
}
