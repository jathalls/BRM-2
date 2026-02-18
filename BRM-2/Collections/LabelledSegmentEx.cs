using System.Xml.Serialization;

namespace BRM_2.Collections;
public class LabelledSegmentEx : LabelledSegmentTable
{

    [XmlArray("Calls")]
    [XmlArrayItem("Call")]
    public List<Call> Calls { get; set; } = new List<Call>();

    [XmlArray("BatIds")]
    [XmlArrayItem("IdedBats")]
    public List<IdedBatEx> IdedBats
    {
        get { return _idedBats; }
        set { _idedBats = value; }
    }

    private List<IdedBatEx> _idedBats = new List<IdedBatEx>();

     
    public string SegmentTextForDisplay
    {
        get
        {
            string text = $"{StartOffsetTimeSpan.TotalSeconds:0.00}s - {EndOffsetTimeSpan.TotalSeconds:0.00}s = {(EndOffsetTimeSpan - StartOffsetTimeSpan).TotalSeconds:0.00}s; {Comment}";
            return text;
        }
    }

    [XmlAttribute("EndOffset")]
    public TimeSpan EndOffsetTimeSpan
    {
        get { return EndOffset.TimeOfDay; }
        set { EndOffset = new DateTime() + value; }
    }

    [XmlAttribute("StartOffset")]
    public TimeSpan StartOffsetTimeSpan
    {
        get { return StartOffset.TimeOfDay; }
        set { StartOffset = new DateTime() + value; }
    }

     
    public List<BatSummary> BatSummaryList
    {
        get
        {
            
            return _batSummaryList;
        }

        set
        {
            _batSummaryList = value;
        }
    }

    private List<BatSummary> _batSummaryList = new List<BatSummary>();

    public LabelledSegmentEx() : base()
    {
        Calls = new List<Call>();
        IdedBats = new List<IdedBatEx>();
    }

    public LabelledSegmentEx(LabelledSegmentTable lst) : base()
    {
        ID = lst.ID;
        RecordingID = lst.RecordingID;
        StartOffset = lst.StartOffset;
        EndOffset = lst.EndOffset;
        Comment = lst.Comment;
        AutoID = lst.AutoID;
        AutoIdProb = lst.AutoIdProb;
        Calls = new List<Call>();
        IdedBats = new List<IdedBatEx>();
        GetSegBatSummary();
    }

    public LabelledSegmentTable GetTable()
    {
        var lst = new LabelledSegmentTable();
        lst.ID = ID;
        lst.RecordingID = RecordingID;
        lst.StartOffset = StartOffset;
        lst.EndOffset = EndOffset;
        lst.Comment = Comment;
        lst.AutoID = AutoID;
        lst.AutoIdProb = AutoIdProb;
        return lst;
    }

    private async void GetSegBatSummary()
    {
        BatSummaryList = await GetSegBatSummaryAsync();
    }

    public async Task<List<BatSummary>> GetSegBatSummaryAsync(bool force=false)
    {
        if(BatSummaryList?.Any()??false)
        {
            if (!force)
            {
                return BatSummaryList;
            }
        }
        //var result=DBAccess.GetBatSummaryForSegment(ID).Result;
        if (IdedBats == null || IdedBats.Count == 0)
        {
            IdedBats = await DBAccess.GetIdedBatsForSegmentAsync(ID);
            if (IdedBats == null || IdedBats.Count == 0)
            {
                BatSummaryList = new List<BatSummary>();
                return new List<BatSummary>();
            }
        }
        // if we get here there are bats in the IdedBats list
        var list = new List<BatSummary>();
        foreach (var bat in IdedBats)
        {
            BatSummary batSummary = new BatSummary();
            batSummary.BatDuration = EndOffsetTimeSpan - StartOffsetTimeSpan;
            batSummary.BatName = bat.Name;
            batSummary.NumSegments = 1;
            batSummary.ByAutoId = bat.ByAutoId;
            list.Add(batSummary);
        }
        return list;

    }


    /// <summary>
    /// Analyses the comment for any bat names and sets the IdedBats list in this segment
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>

