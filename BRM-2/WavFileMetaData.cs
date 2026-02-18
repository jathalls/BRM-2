using Encoding=System.Text.Encoding;
namespace BRM_2;
internal class WavFileMetaData
{
    /// <summary>
    ///     Constructor for WavFileMetaData.  Given a path to a .wav file reads the WAMD or
    ///     GUANO metdata from that file and uses it and other file information to populate the
    ///     appropriate data fields.
    /// </summary>
    /// <param name="filename"></param>
    public WavFileMetaData(string filename)
    {
        // set a default start date as the file creation or modified date
        try
        {
            m_Location = new GPSLocation(0.0, 0.0);
            if (!File.Exists(filename))
            {
                success = false;
                return;
            }
            if (m_Start == null)
            {
                FileInfo? info = null;
                try
                {
                    info = new FileInfo(filename);
                }
                catch (IOException iox)
                {
                    //Debug.WriteLine($"IO- {iox.Message}-{iox.HResult}");
                }
                if ((info?.Length ?? 0) > 0)
                {
                    var duration = Tools.GetFileDatesAndTimes(filename, out string wavfile, out DateTime fileStart, out DateTime fileEnd);

                    m_Start = fileStart;
                    m_Created = fileStart;
                    m_End = fileEnd;

                    /*if (DBAccess.GetDateTimeFromFilename(filename, out var dt))
                    {
                        m_Start = dt;
                    }
                    else
                    {
                        m_Start = File.GetCreationTime(filename);
                        m_Created = m_Start;
                        if (m_Start == null || m_Start.Value.Year < 1960) m_Start = File.GetLastWriteTime(filename);
                    }*/ //replaced with the four lines above 23/7/2020
                    Read_MetaData(filename);
                    if (rawMetadata != null) success = true;
                }
            }



            if (m_Start != null && m_Duration != null && m_End == null) m_End = m_Start + m_Duration;
            m_Location = new GPSLocation(m_Start ?? DateTime.Now, filename);
        }
        catch (Exception ex)
        {
            Tools.ErrorLog(ex.Message);
            //Debug.WriteLine("Error in WavFileMetaData:- " + ex.Message);
            m_Note = "Metadata not read\n";
            success = false;
        }
    }

    public enum JsonState { INKEY, INBAREVALUE, INBRACKETEDSTRINGVALUE, ENDOFLINE };

    /// <summary>
    ///     The SpeciesProbabilityList of any Auto-identification as a string
    /// </summary>
    public string m_AutoID { get; private set; } = "";

    /// <summary>
    ///     The name of the recording device
    /// </summary>
    public string m_Device { get; private set; }

    /// <summary>
    ///     The duration of the .wav file calculated from the file header information
    /// </summary>
    public TimeSpan? m_Duration { get; private set; }

    /// <summary>
    ///     The end date and time for the file extracted from the metadata or by taking the start
    ///     Date and time and adding the file duration
    /// </summary>
    public DateTime? m_End { get; private set; }

    /// <summary>
    ///     the fully qualified name of the file from which the data was extracted
    /// </summary>
    public string m_FileName { get; private set; }

    /// <summary>
    ///     The location for the recording as GPS latitude and longitude as a pair of doubles
    /// </summary>
    public GPSLocation m_Location { get; private set; }

    /// <summary>
    ///     The string containing the manual identification from the Metadata
    /// </summary>
    public string m_ManualID { get; private set; } = "";

    /// <summary>
    ///     The type of microphone used for the recording
    /// </summary>
    public string m_Microphone { get; private set; }

    /// <summary>
    ///     The contents of the notes field of the metadata
    /// </summary>
    public string m_Note { get; private set; } = "";

    /// <summary>
    ///     The software used for the analysis
    /// </summary>
    public string m_Software { get; private set; }

    /// <summary>
    ///     The start time and date for the wavfile extracted from the filename or metadata, or if that
    ///     is not available from the filename, or failing that the file creation date and time
    ///     or the file modified date and time
    /// </summary>
    public DateTime? m_Start { get; private set; } = null;

    /// <summary>
    /// for Recordings on Bat Recorder, the host device
    /// </summary>
    public string m_HostDevice { get; private set; }

