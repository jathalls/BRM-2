namespace BRM_2.Collections;
public class IdedBatEx : IdedBatTable
{
    [XmlAttribute("ByAutoId")]
    public string ByAutoIdString
    {
        get { return ByAutoId.ToString(); }
        set
        {
            if (value.ToUpper() == "TRUE") { ByAutoId = true; }
            else { ByAutoId = false; }
        }
    }

    /// <summary>
    /// Returns a 4-letter upper case string identifying the bat
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task<string> Get4LetterCode()
    {
        string result = "";
        if (ByAutoId) { return ""; }

        var bat = await DBAccess.GetNamedBat(Name);
        var tagList = await DBAccess.GetBatTagsAsync(bat.ID);
        string code = (from tag in tagList ?? new List<BatTag>()
                       where (tag?.Tag?.Length ?? 0) == 4 && (tag?.Tag?.ToUpper() ?? "") == (tag?.Tag ?? " ")
                       select (tag?.Tag ?? "")).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(code))
        {
            code = (bat.Batgenus.Substring(0, 3) + bat.BatSpecies.Substring(0, 3)).ToUpper();
        }
        return code;

    }

    internal async Task<string> GetLabel()
    {
        string result = "";
        if (ByAutoId) { return ""; }

        var bat = await DBAccess.GetNamedBat(Name);
        
        return bat.Label;
    }

    public IdedBatEx() : base()
    {
    }

    public IdedBatEx(IdedBatTable ibt) : base()
    {
        this.ID = ibt.ID;
       
        this.Name = ibt.Name;
        this.ByAutoId = ibt.ByAutoId;
        this.SegmentID= ibt.SegmentID;

    }

    public IdedBatTable GetTable()
    {
        IdedBatTable ibt = new IdedBatTable();
        ibt.ID = this.ID;
        
        ibt.Name = this.Name;
        ibt.ByAutoId = this.ByAutoId;
        ibt.SegmentID = this.SegmentID;

        return ibt;
    }
}