    internal async Task<(List<IdedBatEx> batList, string moddedDescription)> GetDescribedBatsAsync(bool force=true)
    {
        if(IdedBats?.Any()??false)
        {
            if (!force)
            {
                return (IdedBats, Comment);
            }
        }
        //Debug.WriteLine("GetDescribedBatsAsync");
        var matchingBats = new List<IdedBatEx>();
        var bracketed = "";
        var result = new List<IdedBatEx>();
        var moddedDescription = Comment;
        //Debug.WriteLine($"\tget bats from <{moddedDescription}>");
        if (string.IsNullOrWhiteSpace(Comment))
        {
            var nobat = new IdedBatEx();
            nobat.ByAutoId = false;
            matchingBats.Add(nobat);

            IdedBats = matchingBats;
            return (IdedBats, moddedDescription);
        }



        string description = Comment;
        string autoDescription = "";

        var len = Comment.IndexOf('{');
        if (len >= 0)
        {
            bracketed = Comment.Substring(len).Trim();
            description = description.Substring(0, len);
        } // we have removed all text from { to the end to eliminate bracketed text

        var autoIDstart = description.IndexOf("(Auto");
        var autoEnd = description.IndexOf(")");
        if (autoIDstart >= 0 && autoEnd >= 0)
        {
            var autoLength = autoEnd - autoIDstart;
            autoDescription = description.Substring(autoIDstart, autoLength);
            description = description.Substring(0, autoIDstart);
        } // we have separated the main description and the first AuotID (bracketed) section
        var manualIds = await GetIdedBatsAsync(description, AutoID: false);
        result.AddRange(manualIds.batList);
        var autoIds = await GetIdedBatsAsync(autoDescription, AutoID: true);
        result.AddRange(autoIds.batList);

        moddedDescription = manualIds.moddedDescription + autoDescription + bracketed;
        return (result, moddedDescription);

    }

    /// <summary>
    /// Locates the described bats in the string by tag or by name or by latin name (genus+species)
    /// </summary>
    /// <param name="description"></param>
    /// <param name="AutoID"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<(IEnumerable<IdedBatEx> batList, string moddedDescription)> GetIdedBatsAsync(string description, bool AutoID)
    {
        //Debug.WriteLine("GetIdedBatsAsync");
        string moddedDescription = description;
        List<IdedBatEx> result = new List<IdedBatEx>();
        List<BatTag> containedTags = new List<BatTag>();
        containedTags = await DBAccess.GetContainedTagsAsync(description);// returns contained tags sorted longest to shortest
        string udescription = description.ToUpper();
        foreach (var tag in containedTags)
        {
            string desc = description;
            if (IsUpperCase(tag.Tag))
            {
                desc = udescription;
            }
            var loc = desc.IndexOf(tag.Tag);
            if (loc >= 0)
            {
                //Debug.WriteLine($"found {tag.Tag} in {desc}");
                var bat = await DBAccess.GetBatByIDAsync(tag.BatID);
                var iBat = new IdedBatEx();
                iBat.Name = (bat?.Name) ?? "";
                iBat.ByAutoId = AutoID;
                result.Add(iBat);
                moddedDescription = moddedDescription.Replace(tag.Tag, iBat.Name);
                if (description.Length > tag.Tag.Length)
                {
                    //Debug.WriteLine($"d=<{description}> t=<{tag.Tag}> loc={loc} tagLength={tag.Tag.Length}" );
                    description = description.Substring(0, loc) + description.Substring(loc + tag.Tag.Length - 1);
                }
                else
                {
                    description = "";
                }
                udescription = description.ToUpper();
                //udescription = description.Substring(0, loc) + description.Substring(loc + tag.Tag.Length-1);
                //Debug.WriteLine($"\tfound bat {iBat.Name}");

                continue;
            }

        }

        return (result, moddedDescription);
    }

    private bool IsUpperCase(string s)
    {
        if (s == s.ToUpper()) return true;
        return false;
    }


    /// <summary>
    /// Looks in the comment for a bracketed section of text containing a bat name and probability
    /// and if found sets the AutoID and AutoId Prob to those values
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    internal void GetAutoIdFromComment()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the fully qualified file name for the recording containing this segment
    /// does not check for the existence of the file or the path
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task<string> GetFile()
    {
        string result = "";
        var rec = await DBAccess.GetRecordingAsync(RecordingID);
        result = await rec.GetFile();


        return result;
    }