    /// <summary>
    /// for Recordings on bat recorder, the host OS
    /// </summary>
    public string m_HostOS { get; private set; }

    /// <summary>
    ///     The temperature at the time of the recording
    /// </summary>
    public string m_Temperature { get; private set; }

    public List<Meta> metaData { get; private set; } = new List<Meta>();
    public bool success { get; set; }

    internal string FormattedText()
    {
        var text = "";
        if (!string.IsNullOrWhiteSpace(Source))
            text += "[ " + Source + " metadata:-";
        else
            text += "[ Metadata:-";
        if (m_FileName != null) text += "\n" + m_FileName;
        if (m_Start != null)
            text += "\n" + m_Start.Value.ToShortDateString() + " " + m_Start.Value.ToLongTimeString();
        if (m_End != null) text += " - " + m_End.Value.ToShortDateString() + " " + m_End.Value.ToLongTimeString();
        if (m_Duration != null) text += "\nFile Duration = " + m_Duration.Value.TotalSeconds + " s";

        if (m_Location != null && !string.IsNullOrWhiteSpace(m_Location.m_Name))
        {
            text += "\n" + m_Location.m_Name;
            if (m_Location != null && !string.IsNullOrWhiteSpace(m_Location.m_ID))
                text += " (" + m_Location.m_ID + ")";
        }

        if (m_Location != null && GpxHandler.IsValidLocation((decimal)m_Location.m_Latitude, (decimal)m_Location.m_Longitude))
            text += "\nGPS:- " + m_Location.m_Latitude + ", " + m_Location.m_Longitude;
        if (m_Device != null) text += "\nDevice:- " + m_Device;
        if (m_Microphone != null) text += "\nMic:- " + m_Microphone;
        if (m_Temperature != null) text += "\nTemp:- " + m_Temperature;
        if (m_Software != null) text += "\nAnalysed with:- " + m_Software;
        if (m_Note != null) text += "\n    " + m_Note;
        if (!string.IsNullOrWhiteSpace(infoString)) text += $"\nINFO:-\n{infoString}";
        text += "\n]\n";

        return text.Replace("\n\n", "\n").Trim();
    }

    /// <summary>
    ///     file creation date and time.  Start date is the earlier of this and the embedded metadata
    ///     date and time as that might be the analysed date rather than the collected date
    /// </summary>
    private DateTime? m_Created { get; }

    private DateTime? m_MetaDate { get; set; }
    private string m_Notes { get; set; } = "";
    private string Source { get; set; }
    private byte[]? rawMetadata { get; set; }

    public string infoString { get; set; } = "";

    /// <summary>
    ///     Given a 'chunk' of metadata from a wav file wamd chunk
    ///     which is everything after the wamd header and size attribute,
    ///     Returns true if any wamd data field is found and decoded
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    private bool decode_wamd_data(byte[] metadata)
    {
        var result = false;
        var entries = new Dictionary<short, string>();

        var bReader = new BinaryReader(new MemoryStream(metadata));

        while (bReader.BaseStream.Position < bReader.BaseStream.Length)
        {
            var type = bReader.ReadInt16(); // 01 00
            var size = bReader.ReadInt32(); // 03 00 00 00
            var bData = bReader.ReadBytes(size);
            if (type > 0)
                try
                {
                    var data = Encoding.UTF8.GetString(bData);
                    entries.Add(type, data);
                }
                catch (Exception ex)
                {
                    Tools.ErrorLog(ex.Message);
                    //Debug.WriteLine(ex);
                }
        }

        foreach (var entry in entries)
            switch (entry.Key)
            {
                case 0x000A:
                    m_Note += entry.Value + "; ";
                    m_Note = m_Note.Replace(@"\n", "");
                    var meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "Note", Value = m_Note };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x0005:
                    var dt = new DateTime();
                    if (DateTime.TryParse(entry.Value, out dt))
                    {
                        m_MetaDate = dt;
                        m_Start = m_Start != null ? dt < m_Start.Value && dt.Year > 1960 ? dt : m_Start : dt;

                        if (m_Duration != null) m_End = m_Start + m_Duration;
                        meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "StartDate", Value = m_Start.ToString() };
                        metaData.Add(meta);
                        result = true;
                    }

                    break;

