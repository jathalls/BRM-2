namespace BRM_2;
internal class Importer
{
    public Importer() {  }

    public async Task<RecordingSessionEx> ImportFromWav(string path) 
    {
        if (!Directory.Exists(path)) return new RecordingSessionEx();
        RecordingSessionEx session= new RecordingSessionEx();
        //var wavFiles=Directory.EnumerateFiles(path,"*.wav");
#if MACATALYST
        url=SecurityScopedBookmarks.TryRestoreFolderFromBookmark(path);
        path=url?.Path ?? path;
#endif

        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        FileInfo[] wavFiles = directoryInfo.GetFiles().Where(f=>f.Name.ToUpper().EndsWith(".WAV")).OrderBy(f => f.CreationTime).ToArray();
        session.SessionTag=directoryInfo.Name;
        
        if ((wavFiles?.Any()) ?? false)
        {
            session.SessionStart = wavFiles[0].CreationTime;
            session.SessionEnd = wavFiles.Last().CreationTime + TimeSpan.FromMinutes(5);
            
            session.OriginalFilePath = path;
            session.SessionTag = directoryInfo.Name;
            //Debug.WriteLine($"{session.SessionTag}");
            /* TODO as future expansion, for now leave it to the user to fill in the form
            if (File.Exists(Path.Combine(path, "header.xml")))
            {
                session= ImportHeader(Path.Combine(path, "header.xml"),session);
            }*/
            
            

            if (session == null) return new RecordingSessionEx();

            WavFileMetaData? metaData=null;
            bool first = true;
            WavFileMetaData? firstMetaData = null;
            DateTime overallEnd = DateTime.MinValue;

            foreach(FileInfo wavFile in wavFiles)
            {
                //Debug.WriteLine($"\n\nNext {wavFile.Name}");
                (RecordingEx? rec,WavFileMetaData? wfmd) details = await GetRecordingDetailsFromFileAsync(wavFile);
                var recording = details.rec;
                var wfmd = details.wfmd;
                if (recording == null) continue;
                metaData = wfmd;
                session.recordings.Add(recording??new RecordingEx());
                if (first)
                {
                    first = false;
                    firstMetaData = wfmd;
                }
                if ((metaData?.m_End??DateTime.MinValue) > overallEnd) { overallEnd=metaData?.m_End??DateTime.MinValue; }

                //Debug.WriteLine($"wav file {wavFile} added to session");
            }
            if (metaData != null)
            {// metadata is the metadata for the last recording in the seeion
                session.SessionEnd = overallEnd;
                
                if (firstMetaData != null)
                {
                    session.SessionStart=firstMetaData.m_Start ?? session.SessionStart;
                    
                    session.LocationGPSLatitude = (decimal)firstMetaData.m_Location.m_Latitude;
                    session.LocationGPSLongitude = (decimal)firstMetaData.m_Location.m_Longitude;
                    session.Location = firstMetaData.m_Location.m_Name;
                    session.microphone = firstMetaData.m_Microphone;
                    session.Equipment = firstMetaData.m_Device;
                    session.SessionNotes = firstMetaData.FormattedText();
                }
            }

            
            
        }
        return session;
    }

    /// <summary>
    /// Reads wav file metadata and uses that plus the fileInfo to create and return a new
    /// RecordingInstance, including it's list of LabelledSegments
    /// </summary>
    /// <param name="wavFile"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<(RecordingEx? rec ,WavFileMetaData? wfmd)> GetRecordingDetailsFromFileAsync(FileInfo wavFile)
    {
        //Debug.WriteLine("GetRecordingDetailsFromFileAsync");
        var duration=Tools.GetFileDatesAndTimes(wavFile.FullName, out string wavfile, out DateTime fileStart, out DateTime fileEnd);
        if (duration.TotalSeconds < 1.0d) return ((RecordingEx?)null, (WavFileMetaData?)null);
        RecordingEx rec = new RecordingEx();
        rec.RecordingDate = fileStart;
        rec.RecordingStartTime = fileStart;
        rec.RecordingEndTime = fileEnd;
        rec.RecordingName = wavFile.Name;
        
        rec.GetMetaDataFromFile(wavFile, out WavFileMetaData wfmd);
        string note = wfmd.m_Note;
        (RecordingEx?, WavFileMetaData) result = new(rec, wfmd);
        //TODO extract IDs from metadata
        await rec.UpdateLabelledSegmentsAsync(wavFile, fileStart, fileEnd,note);
        
        

        return result;
    }

    

    /// <summary>
    /// Imports a complete session encoded in an xml file
    /// </summary>
    /// <param name="path">filly qualified path to the xml file</param>
    public void ImportXMLSession(string path)
    {
        if (File.Exists(path))
        {
            DBAccess.InsertXMLSession(path);
        }
               
    }
}

/*
 *#if MACCATALYST
   using Foundation;
   using System.Text;
   
   public static class WavSidecarWriter
   {
       public static void WriteTxtForAllWavsInPickedFolder(NSUrl folderUrl)
       {
           var started = folderUrl.StartAccessingSecurityScopedResource();
           try
           {
               var folderPath = folderUrl.Path;
               if (string.IsNullOrWhiteSpace(folderPath))
                   return;
   
               foreach (var wavPath in Directory.EnumerateFiles(folderPath, "*.wav", SearchOption.TopDirectoryOnly))
               {
                   var txtPath = Path.ChangeExtension(wavPath, ".txt");
   
                   // Example contents — replace with your real content
                   var content = $"Sidecar for {Path.GetFileName(wavPath)}{Environment.NewLine}";
   
                   File.WriteAllText(txtPath, content, Encoding.UTF8);
               }
           }
           finally
           {
               if (started)
                   folderUrl.StopAccessingSecurityScopedResource();
           }
       }
   }
   #endif
 * 
 */
