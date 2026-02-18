namespace BRM_2.Collections;
[Table("Meta")]
public class Meta
{
    [PrimaryKey,AutoIncrement]
    public int ID { get; set; }

    [XmlAttribute("Label")]
    public string Label { get; set; } = string.Empty;

    [XmlAttribute("Value")]
    public string Value { get; set; }

    [XmlAttribute("Type")]
    public string Type {  get; set; }

    public int RecordingID { get; set; }
}
