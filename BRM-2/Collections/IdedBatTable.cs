using System.Xml.Serialization;
namespace BRM_2.Collections;

 [Table("IdedBatTable")]
 public class IdedBatTable
 {
     [PrimaryKey,AutoIncrement]
     public int ID { get; set; }


     [XmlAttribute("BatName")]
     public string Name { get; set; } = "";

    

     public bool ByAutoId { get; set; } = false;

     public int SegmentID { get; set; } = -1;

    
 }