                case 0x000C:
                    if (string.IsNullOrWhiteSpace(m_ManualID)) m_ManualID = "";
                    if (!m_ManualID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                    {
                        m_ManualID = (m_ManualID + " " + entry.Value).Trim();
                    }
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "ManualID", Value = m_ManualID.Trim() };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x000B:
                    if (string.IsNullOrWhiteSpace(m_AutoID)) m_AutoID = "";
                    if (!m_AutoID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                    {
                        m_AutoID = (entry.Value.Trim() + " " + m_AutoID).Trim();
                    }
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "AutoID", Value = m_AutoID.Trim() };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x000E: //AUTO_ID_STATS
                    if (string.IsNullOrWhiteSpace(m_AutoID)) m_AutoID = "";

                    m_AutoID = m_AutoID + ": " + entry.Value.Trim();
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "AutoIDStats", Value = entry.Value.Trim() };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x0006: // GPS_FIRST
                    if (!GPSLocation.IsValidLocation(m_Location?.m_Latitude ?? 0.0, m_Location?.m_Longitude ?? 0.0))
                    {
                        m_Location = new GPSLocation(entry.Value);
                    }
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "GPSLocation", Value = $"{m_Location?.m_Latitude??-200},{m_Location?.m_Longitude??-200}" };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x0001:
                    m_Device = entry.Value;
                    result = true;
                    if (entries.ContainsKey(0x0002)) m_Device = m_Device + " " + entries[0x0002];
                    if (entries.ContainsKey(0x0003)) m_Device = m_Device + " " + entries[0x0003];
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "Device", Value = m_Device };
                    metaData.Add(meta);
                    break;

                case 0x0012:
                    m_Microphone = entry.Value;
                    result = true;
                    if (entries.ContainsKey(0x0013)) m_Microphone = m_Microphone + " " + entries[0x0013];
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "Microphone", Value = m_Microphone };
                    metaData.Add(meta);
                    break;

                case 0x0015: //TEMP_INT
                    if (!string.IsNullOrWhiteSpace(m_Temperature)) m_Temperature = entry.Value;
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "InternalTemp", Value = entry.Value };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x0016: //TEMP_EXT
                    m_Temperature = entry.Value;
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "ExternalTemp", Value = entry.Value };
                    metaData.Add(meta);
                    result = true;
                    break;

                case 0x0008: //SOFTWARE
                    m_Software = entry.Value;
                    meta = new Meta() { ID = -1, RecordingID = -1, Type = "wamd", Label = "Software", Value = entry.Value };
                    metaData.Add(meta);
                    result = true;
                    break;
            }

        if (result)
        {
            if (string.IsNullOrWhiteSpace(Source))
                Source = "WAMD";
            else
                Source += " and WAMD";
        }

        return result;
    }

    /// <summary>
    ///     Given the GUANO data chunk from a .wav file, converted into a UTF-8 string,
    ///     parses that string for Guano data fields and uses them to populate the data
    ///     section of the class.
    /// </summary>
    /// <param name="metadataString"></param>
    /// <returns></returns>
    private bool DecodeGuanoData(string metadataString)
    {
        var result = false;
        var entries = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(metadataString))
        {
            var lines = metadataString.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var splitted = line.SplitOnFirst(':');
                    if (!string.IsNullOrWhiteSpace(splitted[0]) && !string.IsNullOrWhiteSpace(splitted[1]))
                    {
                        entries.Add(splitted[0], splitted[1]);
                    }
                }
            }

            //
            // Codes taken from https://github.com/riggsd/guano-spec/blob/master/guano_specification.md
            //
            foreach (var entry in entries)
                switch (entry.Key)
                {
                    case "Note":
                        m_Note += entry.Value + "; ";
                        m_Note = m_Note.Replace(@"\n", "");
                        metaData.Add(new Meta() { Type = "guan", Label = "Note", Value = m_Note });
                        result = true;
                        break;

                    case "Timestamp":
                        if (DateTime.TryParse(entry.Value, out var dt))
                        {
                            m_MetaDate = dt;
                            m_Start = m_Start != null ? dt < m_Start.Value && dt.Year > 1960 ? dt : m_Start : dt;

                            if (m_Duration != null) m_End = m_Start + m_Duration;
                            metaData.Add(new Meta() { Type = "guan", Label = "StartDate", Value = m_Start.ToString() });
                            result = true;
                        }

                        break;

                    case "Species Manual ID":
                        if (string.IsNullOrWhiteSpace(m_ManualID)) m_ManualID = "";
                        if (!m_ManualID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                        {
                            m_ManualID = (m_ManualID + " " + entry.Value).Trim();
                        }
                        metaData.Add(new Meta() { Type = "guan", Label = "ManualID", Value = m_ManualID.Trim() });
                        result = true;
                        break;

                    case "Species Auto ID":
                        if (string.IsNullOrWhiteSpace(m_AutoID)) m_AutoID = "";
                        if (!m_AutoID.Contains(entry.Value.Trim())) // no need to duplicate if this string already present
                        {
                            m_AutoID = (m_AutoID + " " + entry.Value).Trim();
                        }
                        metaData.Add(new Meta() { Type = "guan", Label = "AutoID", Value = m_AutoID.Trim() });
                        result = true;
                        break;

                    case "Loc Position":
                        if (!GpxHandler.IsValidLocation(m_Location))
                        {
                            var lat = 200.0f;
                            var longit = 200.0f;
                            var sections = entry.Value.Trim().Split(' ');
                            if (sections.Length > 1)
                            {
                                float.TryParse(sections[0], out lat);
                                float.TryParse(sections[1], out longit);
                                if (lat < 200.0f && longit < 200.0f)
                                {
                                    m_Location = new GPSLocation(lat, longit);
                                    result = true;
                                    metaData.Add(new Meta() { Type = "guan", Label = "GPSLocation", Value = $"{m_Location.m_Latitude},{m_Location.m_Longitude}" });
                                }
                            }
                        }
                        else result = true;

                        break;

                    case "Make":
                        m_Device = entry.Value;
                        if (entries.ContainsKey("Model")) m_Device = m_Device + " " + entries["Model"];
                        metaData.Add(new Meta() { Type = "guan", Label = "Device", Value = m_Device });
                        result = true;
                        break;

                    case "Original Filename":
                        m_FileName = entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "FileName", Value = entry.Value.Trim() });
                        result = true;
                        break;

                    case "Temperature Ext":
                        m_Temperature = entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "ExternalTemp", Value = entry.Value });
                        result = true;
                        break;

                    case "Temperature Int":
                        if (string.IsNullOrWhiteSpace(m_Temperature)) m_Temperature = entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "InternalTemp", Value = entry.Value });
                        result = true;
                        break;

                    case "WA|Kaleidoscope|Version":
                        m_Software = "Kaleidoscope v" + entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "Software", Value = m_Software });
                        result = true;
                        break;

                    case "BATREC|Version":
                        //m_Software = "Bat Recorder v" + entry.Value;
                        m_Software = entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "Software", Value = m_Software });
                        result = true;
                        break;

                    case "BATREC|Host Device":
                        m_HostDevice = entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "Host Device", Value = m_HostDevice });
                        result = true;
                        break;

                    case "BATREC|Host OS":
                        m_HostOS = entry.Value;
                        metaData.Add(new Meta() { Type = "guan", Label = "Host OS", Value = m_HostOS });
                        result = true;
                        break;


                    default:
                        if (entry.Value.StartsWith("{"))
                        {
                            metaData.AddRange(parseJSONMetadata(entry));
                        }
                        else
                        {
                            metaData.Add(new Meta() { Type = "guan", Label = entry.Key, Value = entry.Value });
                        }
                        result = true;
                        break;
                }
        }

        if (result)
        {
            if (string.IsNullOrWhiteSpace(Source))
                Source = "GUANO";
            else
                Source += " and GUANO";

            if (!string.IsNullOrWhiteSpace(m_HostOS))
            {
                // then recorded on Bat Recorder, so we want to modify the microphone and device strings
                string devString = $"{m_Software} on {m_HostDevice} under {m_HostOS}";
                string micString = $"{m_Device}";
                m_Microphone = micString;
                m_Device = devString;
            }
        }

        return result;
    }

    /// <summary>
    /// given anKeyvaluePair in which the Value is a string enclosed in {} which is JSON encoded metadata from a WA metadata item,
    /// parses the sub-items into key:value pairs, comma separated, and returns them as a
    /// list of Metas.  The key for each is the original key | and the secondary key.
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    private List<Meta> parseJSONMetadata(KeyValuePair<string, string> entry)
    {
        List<Meta> result = new List<Meta>();

        //Debug.WriteLine("Parse JSON " + entry.Value);

        string json = entry.Value.Replace(@"{", "").Replace(@"}", "").Trim();
        if (!json.Contains(":"))
        {
            // we have a complex string as a single value
            result.Add(new Meta() { Type = "guan", Label = entry.Key, Value = json });
            //Debug.WriteLine($"\t{entry.Key}:{json}");
        }
        else
        {
            string key = entry.Key + "|";
            string value = "";
            JsonState state = JsonState.INKEY;
            int pos = 0;
            char underCursor = json[pos];
            while (state != JsonState.ENDOFLINE)
            {
                switch (state)
                {
                    case JsonState.INKEY:
                        if (underCursor == ':')
                        {
                            state = JsonState.INBAREVALUE;
                        }
                        else
                        {
                            key += underCursor;
                        }
                        if (++pos >= json.Length) state = JsonState.ENDOFLINE;
                        else underCursor = json[pos];
                        break;

                    case JsonState.INBAREVALUE:
                        if (underCursor == '[')// start of a bracketed string section
                        {
                            state = JsonState.INBRACKETEDSTRINGVALUE;
                        }
                        else if (underCursor == ',')// end of the value field
                        {
                            state = JsonState.INKEY;
                            result.Add(new Meta() { Type = "guan", Label = key, Value = value });
                            //Debug.WriteLine($"\t{key}:{value}");
                            key = entry.Key + "|";
                            value = "";
                        }
                        else
                        {
                            value += underCursor;
                        }
                        if (++pos >= json.Length) state = JsonState.ENDOFLINE;
                        else underCursor = json[pos];
                        break;

                    case JsonState.INBRACKETEDSTRINGVALUE:
                        if (underCursor == ']')
                        {
                            state = JsonState.INBAREVALUE;
                        }
                        else
                        {
                            value += underCursor;
                        }
                        if (++pos >= json.Length) state = JsonState.ENDOFLINE;
                        else underCursor = json[pos];
                        break;

                    default:

                        break;
                }
            }// end while
            if (!key.EndsWith("|") && !string.IsNullOrWhiteSpace(value))
            {
                result.Add(new Meta() { Type = "guan", Label = key, Value = value });
                //Debug.WriteLine($"\t{key}:{value} <EOL>");
            }
        }

        return (result);
    }

    /// <summary>
    ///     Retrieves the metadata sections from a .wav file for either WAMD or GUANO formatted data.
    ///     The file from which to extract the data is wavFilename and the metadata chunk itself is returned as
    ///     a byte[] called metadata.  Formatted versions of the data are returned in the out parameters wamd_data
    ///     and guano_data.  If not present in that format the classes will be returned empty.
    ///     The function returns a string comprising the metadate note section followed by a ; followed by the manual
    ///     species identification string and an optional auto-identification string in brackets.
    ///     the data out parameters will be null if not found.
    ///     Accepts both .wav and .zc files
    /// </summary>
    /// <param name="wavFilename"></param>
    /// <param name="metadata"></param>
    /// <param name="wamd_data"></param>
    /// <param name="guano_data"></param>
    /// <returns></returns>
    private bool Read_MetaData(string wavFilename)
    {
        var result = false;
        rawMetadata = null;
        bool junked = false;

        if (string.IsNullOrWhiteSpace(wavFilename)) return result;
        if (!wavFilename.Trim().EndsWith(".WAV", StringComparison.OrdinalIgnoreCase)) return (readZcMetadata(wavFilename));
        if (!File.Exists(wavFilename) || ((new FileInfo(wavFilename)?.Length ?? 0) <= 0L)) return result;
        try
        {
            using (var fs = File.Open(wavFilename, FileMode.Open))
            {
                var reader = new BinaryReader(fs);

                // chunk 0
                var chunkID = reader.ReadInt32(); //RIFF
                var fileSize = reader.ReadInt32(); // 4 bytes of size
                var riffType = reader.ReadInt32(); //WAVE

                // chunk 1
                var fmtID = reader.ReadInt32(); //fmt_
                var fmtSize = reader.ReadInt32(); // bytes for this chunk typically 16
                int fmtCode = reader.ReadInt16(); // typically 1
                int channels = reader.ReadInt16(); // 1 or 2
                var sampleRate = reader.ReadInt32(); //
                var byteRate = reader.ReadInt32();
                int fmtBlockAlign = reader.ReadInt16(); // 4
                int bitDepth = reader.ReadInt16(); //16

                if (fmtSize == 18) // not expected for .wav files
                {
                    // Read any extra values
                    int fmtExtraSize = reader.ReadInt16();
                    reader.ReadBytes(fmtExtraSize);
                }

                var header = new byte[4];
                byte[]? data = null;
                var dataBytes = 0;
                string strHeader = "";
                // WAMD_Data wamd_data = new WAMD_Data();

                try
                {
                    rawMetadata = null;
                    int size;
                    do
                    {
                        try
                        {
                            if (!junked)
                            {
                                header = reader.ReadBytes(4);

                                if (header == null || header.Length != 4) break;
                                strHeader = Encoding.UTF8.GetString(header);
                            }
                            if (strHeader == "junk")
                            {
                                junked = true;
                            }
                            if (junked)
                            {
                                strHeader = scanForHeader(ref reader);
                            }

                            size = reader.ReadInt32();


                            // if header is 'junk' load the data chunk and scan through it byte by byte for wamd header and restart from there
                            //Debug.WriteLine($"Header={strHeader} and size={size}");
                            if (size <= 0) break;
                            try
                            {
                                data = reader.ReadBytes(size);
                            }
                            catch (Exception ex)
                            {
                                //Debug.WriteLine($"Tried to read too much data - {size}:-{ex}");
                            }

                            try
                            {
                                if ((size & 0x0001) != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
                                    // we have an odd number of bytes for size, so read the xtra null byte of padding
                                    reader.ReadByte();
                            }
                            catch (Exception) // just in case it overflows the data file
                            {
                            }

                            if (strHeader.ToLower() == "list" && data != null)
                            {
                                List<byte> data2 = new List<byte>();
                                foreach (var item in data)
                                {
                                    Char cItem = (char)item;
                                    if (Char.IsLetterOrDigit(cItem) |
                                        Char.IsPunctuation(cItem) ||
                                        Char.IsWhiteSpace(cItem) ||
                                        Char.IsSymbol(cItem)) data2.Add(item);
                                }
                                data = data2.ToArray();
                                //Debug.WriteLine($"INFO data:- {header}/{size}/{data.Length}");
                                infoString = Encoding.UTF8.GetString(data).Trim();
                                infoString = infoString.Replace("INFO", "").Trim();
                                infoString = infoString.Replace("ICMT", "").Trim();
                                //infoString = infoString.Substring(4);
                                infoString = infoString.Replace(")", ")\n").Trim();
                                infoString = infoString.Replace("IART", "\n").Trim();



                            }
                            if (strHeader == "data") dataBytes = size;
                            if (strHeader == "wamd" && data != null)
                            {
                                //Debug.WriteLine("WAMD data:-" + header + "/" + size + "/" + data.Length);
                                rawMetadata = data;
                                result = decode_wamd_data(rawMetadata);
                            }

                            if (strHeader == "guan" && data != null)
                            {
                                //Debug.WriteLine("GUANO data:-" + header + "/" + size + "/" + data.Length);
                                m_Header = strHeader;
                                m_Size = size;
                                rawMetadata = data;
                                var metadataString = Encoding.UTF8.GetString(data);
                                result = DecodeGuanoData(metadataString);
                            }
                        }
                        catch (Exception)
                        {
                            //Debug.WriteLine("Overflowed the data file - " + reader.BaseStream.Position + "/" +
                                      //      reader.BaseStream.Length);
                            break;
                        }
                    } while (reader.BaseStream.Position < reader.BaseStream.Length);
                }
                catch (IOException iox)
                {
                    Tools.ErrorLog(iox.Message);
                    //Debug.WriteLine("Error reading wav file:- " + iox.Message);
                }

                var durationInSecs = 0.0d;
                if (byteRate > 0 && channels > 0 && dataBytes > 0)
                {
                    durationInSecs = (double)dataBytes / byteRate;
                    m_Duration = TimeSpan.FromSeconds(durationInSecs);
                    result = true;
                }
            }
        }
        catch (Exception ex)
        {
            Tools.ErrorLog(ex.Message);
            //Debug.WriteLine(ex);
        }

        return result;
    }

    /// <summary>
    /// header for the rawMetadata
    /// </summary>
    public string m_Header;
    /// <summary>
    /// size of the rawMetadata
    /// </summary>
    public int m_Size;

    /// <summary>
    /// scans through the input stream byte by byte looking for a string saying wamd or guan and if found
    /// returns that as a string
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private string scanForHeader(ref BinaryReader reader)
    {
        string header = "";
        char[] headerBytes = new char[4];
        int index = 0;
        byte next;
        do
        {
            try
            {
                next = reader.ReadByte();
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Tried to read too much data scanning for Header");
                return "";
            }
            switch ((char)next)
            {
                case 'w': if (index == 0) { headerBytes[index++] = 'w'; } break;
                case 'a':
                    if (index == 1 && headerBytes[index - 1] == 'w') headerBytes[index++] = 'a';
                    if (index == 2 && headerBytes[index - 2] == 'u') headerBytes[index++] = 'a';
                    break;
                case 'm':
                    if (index == 2) headerBytes[index++] = 'm';
                    break;
                case 'd':
                    if (index == 3) headerBytes[index++] = 'd';
                    break;
                case 'g':
                    if (index == 0) headerBytes[index++] = 'g';
                    break;
                case 'u':
                    if (index == 1) headerBytes[index++] = 'u';
                    break;
                case 'n':
                    if (index == 3) headerBytes[index++] = 'n';
                    break;
                default: index = 0; break;
            }
            if (index == 4)
            {
                header = new string(headerBytes);
                if (header == "wamd") return (header);
                if (header == "guan") return (header);
                index = 0;
            }

        } while (reader.BaseStream.Position < reader.BaseStream.Length);
        return ("");
    }

    private bool readZcMetadata(string fileName)
    {
        var result = false;
        try
        {
            ZcMetadata zcMetadata = new ZcMetadata(fileName);

            result = zcMetadata.ReadData();

            m_FileName = fileName;
            if (zcMetadata.hasGpsLocation)
            {
                m_Location = new GPSLocation(zcMetadata.Latitude, zcMetadata.Longitude);
                m_Location.m_Name = zcMetadata.Location;
            }

            m_ManualID = zcMetadata.Species;
            m_Device = zcMetadata.Tape + " " + zcMetadata.Spec;

            m_Note = zcMetadata.Note + "\n" + zcMetadata.Note1;

            if (!String.IsNullOrWhiteSpace(zcMetadata.GuanoData))
            {
                DecodeGuanoData(zcMetadata.GuanoData);
            }
            rawMetadata = new byte[10];
            if (m_Start == null || DateTime.Now - m_Start.Value < new TimeSpan(0, 5, 0))
            {
                m_Start = zcMetadata.StartDateTime;
            }
            if (m_Duration == null) m_Duration = zcMetadata.Duration;
            result = true;
        }
        catch (Exception)
        {
            rawMetadata = null;
            result = false;
        }

        return (result);
    }

   
    /// <summary>
    /// Reads the metadata contained in 'file' and appends it to the destinationFile.  Data written is that
    /// held in rawMetadata after a call to ReadMetaData inside the wavmetadata constructor.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="destinationFile"></param>
    internal static void CopyMetaData(string file, string destinationFile)
    {
        var instance = new WavFileMetaData(file);
        if (instance.m_Size > 0)
        {


            using (var streamWriter = new StreamWriter(destinationFile, true))
            {
                if (streamWriter != null)
                {
                    var BinaryWriter = new BinaryWriter(streamWriter.BaseStream);
                    {
                        BinaryWriter.Write(instance.m_Header);
                        BinaryWriter.Write((int)instance.m_Size);
                        BinaryWriter.Write(instance.rawMetadata);
                    }

                }
            }
        }
    }
}

    
