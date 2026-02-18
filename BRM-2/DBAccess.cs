namespace BRM_2;


	public static class DBAccess
{

    private static SQLiteAsyncConnection? db = null;

    /// <summary>
    /// inserts or updates the given batEntry and its associated call definition
    /// </summary>
    public static async Task<int> InsertBatAsync(BatEx batEx)
    {
        BatTable batEntry = batEx.GetTable();
        if (batEntry == null) return (-1);
        if (db == null) db = await DBManager.GetConnection();
        int batsUpdated = await db.InsertAsync(batEntry);
        if (batEx.Calls != null && batEx.Calls.Any())
        {
            foreach (var call in batEx.Calls)
            {
                call.batID = batEntry.ID;
                _ = await db.InsertAsync(call);
            }
        }
        return batsUpdated;
    }

    public static async Task InsertBatElementAsync(XElement? batXL, SQLiteAsyncConnection db)
    {
        if (batXL is null) return;
        if (db == null) return;

        var batEx = ConvertXmlBat(batXL);
        if (batEx != null)
        {
            var batEntry = batEx.GetTable();
            await db.InsertAsync(batEntry);
            if ((batEx.BatTags ?? new List<BatTag>()).Any())
            {
                foreach (var tag in batEx?.BatTags ?? new List<BatTag>())
                {
                    tag.BatID = batEntry?.ID ?? -1;
                    await db.InsertAsync(tag);
                }
            }
            if ((batEx?.Calls ?? new List<Call>()).Any())
            {
                foreach (var call in batEx?.Calls ?? new List<Call>())
                {
                    call.batID = batEntry?.ID ?? -1;
                    await db.InsertAsync(call);
                }
            }
        }
    }

    /// <summary>
    ///     Converts the XML batEntry.
    ///     Takes batEntry as an XElement from an XML file and extracts batEntry, tag and call details
    ///     from the XML, creating new instances of Bat, BatTag and BatCall classes in the process.
    ///     The batEntry is merged with any existing batEntry of the same name (or inserted if it does not exist)
    ///     If there was no existing batEntry then all BatCalls in the definition are added to the database and
    ///     linked to the new batEntry.
    /// </summary>
    /// <param name="bat">
    ///     The batEntry.
    /// </param>
    /// <param name="dc"></param>
    /// <returns>
    /// </returns>
    private static BatEx ConvertXmlBat(XElement bat)
    {
        var newBat = new BatEx();
        BatEx? insertedBat = null;
        try
        {
            newBat.Name = (bat?.Descendants("BatCommonName")?.FirstOrDefault()?.Value) ?? "";
            newBat.Batgenus = (bat?.Descendants("BatGenus")?.FirstOrDefault()?.Value) ?? "";
            newBat.BatSpecies = (bat?.Descendants("BatSpecies")?.FirstOrDefault()?.Value) ?? "";

            if (bat?.Descendants("BatNotes")?.Any() ?? false)
            {

                newBat.Notes = (bat?.Descendants("BatNotes")?.FirstOrDefault()?.Value) ?? "";
            }
            newBat.Label = (bat?.Descendants("Label")?.FirstOrDefault()?.Value) ?? "";


            //newBat.Notes = "";
            var parameters = "";

            var newTags = bat?.Descendants("BatTag") ?? Enumerable.Empty<XElement>();
            if (((newTags?.Count()) ?? 0) > 0)
            {
                var tagList = newTags.Select(tag => tag.Value).ToList();
                newBat.BatTags = new List<BatTag>();
                foreach (var tag in tagList)
                {
                    var batTag = new BatTag();
                    batTag.Tag = tag;
                    newBat.BatTags.Add(batTag);
                }

            }

            var newCommonNames = bat?.Descendants("BatCommonName") ?? Enumerable.Empty<XElement>();
            if (((newCommonNames?.Count()) ?? 0) > 0) newBat.Name = newCommonNames.First().Value;
            newBat.Calls.Clear();
            var callData = bat?.Descendants("Call") ?? Enumerable.Empty<XElement>();
            if (((callData?.Count()) ?? 0) > 0)
            {
                foreach (var call in callData ?? Enumerable.Empty<XElement>())
                {
                    var dbCall = GetXmlCallParameters(call, parameters);
                    newBat.Calls.Add(dbCall);
                }
            }




        }
        catch (Exception ex)
        {
            //Debug.WriteLine(ex.Message);
        }

        return newBat;
    }