    /// <summary>
    /// Saves this segment of the specified recording to a new file created for the purpose, and returns the path and name of the new file
    /// </summary>
    /// <param name="recording"></param>
    /// <returns>the fully qualified name of the new file</returns>
    public async Task<string> Save(RecordingEx recording)
    {
        if (recording == null || recording.ID != RecordingID)
        {
            Debug.WriteLine($"LabelledSegment.Save: getting recording with ID {RecordingID}");
            recording = await DBAccess.GetRecordingAsync(RecordingID);
        }
        if (recording == null)
        {
            Debug.WriteLine($"LabelledSegment.Save: no recording with ID {RecordingID}");
            return "";
        }
        string folder = "";
        try
        {
            Debug.WriteLine($"LabelledSegment.Save: getting file for recording ID {RecordingID}");
            folder = await recording.GetFile();
            if (!File.Exists(folder))
            {
                Debug.WriteLine($"LabelledSegment.Save: no file {folder}");
                return "";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LabelledSegment.Save: exception getting file for recording ID {RecordingID}: {ex.Message}");
            return "";
        }
        Debug.WriteLine($"LabelledSegment.Save: copying segment {ID} from file {folder}");
        var destination = await CopyFile(folder);
        return destination ?? "";

    }

    /// <summary>
    /// given a fully qualified .wav file name, creates a destination directory and copies the current segment
    /// to a new file in that directory.  Returns the fully qualified file name of the new file
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<string> CopyFile(string? folder)
    {
        string? path = Path.GetDirectoryName(folder ?? "") ?? "";
        string? fileName = Path.GetFileName(folder);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length < 5)
        {
            Debug.WriteLine($"LabelledSegment.CopyFile: invalid file name {fileName}");
            return "";
        }

        string newFolder = Path.Combine(path, fileName.Substring(0, fileName.Length - 4));
        string baseFolder = newFolder;

        Debug.WriteLine($"LabelledSegment.CopyFile: creating directory {newFolder}");
        if (!Directory.Exists(newFolder))
        {
            Directory.CreateDirectory(newFolder);
        }
        string newFileName = $"{fileName.Substring(0, fileName.Length - 4)}_{(int)StartOffsetTimeSpan.TotalSeconds}.wav";
        string newFQFile = Path.Combine(newFolder, newFileName);
        Debug.WriteLine($"LabelledSegment.CopyFile: creating file {newFQFile}");
        if (File.Exists(newFQFile))

        {
            var bakFile = Path.ChangeExtension(newFQFile, ".bak");
            if (File.Exists(bakFile)) File.Delete(bakFile);
            File.Copy(newFQFile, bakFile);
            File.Delete(newFQFile);
        }
        Debug.WriteLine($"LabelledSegment.CopyFile: extracting segment {ID} to file {newFQFile}");
        await ExtractWavSegmentAsync(folder, newFQFile, StartOffsetTimeSpan, EndOffsetTimeSpan);
        if (File.Exists(newFQFile))
        {
            Debug.WriteLine($"LabelledSegment.CopyFile: created file {newFQFile}");
            return (newFQFile);
        }
        else
        {
            return "";
        }
    }

    /*
    public static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan endFromStart)
    {
        using (WaveFileReader reader = new WaveFileReader(inPath))
        {
            using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
            {
                int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                int endPos = (int)endFromStart.TotalMilliseconds * bytesPerMillisecond;
                endPos = endPos - endPos % reader.WaveFormat.BlockAlign;
                

                TrimWavFile(reader, writer, startPos, endPos);
            }
        }
    }

    private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
    {
        reader.Position = startPos;
        byte[] buffer = new byte[1024];
        while (reader.Position < endPos)
        {
            int bytesRequired = (int)(endPos - reader.Position);
            if (bytesRequired > 0)
            {
                int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                int bytesRead = reader.Read(buffer, 0, bytesToRead);
                if (bytesRead > 0)
                {
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
    }*/


    /// <summary>
    /// Copies source file 'folder' to new file 'newFQFile' starting at startOffsetTimeSpan and ending at endOffsetTimeSpan
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="newFQFile"></param>
    /// <param name="startOffsetTimeSpan"></param>
    /// <param name="endOffsetTimeSpan"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task ExtractWavSegmentAsync(string? folder, string newFQFile, TimeSpan startOffsetTimeSpan, TimeSpan endOffsetTimeSpan)
    {
        if (!string.IsNullOrWhiteSpace(folder) && File.Exists(folder))
        {
            try
            {
                Debug.WriteLine($"[ExtractWavSegmentAsync] Extracting segment from {folder} to {newFQFile}");
                await CopyWavSegmentAsync(folder, newFQFile, startOffsetTimeSpan, endOffsetTimeSpan);
                Debug.WriteLine($"[ExtractWavSegmentAsync] Segment extracted successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExtractWavSegmentAsync] Error: {ex.Message}");
                throw;
            }
        }
        else
        {
            Debug.WriteLine($"[ExtractWavSegmentAsync] File does not exist: {folder}");
        }
    }

