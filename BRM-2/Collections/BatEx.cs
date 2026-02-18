namespace BRM_2.Collections;

  public class BatEx : BatTable
  {

       
      public List<Call> Calls { get; set; } = new List<Call>();

       
      public List<BatTag> BatTags { get; set; } = new List<BatTag>();


       
      public BatTag Tag { get; set; } = new BatTag();


       
      public string CallType
      {
          get
          {
              string type = "";
              foreach (var call in Calls ?? new List<Call>())
              { type += $"{call.CallType};"; }
              return type;
          }
      }

     
      public string FLow { get; set; }= string.Empty;

       
      public string FHigh { get; set; }= string.Empty;
       
      public string Duration { get; set; }= string.Empty;
       
      public string Bandwidth { get; set; }= string.Empty;
       
      public string Interval { get; set; }= string.Empty;

       
      public string FPeak { get; set; }= string.Empty;

      public BatEx(BatTable bat) : base()
      {
          this.ID = bat.ID;
          this.Batgenus = bat.Batgenus;
          this.BatSpecies = bat.BatSpecies;
          this.Name = bat.Name;
          this.Label = bat.Label;
          this.Notes = bat.Notes;
      }

      public BatEx() : base()
      {
      }

      public BatTable GetTable()
      {
          BatTable bat = new BatTable();
          bat.ID = this.ID;
          bat.Batgenus = this.Batgenus;
          bat.BatSpecies = this.BatSpecies;
          bat.Name = this.Name;
          bat.Label = this.Label;
          bat.Notes = this.Notes;
          return bat;
      }
  }