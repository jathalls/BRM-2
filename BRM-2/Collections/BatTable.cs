namespace BRM_2.Collections;

 //[Table("BatTable")]
 public class BatTable
 {
     [PrimaryKey, AutoIncrement]
     public int ID { get; set; }
     public string Batgenus { get; set; }
     public string BatSpecies { get; set; }

     public string Name { get; set; } = string.Empty;

     public string Label { get; set; } = string.Empty;
     public string Notes { get; set; }
 }