    private static Call GetXmlCallParameters(XElement call, string parameters)
    {
        var dbCall = new Call();
        var mean = 0.0d;
        var variation = 0.0d;
        var xFstart = call.Descendants("fStart");
        if (((xFstart?.Count()) ?? 0) > 0)
        {
            parameters = xFstart?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters))
                if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                {
                    dbCall.StartFrequency = mean;
                    dbCall.StartFrequencyVariation = variation;
                }
        }

        mean = 0.0d;
        variation = 0.0d;
        var xFend = call.Descendants("fEnd");
        if (!xFend.IsNullOrEmpty())
        {
            parameters = xFend?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters))
                if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                {
                    dbCall.EndFrequency = mean;
                    dbCall.EndFrequencyVariation = variation;
                }
        }

        mean = 0.0d;
        variation = 0.0d;
        var xFpeak = call.Descendants("fPeak");
        if (!xFpeak.IsNullOrEmpty())
        {
            parameters = xFpeak?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters))
                if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                {
                    dbCall.PeakFrequency = mean;
                    dbCall.PeakFrequencyVariation = variation;
                }
        }

        mean = 0.0d;
        variation = 0.0d;
        var xInterval = call.Descendants("Interval");
        if (!xInterval.IsNullOrEmpty())
        {
            parameters = xInterval?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters))
                if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                {
                    dbCall.PulseInterval = mean;
                    dbCall.PulseIntervalVariation = variation;
                }
        }

        mean = 0.0d;
        variation = 0.0d;
        var xDuration = call.Descendants("Duration");
        if (!xDuration.IsNullOrEmpty())
        {
            parameters = xDuration?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters))
                if (Tools.GetValuesAsMeanAndVariation(parameters, out mean, out variation))
                {
                    dbCall.PulseDuration = mean;
                    dbCall.PulseDurationVariation = variation;
                }
        }

        var xFunction = call.Descendants("Function");
        if (!xFunction.IsNullOrEmpty())
        {
            parameters = xFunction?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters)) dbCall.CallFunction = parameters;
        }

        var xType = call.Descendants("Type");
        if (!xType.IsNullOrEmpty())
        {
            parameters = xType?.FirstOrDefault()?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(parameters)) dbCall.CallType = parameters;
        }

        var xComments = call.Descendants("Comments");
        if (!xComments.IsNullOrEmpty())
        {
            parameters = xComments?.FirstOrDefault()?.Value ?? "";

            if (!string.IsNullOrWhiteSpace(parameters)) dbCall.CallNotes = parameters;
        }

        return dbCall;
    }

    /// <summary>
    /// Returns a list of BatEx including calls and tags
    /// </summary>
    /// <returns></returns>
    public static async Task<List<BatEx>> GetAllBatsAsync()
    {
        var bats = await DBAccess.GetBatsAsync();

        //Debug.WriteLine("got bats");
        for (int i = 0; i < bats.Count; i++)
        {
            var tags = await DBAccess.GetBatTagsAsync(bats[i].ID);
            bats[i].BatTags = tags;

            var calls = await DBAccess.GetCallsForBatAsync(bats[i].ID);
            bats[i].Calls = calls;
        }
        return bats;
    }

    /// <summary>
    /// Returns a list of all reference bats in the database as BatEx, without populated sub-fields
    /// only contains:-
    /// ID
    /// Batgenus
    /// BatSpecies
    /// Name
    /// Notes
    /// </summary>
    /// <returns></returns>
    public static async Task<List<BatEx>> GetBatsAsync()
    {
        List<BatEx> result= new List<BatEx>();
        if (db == null) db = await DBManager.GetConnection();
        var bats = await db.Table<BatTable>().ToListAsync();
        foreach ( var bat in bats) {
            BatEx batEx = new BatEx(bat);
            result.Add(batEx);
        }

        return result;


    }

    public static async Task<List<Call>> GetCallsForBatAsync(int BatID)
    {
        if (db == null) db = await DBManager.GetConnection();

        var calls = await db.Table<Call>().Where(cl => cl.batID == BatID).ToListAsync();
        return calls;
    }

    public async static Task<List<BatTag>> GetBatTagsAsync(int iD)
    {
        if (db == null) db = await DBManager.GetConnection();

        var tags = await db.Table<BatTag>().Where(bt => bt.BatID == iD).ToListAsync();
        return tags;
    }

    public static async Task<List<RecordingSessionEx>> GetSessionsAsync()
    {
        if (db == null) db = await DBManager.GetConnection();
        var result = new List<RecordingSessionEx>();
        var sesssTables= await db.Table<RecordingSessionTable>().ToListAsync();
        foreach (var sess in sesssTables??new List<RecordingSessionTable>())
        {
            result.Add(new RecordingSessionEx(sess));
        }
        return result;



    }

    private static string DateTimeFormat = "yyyy-MM-ddThh:mm:ss";

    /// <summary>
    /// Fills a RecordingSession from an xml file and adds it to the database
    /// </summary>
    /// <param name="xSession"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal static async void InsertXMLSession(string fileName)
    {
        RecordingSessionEx sessionEx = new RecordingSessionEx();


        try
        {
            XDocument doc = XDocument.Load(fileName);
            foreach (var element in doc.Descendants())
            {
                element.Attributes().Where(a => string.IsNullOrWhiteSpace(a.Value)).Remove();
            }

            using (var reader = doc?.Root?.CreateReader())
            {
                if (reader != null)
                {
                    var serializer = new XmlSerializer(typeof(RecordingSessionEx));
                    sessionEx = (RecordingSessionEx?)(serializer.Deserialize(reader)) ?? new RecordingSessionEx();
                    reader.Close();
                }
            }
            _ = await DBAccess.InsertSessionAsync(sessionEx);
        }
        catch (Exception ex)
        {
            //Debug.WriteLine(ex.Message);
        }

    }

    public static async Task<List<RecordingSessionTable>> GetRecordingSessionsAsync()
    {
        if (db == null) db = await DBManager.GetConnection();

        return await db.Table<RecordingSessionTable>().ToListAsync();
    }

    /// <summary>
    /// Inserts a RecordingSession into the database
    /// </summary>
    /// <param name="sessionEx"></param>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task<int> InsertSessionAsync(RecordingSessionEx? sessionEx)
    {
        if (sessionEx == null) return (-1);
        if (db == null) db = await DBManager.GetConnection();
        var sessionEntry = sessionEx.GetTable();
        Debug.WriteLine($"Insert Session {sessionEx.SessionTag}");
        var sessionCollection = await DBAccess.GetRecordingSessionsAsync();
        if (sessionCollection == null)
        {
            Tools.ErrorLog("Cannot obtain or create a sessionEx collection");

        }
        Debug.WriteLine($"Found {sessionCollection?.Count??0} records");    
        sessionEx.NumberOfRecordings = sessionEx.recordings.Count;
        var existing = (from sess in sessionCollection ?? new List<RecordingSessionTable>()
                        where sess.SessionTag == sessionEx.SessionTag
                        select sess).FirstOrDefault();

        int SessionsUpdated = -1;
        if (existing != null)
        {
            Debug.WriteLine($"Found matching existing session");
            sessionEntry.ID= existing.ID;
            SessionsUpdated = await db.UpdateAsync(sessionEntry);
            Debug.WriteLine($"Sessions updated: {SessionsUpdated}");
        }
        else
        {
            sessionEntry.ID = 0;
            SessionsUpdated = await db.InsertAsync(sessionEntry);
            Debug.WriteLine($"Sessions Inserted: {SessionsUpdated}");
        }

        if (SessionsUpdated > 0)
        {

            foreach (var recording in sessionEx.recordings ?? new List<RecordingEx>())
            {
                recording.SessionID = sessionEntry.ID;
                Debug.WriteLine($"{recording.RecordingName}");
                _ = await DBAccess.InsertRecordingAsync(recording);
                Debug.WriteLine("\tInserted");
            }
        }



        return SessionsUpdated;


    }

    public static async Task<int> InsertRecordingAsync(RecordingEx recordingEx)
    {
        if (db == null) db = await DBManager.GetConnection();
        int recordsUpdated = -1;
        if (recordingEx == null) return -1;
        var recordingEntry = recordingEx.GetTable();
        if (recordingEx.ID > 0)
        {
            recordsUpdated = await db.UpdateAsync(recordingEntry);
            //Debug.WriteLine($"Updated rec {recordingEx.ID}, has {recordingEx.Metas.Count} metas and {recordingEx.LabelledSegments.Count} segmentsEx");
        }
        else
        {
            recordsUpdated = await db.InsertAsync(recordingEntry);
            //Debug.WriteLine($"Inserted rec {recordingEx.ID}, has {recordingEx.Metas.Count} metas and {recordingEx.LabelledSegments.Count} segmentsEx");
        }

        if (recordsUpdated > 0)
        {
            if (recordingEx.Metas != null && recordingEx.Metas.Any())
            {
                foreach (var meta in recordingEx.Metas)
                {
                    int metasUpdated = -1;
                    meta.RecordingID = recordingEntry.ID;
                    if (meta.ID > 0)
                    {
                        metasUpdated = await db.UpdateAsync(meta);
                        //Debug.WriteLine($"updated Meta {meta.ID}");
                    }
                    else
                    {
                        metasUpdated = await db.InsertAsync(meta);
                        //Debug.WriteLine($"inserted Meta {meta.ID}");
                    }

                }

                foreach (var segmentEx in recordingEx.LabelledSegments ?? new List<LabelledSegmentEx>())
                {
                    segmentEx.ID = -1;
                    segmentEx.RecordingID = recordingEntry.ID;

                    int segmentsUpdated = await DBAccess.InsertLabelledSegmentAsync(segmentEx);
                    //Debug.WriteLine($"Inserted segmentEx {segmentEx.ID}");
                }

            }
        }
        return (recordsUpdated);
    }

    /// <summary>
    /// Inserts or updates a labelled segmentEx and its associated data in the database asynchronously.
    /// </summary>
    /// <remarks>This method also inserts or updates the associated <c>Calls</c> and <c>IdedBats</c>
    /// collections of the segmentEx. Each associated entity will have its <c>SegmentID</c> property set to the
    /// <c>ID</c> of the segmentEx before being inserted or updated.</remarks>
    /// <param name="segmentEx">The <see cref="LabelledSegmentTable"/> instance representing the segmentEx to be inserted or updated. If the
    /// segmentEx's <c>ID</c> is greater than 0, it will be updated; otherwise, it will be inserted.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the number of
    /// rows affected in the database. Returns -1 if the <paramref name="segmentEx"/> is <see langword="null"/>.</returns>
    public static async Task<int> InsertLabelledSegmentAsync(LabelledSegmentEx segmentEx)
    {
        if (db == null) db = await DBManager.GetConnection();
        if (segmentEx == null) return -1;
        LabelledSegmentTable segmentEntry= segmentEx.GetTable();
        int segmentsUpdated = -1;

        if (segmentEx.ID > 0)
        {
            segmentsUpdated = await db.UpdateAsync(segmentEntry);
        }
        else
        {
            segmentsUpdated = await db.InsertAsync(segmentEntry);
        }
        if (segmentsUpdated > 0)
        {
            foreach (var call in segmentEx.Calls ?? new List<Call>())
            {
                int callsUpdated;
                call.SegmentID = segmentEntry.ID;
                if (call.ID > 0)
                {
                    callsUpdated = await db.UpdateAsync(call);
                }
                else
                {
                    callsUpdated = await db.InsertAsync(call);
                }
            }

            foreach (var batEx in segmentEx.IdedBats ?? new List<IdedBatEx>())
            {
                batEx.SegmentID = segmentEntry.ID;
                var batEntry= batEx.GetTable();
                int idedBatsUpdated = -1;
                if (batEx.ID > 0)
                {
                    idedBatsUpdated = await db.UpdateAsync(batEntry);
                }
                else
                {
                    idedBatsUpdated = await db.InsertAsync(batEntry);
                }
            }
        }
        return segmentsUpdated;
    }


    internal async static Task UpdateLabelledSegment(LabelledSegmentEx labelledSegment)
    {
        await DBAccess.InsertLabelledSegmentAsync(labelledSegment);
    }

    internal static async Task<RecordingSessionEx> GetRecordingSessionAsync(int sessionID)
    {
        if (db == null) db = await DBManager.GetConnection();
        try
        {
            return await db.Table<RecordingSessionEx>().Where(sess => sess.ID == sessionID).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Error("GetRecordingSessionAsync");
            return null;
        }
    }

    internal static void Error(string message)
    {
        //Debug.WriteLine($"\t\t!!!ERROR{message}");
    }




    internal static async Task<RecordingSessionTable> GetRecordingSessionAsync(string sessionTag)
    {


        if (db == null) db = await DBManager.GetConnection();

        try
        {
            return await db.Table<RecordingSessionTable>().Where(rs => rs.SessionTag == sessionTag).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Error($"GetRecordingSessionAsync:- {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Deletes the recordingSessionEx after deleting all the contained recordings
    /// </summary>
    /// <param name="recordingSessionEx"></param>
    /// <returns></returns>
    internal static async Task<int> DeleteSessionAsync(RecordingSessionEx recordingSessionEx)
    {
        if (recordingSessionEx == null) return -1;
        if (recordingSessionEx.ID <= 0) return -1;
        if (db == null) db = await DBManager.GetConnection();
        var recs = await DBAccess.GetRecordingsForSessionAsync(recordingSessionEx.ID);
        foreach (var recording in recs??new List<RecordingEx>())
        {
            _ = await DBAccess.DeleteRecordingAsync(recording.ID);
        }

        var recordingSessionInDb = await db.Table<RecordingSessionTable>().Where(rs => rs.ID == recordingSessionEx.ID).FirstOrDefaultAsync();
        int result = 0;
        if(recordingSessionInDb != null) {
            result = await db.DeleteAsync(recordingSessionInDb);
        }
        return result;

    }


    /// <summary>
    /// Deletes the recordingEx with the specified ID, along with its associated metadata and segmentsEx.
    /// </summary>
    /// <remarks>This method deletes all metadata and segmentsEx associated with the specified recordingEx
    /// before removing the recordingEx itself. Ensure that the database connection is properly initialized before
    /// calling this method.</remarks>
    /// <param name="recID">The unique identifier of the recordingEx to delete. Must be a non-negative integer.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is an integer indicating the outcome:
    /// <list type="bullet"> <item><description>-1 if <paramref name="recID"/> is less than 0.</description></item>
    /// <item><description>The number of rows affected in the database if the operation
    /// succeeds.</description></item> </list></returns>
    private static async Task<int> DeleteRecordingAsync(int recID)
    {
        

        if (recID <= 0) return -1;

        if (db == null) db = await DBManager.GetConnection();

        var metas = await DBAccess.GetMetasForRecordingAsync(recID) ?? new List<Meta>();
        foreach (var meta in metas)
        {
            _ = await db.DeleteAsync(meta);
        }

        var segmentsEx = await DBAccess.GetSegmentsForRecordingAsync(recID) ?? new List<LabelledSegmentEx>();
        foreach (var segment in segmentsEx??new List<LabelledSegmentEx>())
        {
            _ = await DBAccess.DeleteSegmentAsync(segment.GetTable());
        }
        int result = 0; 
        var rec= await db.Table<RecordingTable>().Where(r => r.ID == recID).FirstOrDefaultAsync();
        if(rec!=null) result= await db.DeleteAsync(rec);
        return result;


    }

    internal static async Task<List<Meta>> GetMetasForRecordingAsync(int recid)
    {
        if (db == null) db = await DBManager.GetConnection();
        return await db.Table<Meta>().Where(m => m.RecordingID == recid).ToListAsync();
    }


    internal static async Task<int> DeleteSegmentAsync(LabelledSegmentTable segEntry)
    {
        if (segEntry == null) return -1;
        if (segEntry.ID <= 0) return -1;
        if (db == null) db = await DBManager.GetConnection();

        var calls = await DBAccess.GetCallsForSegmentAsync(segEntry.ID) ?? new List<Call>();
        foreach (var call in calls??new List<Call>())
        {
            _ = await db.DeleteAsync(call);
        }

        var idedBatsEx = await DBAccess.GetIdedBatsForSegmentAsync(segEntry.ID) ?? new List<IdedBatEx>();
        foreach (var batEx in idedBatsEx??new List<IdedBatEx>())
        {
            var entry = batEx.GetTable();
            _ = await db.DeleteAsync(entry);
        }
        int result = 0;
        if(segEntry != null) {
            result = await db.DeleteAsync(segEntry);
        }
        return result;
    }

    internal static async Task<List<Call>> GetCallsForSegmentAsync(int segid)
    {
        if (db == null) db = await DBManager.GetConnection();

        return await db.Table<Call>().Where(c => c.SegmentID == segid).ToListAsync();
    }


    internal static async Task<List<RecordingEx>> GetRecordingsForSessionAsync(int sessionId)
    {

        if (db == null) db = await DBManager.GetConnection();
        List<RecordingEx> recs = new List<RecordingEx>();
        try
        {
            //recs = await db.Table<Recording>().Where(rec => rec.SessionID == sessionId).ToListAsync();
            var recEntries = await db.Table<RecordingTable>().Where(rec => rec.SessionID == sessionId).ToListAsync();
            //Debug.WriteLine($"got {recs.Count} recs");
            foreach (var rec in recEntries??new List<RecordingTable>())
            {
                var recex = new RecordingEx(rec);
                recex.LabelledSegments = await DBAccess.GetSegmentsForRecordingAsync(rec.ID);
                _ = recex.BatSummaryString;
                recs.Add(recex);
            }
        }
        catch (Exception ex)
        {
            //Debug.WriteLine(ex);
        }
        return recs;
    }



    internal static async Task<List<IdedBatEx>> GetIdedBatsForSegmentAsync(int labelledSegmentId)
    {
        if (db == null) db = await DBManager.GetConnection();
        var result= new List<IdedBatEx>();
        //return await db.Table<IdedBat>().Where(b=>b.SegmentID==labelledSegmentId).ToListAsync();
        var allIdedBats = await db.Table<IdedBatTable>().ToListAsync();
        var bats = from idedBat in allIdedBats
                   where idedBat.SegmentID == labelledSegmentId
                   select idedBat;
        if(bats?.Any() ?? false)
        {
            foreach (var bat in bats)
            {
                result.Add(new IdedBatEx(bat));
            }
        }
        return result;

    }

    /// <summary>
    /// Returns a list of all the LabelledSegments for the recordingEx as LabelledSegmentEx with IdedBats populated
    /// </summary>
    /// <param name="iD"></param>
    /// <returns></returns>
    internal static async Task<List<LabelledSegmentEx>> GetSegmentsForRecordingAsync(int iD)
    {
        if (db == null) db = await DBManager.GetConnection();
        List<LabelledSegmentEx> result = new List<LabelledSegmentEx>();
        var segsEntries = await db.Table<LabelledSegmentTable>().Where(seg => seg.RecordingID == iD).ToListAsync();
        if(segsEntries != null && segsEntries.Any())
        {
            foreach (var seg in segsEntries)
            {
                var segEx = new LabelledSegmentEx(seg);
                segEx.IdedBats = await GetIdedBatsForSegmentAsync(seg.ID); 
                
                result.Add(segEx);
            }
        }

        return result;
    }


    internal static async Task<List<string>> GetAllOperatorsListAsync()
    {
        if (db == null) db = await DBManager.GetConnection();

        var sessions = await DBAccess.GetSessionsAsync();

        var operators = (from rs in sessions
                         select rs.Operator).Distinct().ToList();
        return operators;
    }

    internal static async Task<List<string>> GetAllEquipmentListAsync()
    {
        var sessions = await DBAccess.GetSessionsAsync();
        return (from rs in sessions
                select rs.Equipment).Distinct().ToList();
    }

    internal static async Task<List<string>> GetAllMicrophonesListAsync()
    {
        var sessions = await DBAccess.GetSessionsAsync();
        return (from rs in sessions
                select rs.microphone).Distinct().ToList();
    }

    /// <summary>
    /// gets a list of all batEntry tags which are contained in the given string regardless of case and
    /// regardless of nesting.  tags are sorted longest to shortest
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal static async Task<List<BatTag>> GetContainedTagsAsync(string description)
    {
        string udescription = description.ToUpper();
        var result = new List<BatTag>();
        if (db == null) db = await DBManager.GetConnection();
        var allTags = await db.Table<BatTag>().ToListAsync();
        foreach (var tag in allTags)
        {
            if (tag.Tag == tag.Tag.ToUpper()) // all upper case tag, so match must  be case dependent
            {
                if (description.Contains(tag.Tag)) result.Add(tag);
            }
            else
            { // mixed case tag, so do a case independent match - tag is already upper case so no need to modify
                if (udescription.Contains(tag.Tag.ToUpper())) result.Add(tag);
            }
        }
        //var tags = BatCol.Select(batEntry => batEntry.BatTags.Where(bt => udescription.Contains(bt.Tag.ToUpper())));
        //foreach (var tag in tags) { result.AddRange(tag.Select(t=>t.Tag)); }
        //var LatinNames = BatCol.Select(batEntry => batEntry.Batgenus + " " + batEntry.BatSpecies);
        //result.AddRange(LatinNames);
        //var CommonNames = BatCol.Select(batEntry => batEntry.Name);
        result = result.OrderByDescending(x => x.Tag.Length).ToList();
        //Debug.WriteLine($"In {description}:-");
        foreach (var item in result)
        {
            Debug.Write($"{item.Tag}, ");

        }
        //Debug.WriteLine("");
        return result;
    }

    internal static async Task<BatTable?> GetBatByTagAsync(string tag)
    {
        if (db == null) db = await DBManager.GetConnection();

        if (string.IsNullOrWhiteSpace(tag)) return null;
        BatTable? result = null;

        var batTag = await db.Table<BatTag>().Where(t => t.Tag.Contains(tag)).FirstOrDefaultAsync();

        if (batTag != null)
        {
            var bat = await DBAccess.GetBatByIDAsync(batTag.ID);
            return bat;
        }





        return result;
    }

    public async static Task<BatTable?> GetBatByIDAsync(int iD)
    {
        if (db == null) db = await DBManager.GetConnection();
        return await db.Table<BatTable>().Where(b => b.ID == iD).FirstOrDefaultAsync();
    }

    internal static async Task<IEnumerable<string>> GetAllLocationsListAsync()
    {
        if (db == null) db = await DBManager.GetConnection();

        var sess = await db.Table<RecordingSessionTable>().ToListAsync();
        return (sess.Select(s => s.Location).Distinct().ToList());
    }

    internal async static Task<int> GetNumRecordingsForSession(int iD)
    {
        if (db == null) db = await DBManager.GetConnection();

        var recs = (await DBAccess.GetRecordingsForSessionAsync(iD)).Count;

        return (recs);
    }


    internal static async Task<RecordingSessionTable> GetSessionAsync(int sessionID)
    {
        if (db == null) db = await DBManager.GetConnection();

        return await db.Table<RecordingSessionTable>().Where(sess => sess.ID == sessionID).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves a recording with the specified ID from the database. no sub-fields populated
    /// </summary>
    /// <param name="recordingID">The unique identifier of the recording to retrieve.</param>
    /// <returns>A <see cref="RecordingEx"/> object representing the recording with the specified ID,  or <see
    /// langword="null"/> if no matching recording is found.</returns>
    internal static async Task<RecordingEx> GetRecordingAsync(int recordingID)
    {
        if (db == null) db = await DBManager.GetConnection();

        var table= await db.Table<RecordingTable>().Where(rec => rec.ID == recordingID).FirstOrDefaultAsync();
        var result= new RecordingEx(table);
        result.LabelledSegments=await DBAccess.GetSegmentsForRecordingAsync(recordingID);
        _=result.BatSummaryString;
        return result;
    }

    /// <summary>
    /// returns a batEx for the idedBat specified by name, with calls and tags NOT populated
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static async Task<BatEx> GetNamedBat(string name)
    {
        if (db == null) db = await DBManager.GetConnection();

        var bat=await (from b in db.Table<BatTable>()
                 where b.Name == name select b).FirstOrDefaultAsync();
        var batEx = new BatEx(bat);
        return batEx;

    }
}