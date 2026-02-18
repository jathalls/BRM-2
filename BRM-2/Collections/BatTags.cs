namespace BRM_2.Collections;

  public class BatTag
  {
      [PrimaryKey,AutoIncrement]
      public int ID { get; set; }

      public string Tag {  get; set; }

      public int BatID { get; set; }
  }
  