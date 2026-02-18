using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace BRM_2.Collections;
public partial class RecordingEx : RecordingTable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string PropertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName)); }

    [XmlAttribute("RecordingStartTime")]
    [Ignore]
    public TimeSpan RecordingStartTimeSpan
    {
        get { return RecordingStartTime.TimeOfDay; }
        set { RecordingStartTime = RecordingDate.Date + value; }
    }


    [XmlAttribute("RecordingEndTime")]
    [Ignore]
    public TimeSpan RecordingEndTimeSpan
    {
        get { return RecordingEndTime.TimeOfDay; }
        set { RecordingEndTime = RecordingDate.Date + value; }
    }


    public TimeSpan RecordingDuration
    {
        get
        {
            var dur = RecordingEndTime - (RecordingDate.Date + RecordingStartTime.TimeOfDay);
            return dur;
        }
    }




    [XmlArray("Segments")]
    [XmlArrayItem("LabelledSegment")]
    [Ignore]
    public List<LabelledSegmentEx> LabelledSegments
    {
        get
        {
            
            return _labelledSegments;
        }
        set { _labelledSegments = value; }
    }
    private List<LabelledSegmentEx> _labelledSegments = new List<LabelledSegmentEx>();

    

    public async Task<List<LabelledSegmentEx>> RefreshLabelledSegments()
    {
        var ls = await DBAccess.GetSegmentsForRecordingAsync(this.ID);// includes IdedBats
        LabelledSegments = ls ?? new List<LabelledSegmentEx>();
        for (int i = 0; i < LabelledSegments.Count; i++)
        {
            
            
            LabelledSegments[i].BatSummaryList = await LabelledSegments[i].GetSegBatSummaryAsync();
            this.BatSummaryList.AddRange(LabelledSegments[i].BatSummaryList);
        }
        return LabelledSegments;
    }



    [XmlArray("Metas")]
    [XmlArrayItem("Meta")]
    [Ignore]
    public List<Meta> Metas { get; set; } = new List<Meta>();

    [Ignore]
    public int NumberOfSegments
    {
        get { return LabelledSegments.Count; }
    }


    public string RecordingDateOnly { get { return _recordingDate.ToShortDateString(); } }


    private string _batSummaryString = "";
    [Ignore]
    public string BatSummaryString
    {
        get
        {
            return _batSummaryString;
        }

        set
        {
            _batSummaryString = value;
            OnPropertyChanged();
        }
    }

    private List<BatSummary> _batSummaryList = new List<BatSummary>();

    private List<BatSummary> BatSummaryList 
    {
        get { return _batSummaryList; }
        set { _batSummaryList = value; }
    }

    public RecordingEx() : base()
    {
    }

    public RecordingEx(RecordingTable rt) : base()
    {
        this.ID = rt.ID;
        this.RecordingName = rt.RecordingName;
        this.RecordingStartTime = rt.RecordingStartTime;
        this.RecordingEndTime = rt.RecordingEndTime;
        this.RecordingGPSLongitude = rt.RecordingGPSLongitude;
        this.RecordingGPSLatitude = rt.RecordingGPSLatitude;
        this.RecordingNotes = rt.RecordingNotes;
        this.RecordingDate = rt.RecordingDate;
        this.SessionID = rt.SessionID;
        
    }

    public RecordingTable GetTable()
    {
        RecordingTable rt = new RecordingTable();
        rt.ID = this.ID;
        rt.RecordingName = this.RecordingName;
        rt.RecordingStartTime = this.RecordingStartTime;
        rt.RecordingEndTime = this.RecordingEndTime;
        rt.RecordingGPSLongitude = this.RecordingGPSLongitude;
        rt.RecordingGPSLatitude = this.RecordingGPSLatitude;
        rt.RecordingNotes = this.RecordingNotes;
        rt.RecordingDate = this.RecordingDate;
        rt.SessionID = this.SessionID;
        return rt;
    }

    private async void GetRecBatSummary()
    {
        if (string.IsNullOrWhiteSpace(BatSummaryString))
        {
            BatSummaryString = await GetRecBatSummaryAsync();
        }
    }

    public async Task<string> GetRecBatSummaryAsync()
    {
        if(!string.IsNullOrWhiteSpace(BatSummaryString))
        {
            return BatSummaryString;
        }
        string result = "";
        BatSummaryList = await GetRecBatSummariesAsync();

        //result = "";
        foreach (var summary in BatSummaryList)
        {
            result += summary.ToString() + "\n";
        }
        Debug.WriteLine($"\t\tsummary={result}");
        return result;

    }

    public async Task<List<BatSummary>> GetRecBatSummariesAsync(bool force=false)
    {
        if(BatSummaryList?.Any()??false)
        {
            if(!force)
                return BatSummaryList;
        }
        List<BatSummary> batSummaryList = new List<BatSummary>();
        if (LabelledSegments.Count <= 0)
        {
            await RefreshLabelledSegments();
        }
        foreach (var seg in LabelledSegments)
        {
            batSummaryList.AddRange(seg?.BatSummaryList ?? new List<BatSummary>());
        }
        batSummaryList = CondenseBatSummaries(batSummaryList);
        return batSummaryList;
    }

    public static List<BatSummary> CondenseBatSummaries(List<BatSummary> batSummaryList)
    {
        var result = new List<BatSummary>();
        var uniqueBats = batSummaryList.Select(bs => bs.BatName).Distinct();
        foreach (var bat in uniqueBats)
        {
            var summary = new BatSummary();
            summary.BatName = bat;
            summary.BatDuration = TimeSpan.FromMilliseconds(batSummaryList.Where(bs => bs.BatName == bat && !bs.ByAutoId).Select(bs => bs.BatDuration.TotalMilliseconds).Sum());
            summary.NumSegments = batSummaryList.Where(bs => bs.BatName == bat && !bs.ByAutoId).Count();
            summary.ByAutoId = false;
            result.Add(summary);
        }
        return result;
    }

    /// <summary>
    /// Given a filename with path finds all the labelled segments for this recording and adds
    /// them to the LabelledSegments List.  Initially looks for .txt sidecar file for the current
    /// sound file.  Does NOT look at metadata for IDs, those are dealt with elsewhere
    /// </summary>
    /// <param name="wavFile"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task UpdateLabelledSegmentsAsync(FileInfo wavFile, DateTime fileStart, DateTime fileEnd, string note)
    {
        //Debug.WriteLine("UpdateLabelledSegmentAsync");
        string path = Path.GetDirectoryName(wavFile.FullName) ?? "";
        string file = wavFile.Name;
        string fqTextFile = Path.Combine(path ?? "", Path.ChangeExtension(file, ".txt"));

        if (File.Exists(fqTextFile))
        {
            await CreateLabelledSegmentsAsync(fqTextFile);
            //Debug.WriteLine($"\tCreated {this.LabelledSegments.Count} segments from {fqTextFile}");

        }
        else
        {
            // assume one labelled segment per file
            //var segment = new LabelledSegment();
            //segment.StartOffset = new DateTime();
            //segment.EndOffset = segment.StartOffset + (fileEnd - fileStart);
            //segment.Comment = note;
            var segment = await ProcessLabelledSegmentAsync(note);
            this.LabelledSegments.Add(segment);
            //Debug.WriteLine($"\tnew labelled segment from {segment.Comment}");
        }
    }

    /// <summary>
    ///     Processes the labelled segment. Accepts a processed segment comment line consisting
    ///     of a start offset, end offset, duration and comment string and generates a new
    ///     Labelled segment instance and BatSegmentLink instances for each bat represented in
    ///     the Labelled segment. The instances are merged into a single instance of
    ///     CombinedSegmentAndBatPasses to be returned. If the line to be processed is not in the
    ///     correct format then an instance containing an empty LabelledSegment instance and an
    ///     empty List of ExtendedBatPasses. The comment section is checked for the presence of a
    ///     call parameter string and if present new Call is created and populated.
    /// </summary>
    /// <param name="processedLine">
    ///     The processed line.
    /// </param>
    /// <param name="bats">
    ///     The bats.
    /// </param>
    /// <returns>
    /// </returns>
    /// <exception cref="System.NotImplementedException">
    /// </exception>
    public static async Task<LabelledSegmentEx?> ProcessLabelledSegmentAsync(string processedLine)
    {
        //Debug.WriteLine("ProcessLabelledSegmentAsync");
        LabelledSegmentEx? segment = null;
        DateTime start = new DateTime();
        DateTime end = new DateTime();
        string comment = "";
        try
        {
            var parts = processedLine.Split(new char[] { ' ', '\t' }, 3);
            if (parts.Length > 0)
            {
                if (float.TryParse(parts[0], out float value))
                {
                    start += TimeSpan.FromSeconds(value > 0.0f ? value : 0.0f);
                }
                else
                {
                    if (TimeSpan.TryParse(parts[0], out TimeSpan tsValue))
                    {
                        start += tsValue;
                    }
                }
            }
            if (parts.Length > 1)
            {
                if (float.TryParse(parts[1], out float value))
                {
                    end += TimeSpan.FromSeconds(value > 0.0f ? value : 0.0f);
                }
                else
                {
                    if (TimeSpan.TryParse(parts[1], out TimeSpan tsValue))
                    {
                        end += tsValue;
                    }
                }
            }
            for (int i = 2; i < parts.Length; i++)
            {
                comment += parts[i];
            }
            segment = new LabelledSegmentEx();
            segment.StartOffset = start;
            segment.EndOffset = end;
            segment.Comment = comment;
            Debug.WriteLine($"ProcessLabelledSegment {comment}");
            var idedBatList = await segment.GetDescribedBatsAsync();
            segment.IdedBats = idedBatList.batList;
            segment.Comment = idedBatList.moddedDescription;
            Debug.WriteLine($"bat list of {segment.IdedBats.Count}");
            Debug.WriteLine($"Modded comment={segment.Comment}");
            foreach (var bat in segment.IdedBats)
            {
                Debug.WriteLine($"Ided {bat.Name} for segment {bat.SegmentID}");
            }



            /*
            var match = Regex.Match(processedLine,
                "([0-9\\.\\']+)[\\\"]?\\s*[-\t]?\\s*([0-9\\.\\']+)[\\\"]?\\s*[=\t]\\s*([0-9\\.\']+)[\\\"]?\\s*(.+)");
            //e.g. (123'12.3)" - (123'12.3)" = (123'12.3)" (other text)
            // Actually need to match 123.456\t234.789\tcomment with embedded spaces
            if (match.Success)
                //int passes = 1;
                // The line structure matches a labelled segment
                if (match.Groups.Count > 3)
                {
                    segment = new LabelledSegment();
                    segment.Comment = match.Groups[4].Value;

                    var ts = Tools.TimeParse(match.Groups[2].Value);
                    segment.EndOffset = new DateTime() + ts;
                    var es = Tools.TimeParse(match.Groups[1].Value);
                    segment.StartOffset = new DateTime() + es;

                    segment.IdedBats = segment.GetDescribedBatsAsync();



                }*/
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"ERR in ProcessLabelledSegmentAsync {ex.Message}");
        }

        return segment;
    }

    /// <summary>
    /// Goes through a sidecar text file creating a labelled segment for each line and adding the
    /// LabelledSegment to the list.
    /// </summary>
    /// <param name="fqTextFile"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task CreateLabelledSegmentsAsync(string fqTextFile)
    {
        //Debug.WriteLine("CreateLabelledSegmentsAsync");
        var lines = File.ReadAllLines(fqTextFile);
        for (int i = 0; i < lines.Count(); i++)
        {
            string line = lines[i];
            if (line.StartsWith(@"\")) continue;
            if (i < lines.Count() - 1 && lines[i + 1].StartsWith(@"\"))
            {
                line = line + lines[i + 1].Substring(1);
            }
            var modline = Regex.Replace(line, @"[Ss][Tt][Aa][Rr][Tt]", "0.0");
            modline = Regex.Replace(modline, @"[Ee][Nn][Dd]", ((decimal)RecordingDuration.TotalSeconds).ToString());

            var segment = await ProcessLabelledSegmentAsync(modline);
            if (segment != null)
            {
                LabelledSegments.Add(segment);
                //Debug.WriteLine($"Added segment {segment.Comment} with {segment.IdedBats.Count} ids");
            }
        }

    }




    private async Task<(List<IdedBatTable>, string newmodline)> GetIdedBats(string modline, int segId)
    {
        string newmodline = "";
        string autoIds = "";
        string fullAutoIds = "";
        string braces = "";
        List<IdedBatTable> result = new List<IdedBatTable>();
        int lbPos = modline.IndexOf('{');
        if (lbPos >= 0)
        { // remove subcomment in curly braces
            braces = modline.Substring(lbPos);
            modline = modline.Substring(0, lbPos);
        }
        lbPos = modline.IndexOf('(');
        if (lbPos >= 0)
        { // remove autoId in brackets
            autoIds = modline.Substring(lbPos);
            fullAutoIds = autoIds;
            modline = modline.Substring(0, lbPos);
        }
        newmodline = modline;
        var bats = await DBAccess.GetAllBatsAsync();
        Dictionary<BatTable, string> batTags = new Dictionary<BatTable, string>();
        foreach (var bat in bats)
        {
            foreach (var tag in bat.BatTags ?? new List<BatTag>())
            {
                batTags.Add(bat, tag.Tag);
            }
        }
        batTags = (from bt in batTags
                   orderby bt.Value.Length
                   select bt).ToDictionary();
        foreach (var tag in batTags)
        {
            if (modline.Contains(tag.Value))
            {
                IdedBatTable idedBat = new IdedBatTable();
                idedBat.Name = tag.Key.Name;
                idedBat.ByAutoId = false;
                idedBat.SegmentID = segId;
                modline = modline.Replace(tag.Value, "");
                newmodline = newmodline.Replace(tag.Value, tag.Key.Name);
            }
            if (autoIds.Contains(tag.Value))
            {
                IdedBatTable idedBat = new IdedBatTable();
                idedBat.Name = tag.Key.Name;
                idedBat.ByAutoId = true;
                idedBat.SegmentID = segId;
                autoIds = autoIds.Replace(tag.Value, "");
            }
        }
        result = result.Distinct().ToList();
        newmodline += fullAutoIds + braces;




        return ((result, newmodline));
    }

    public String DisplayText
    {
        get { return CreateDisplayText(); }
    }



    public Color DisplayTextColor
    {
        get
        {
            if (File.Exists(this.FullRecordingName() ?? ""))
            {
                return Colors.Black;
            }
            else
            {
                return Colors.Red;
            }
        }
    }
    private string? FullRecordingName()
    {
        RecordingSessionTable session = Task.Run(() => DBAccess.GetSessionAsync(SessionID)).Result;
        if (session != null)
        {
            return Path.Combine(session.OriginalFilePath, RecordingName);
        }
        else
        {
            return RecordingName;
        }

    }

    public string CreateDisplayText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{RecordingName} {RecordingDate.Date + RecordingStartTime.TimeOfDay} For {RecordingDuration.TotalSeconds}s");
        foreach (var meta in Metas)
        {
            sb.AppendLine($"{meta.Label}:- {meta.Value}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Reads the metadata section or sections from the .wav file, puts all the metadata elements in a list
    /// and sets the Metadata list in this Recording
    /// </summary>
    /// <param name="wavFile"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void GetMetaDataFromFile(FileInfo wavFile, out WavFileMetaData wfmd)
    {
        var mdList = new List<Meta>();

        wfmd = new WavFileMetaData(wavFile.FullName);
        mdList = wfmd.metaData;


        Metas = mdList;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetFile()
    {
        var file = RecordingName;
        var session = await DBAccess.GetSessionAsync(SessionID);
        file = Path.Combine(session.OriginalFilePath, file);
        return file;
    }

    /// <summary>
    /// Returns a list, one item for each labelled segment, the items contain a string
    /// identifying all Ided bats in the segment and the start and end offsets in the
    /// segment
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task<List<LabelItem>> GetLabelList()
    {
        List<LabelItem> list = new List<LabelItem>();
        var segments = await DBAccess.GetSegmentsForRecordingAsync(ID);
        foreach (var segment in segments)
        {
            var label = await segment.GetIdedBatsString();
            double start = segment.StartOffsetTimeSpan.TotalSeconds;
            double end = segment.EndOffsetTimeSpan.TotalSeconds;
            LabelItem item = new LabelItem(label, start, end);
            if (!string.IsNullOrWhiteSpace(label))
            {
                list.Add(item);
            }
        }
        return list;

    }

    internal object GetBatSummaries()
    {
        throw new NotImplementedException();
    }
} 
