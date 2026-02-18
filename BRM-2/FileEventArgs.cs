namespace BRM_2;
public class FileEventArgs
{
    public string folder { get; set; } = "";
    public string file { get; set; } = "";

    public FileEventArgs(string FQFileName)
    {
        if (!string.IsNullOrWhiteSpace(FQFileName))
        {
            this.folder = Path.GetDirectoryName(FQFileName) ?? "";
            this.file = Path.GetFileName(FQFileName);
        }
    }
}