    /// <summary>
    /// Inserts an autoID summary into the comment and updates the database record
    /// </summary>
    /// <param name="result"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task InsertSummary(string result)
    {
        string subcomment = "";
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }
        if (Comment.Contains("{"))
        {
            var len = Comment.IndexOf('{');
            subcomment = Comment.Substring(len).Trim();
            Comment = Comment.Substring(0, len).Trim();

        }
        if (Comment.Contains("("))
        {
            var blen = Comment.IndexOf('(');
            Comment = Comment.Substring(0, blen).Trim();
        }
        Comment = $"{Comment} (AutoId: {result}){subcomment}";
        await DBAccess.UpdateLabelledSegment(this);
    }

    /// <summary>
    /// For each ided bat, adds a 4 letter code for the bat and adds it to the string
    /// that is returned seperated by commas
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task<string> GetIdedBatsString()
    {
        string result = "";
        var IdedBats = await DBAccess.GetIdedBatsForSegmentAsync(ID);
        foreach (var bat in IdedBats ?? new List<IdedBatEx>())
        {
            string batCode = await bat.GetLabel();
            if (!result.Trim().EndsWith(',')) result += ", ";
            result += batCode;

        }
        return result;
    }

    /// <summary>
    /// Asynchronously copies a segment of a WAV file from start time to end time without using NAudio
    /// </summary>
    /// <param name="inFile">Source WAV file path</param>
    /// <param name="outFile">Destination WAV file path</param>
    /// <param name="start">Start time offset</param>
    /// <param name="end">End time offset</param>
    public static async Task CopyWavSegmentAsync(string inFile, string outFile, TimeSpan start, TimeSpan end)
    {
        if (!File.Exists(inFile))
        {
            Debug.WriteLine($"[CopyWavSegmentAsync] Source file not found: {inFile}");
            throw new FileNotFoundException($"Source file not found: {inFile}");
        }

        try
        {
            await Task.Run(() => CopyWavSegment(inFile, outFile, start, end));
            Debug.WriteLine($"[CopyWavSegmentAsync] Successfully created segment: {outFile}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CopyWavSegmentAsync] Error copying WAV segment: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Copies a segment of a WAV file by directly manipulating the WAV format structure
    /// </summary>
    private static void CopyWavSegment(string inFile, string outFile, TimeSpan start, TimeSpan end)
    {
        using (FileStream inputStream = File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (FileStream outputStream = File.Create(outFile))
        using (BinaryReader reader = new BinaryReader(inputStream))
        using (BinaryWriter writer = new BinaryWriter(outputStream))
        {
            // Validate and read RIFF header
            if (new string(reader.ReadChars(4)) != "RIFF")
                throw new InvalidOperationException("Invalid WAV file: missing RIFF header");

            int riffSize = reader.ReadInt32();
            if (new string(reader.ReadChars(4)) != "WAVE")
                throw new InvalidOperationException("Invalid WAV file: missing WAVE header");

            // Parse chunks to find fmt and data
            int sampleRate = 0;
            int channels = 0;
            int bytesPerSample = 0;
            long dataStartPos = 0;
            int dataSize = 0;

            while (inputStream.Position < inputStream.Length)
            {
                string chunkId = new string(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    short audioFormat = reader.ReadInt16();
                    if (audioFormat != 1) // PCM only
                        throw new InvalidOperationException("Only PCM WAV files are supported");

                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    int byteRate = reader.ReadInt32();
                    short blockAlign = reader.ReadInt16();
                    short bitsPerSample = reader.ReadInt16();
                    bytesPerSample = bitsPerSample / 8;

                    inputStream.Seek(chunkSize - 16, SeekOrigin.Current);
                }
                else if (chunkId == "data")
                {
                    dataStartPos = inputStream.Position;
                    dataSize = chunkSize;
                    break;
                }
                else
                {
                    inputStream.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            if (sampleRate == 0)
                throw new InvalidOperationException("Invalid WAV file: missing fmt chunk");
            if (dataStartPos == 0)
                throw new InvalidOperationException("Invalid WAV file: missing data chunk");

            // Calculate byte positions based on time offsets
            int bytesPerSecond = sampleRate * channels * bytesPerSample;
            int startBytes = (int)(start.TotalSeconds * bytesPerSecond);
            int endBytes = (int)(end.TotalSeconds * bytesPerSecond);

            // Align to sample boundary (important for audio quality)
            int sampleBlockSize = channels * bytesPerSample;
            startBytes = startBytes - startBytes % sampleBlockSize;
            endBytes = endBytes - endBytes % sampleBlockSize;

            int segmentSize = Math.Max(0, endBytes - startBytes);

            // Write output WAV header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + segmentSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // Write fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(bytesPerSecond);
            writer.Write((short)sampleBlockSize);
            writer.Write((short)(bytesPerSample * 8));

            // Write data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(segmentSize);

            // Copy audio segment
            inputStream.Seek(dataStartPos + startBytes, SeekOrigin.Begin);
            byte[] buffer = new byte[4096];
            int bytesRemaining = segmentSize;

            while (bytesRemaining > 0)
            {
                int bytesToRead = Math.Min(bytesRemaining, buffer.Length);
                int bytesRead = inputStream.Read(buffer, 0, bytesToRead);
                if (bytesRead <= 0) break;

                writer.Write(buffer, 0, bytesRead);
                bytesRemaining -= bytesRead;
            }
        }
    }
}
