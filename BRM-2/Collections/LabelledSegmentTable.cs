namespace BRM_2.Collections;
 [Table("LabelledSegmentTable")]
 public class LabelledSegmentTable
 {
     

     [PrimaryKey,AutoIncrement]
     public int ID { get; set; }

     public int RecordingID { get; set; } = 0;


     public DateTime StartOffset { get; set; }

     public DateTime EndOffset { get; set; } 

     [XmlAttribute("Comment")]
     public string Comment { get; set; } = "";

     [XmlAttribute("AutoID")]
     public string AutoID { get; set; } = string.Empty;

     [XmlAttribute("AutoIdProb")]
     public double AutoIdProb { get; set; } = 0.0d;


 }
 