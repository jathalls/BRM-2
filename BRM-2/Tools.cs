using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
namespace BRM_2;

    public static class StringHelper
    {
        /// <summary>
        /// Extension method on string to split the string on the first occurence of the specified character
        /// returning a two string array the first contining everything up to the splitter and the second
        /// everything after the splitter.  The splitter is not in either.  string[1] may be empty.
        /// if c is not in the string the full string is returned in string[0].
        /// if the splitter is the first character string[0] will be empty and string[1] will be the original
        /// without the splitter.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string[] SplitOnFirst(this string s, char c)
        {
            string[] result = new string[2];
            result[0] = s;
            result[1] = "";
            if (s.Contains(c))
            {
                int index = s.IndexOf(c);
                if (index >= 0)
                {
                    result[0] = s.Substring(0, index);
                    result[1] = s.Substring(index) + " "; // in case the string ends with c, add some spaces to make it longer than 1 char
                    result[1] = result[1].Substring(1).Trim(); // still contains the c, so remove it and trim the space padding away - may leave an empty string
                }
            }

            return (result);
        }
    }

    public static class DateTimeHelper
    {
        public static string ShortYear(this DateTime dt)
        {
            string result = "";
            int year= dt.Year;
            if (year >= 2000)
            {
                result = $"{(year-2000):00}";
            }else if (year >= 1900)
            {
                result = $"{(year - 1900):00}";
            }
            return result;
        }
    }

    /// <summary>
    ///     Class of miscellaneous, multi access functions - all static for ease of re-use
    /// </summary>
    public static class Tools
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static string macroFileName = @"C:\audacity-win-portable\Portable Settings\Macros\BRM-Macro.txt";

        /// <summary>
        ///     BlobTypes are used to identify the type of binary data object stored in the database.
        ///     The enum types are 3 or 4 char strings that are stored as string literals in the database
        ///     but the enum allows simple internal handling.  The enum is converted to a string to be
        ///     stored in the database and is converted back to an enum on retrieval.  enum names must be limited
        ///     to 4 chars to fit into the database type field.
        ///     BMP is a raw bitmap
        ///     BMPS is a BitmapSource object
        ///     WAV is a snippet of waveform read from a .wav file.
        ///     PNG is an image in PNG format (prefferred)
        ///     SPCT is an image of a self-generated sonagram/spectrogram in PNG image format
        /// </summary>
        public enum BlobType
        {
            NONE = 0,
            ANY = 1,
            BMP,
            BMPS,
            WAV,
            PNG,
            SPCT
        }





        /// <summary>
        /// Copies a directory and its contents recursively or not depending
        /// on the boolean parameter
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
                File.SetAttributes(temppath, FileAttributes.Normal);
                File.SetCreationTime(temppath, file.CreationTime);
                File.SetLastAccessTime(temppath, file.LastAccessTime);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }


        /// <summary>
        /// Recursively deletes this directory and all sub directories and allthe files in those
        /// directories, setting attributes to Normal as it goes so even Read-Only items will get deleted
        /// </summary>
        /// <param name="topDir"></param>
        public static void DirectoryDelete(string topDir)
        {
            if (!Directory.Exists(topDir)) return;
            File.SetAttributes(topDir, FileAttributes.Normal);
            var files = Directory.EnumerateFiles(topDir);
            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            var folders = Directory.EnumerateDirectories(topDir);
            foreach (var folder in folders)
            {
                Tools.DirectoryDelete(folder);
            }
            Directory.Delete(topDir);
        }

        /// <summary>
        /// Writes an error message to a log file
        /// </summary>
        /// <param name="error">the string containing the error message to log</param>
        public static void ErrorLog(string error)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"BRMLite\BRMLiteErrors\");
            var errorFile = Path.Combine(path??@".\Errors\", "BRM-Error-Log.txt");
            string caller = "";
            string callerscaller = "";
            try
            {
                var build = (Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString())??"";

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                using (var stream = new StreamWriter(errorFile, true))
                {

                     stream.WriteLine("\n" + DateTime.Now + "Bat Recording Manager v" + build + "\n");
                    var stackTrace = new StackTrace();
                    caller = (stackTrace.GetFrame(1)?.GetMethod()?.Name) ?? "No Stack";
                    if (!caller.Contains("No Stack"))
                    {
                        callerscaller = (stackTrace.GetFrame(2)?.GetMethod()?.Name) ?? "";
                    }

                    stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "[" +
                                     caller + $"]/[{callerscaller}] :- " + error);
                    //Debug.WriteLine("ERROR:- in " + caller + ":- " + error);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("\n\n**** Error writing to error log" + ex);
                File.AppendAllText(path + "FatalError.txt", $"Error writing to Log file!!!!!!!!!!!{ex.Message}[{caller}]/[{callerscaller}] for {error}\n");
            }
        }

        public static int ToPasses(TimeSpan duration)
        {
            int result = 0;
            var secs = duration.TotalSeconds;
            result=(int)Math.Floor((secs+2.5d)/5.0d);
            if (result < 1 && secs > 0.0d) result = 1;

            return result;
        }


        /// <summary>
        ///     Formats the time span. Given a Timespan returns a formatted string as mm'ss.sss" or 23h59'58.765"
        /// </summary>
        /// <param name="time">
        ///     The time.
        /// </param>
        /// <returns>
        /// </returns>
        public static string FormattedTimeSpan(TimeSpan time)
        {
            TimeSpan absTime = time;
            var result = "";
            if (time != null)
            {
                absTime = time.Duration();
                if (absTime.Hours > 0) result = result + absTime.Hours + "h";
                if (absTime.Hours > 0 || absTime.Minutes > 0) result = result + absTime.Minutes + "'";
                var seconds = absTime.Seconds + absTime.Milliseconds / 1000.0m;
                result = result + $"{seconds:0.0#}\"";
            }

            if (time.Ticks < 0L)
            {
                result = "(-" + result + ")";
            }

            return result;
        }

        /// <summary>
        /// / parses the recording name to try and get date and time from it.
        /// Essentially the same as getDateTimeFromFilename(string file,out DateTime date)
        /// Works with both .wav files containing yyyymmdd[-_]hhmmss formatted date time
        /// or ZC files as YMddhhmm[-_]ss where Y and M may be alphanumeric
        /// if Parsing fails returns DateTime.Now
        /// </summary>
        /// <param name="wavfile"></param>
        /// <returns></returns>
        public static DateTime getDateTimeFromFilename(string wavfile)
        {
            DateTime result = DateTime.Now;
            if (GetDateTimeFromFilename(wavfile, out DateTime date))
            {
                result = date;
            }
            else
            {
                //Debug.WriteLine("Unable to get date time from {" + wavfile + "}");
            }
            return (result);
        }

        /// <summary>
        ///     parses the recording filename to see if it contains sequences that correspond to a date
        ///     and/or time and if so returns those dates and times combined in a single dateTime parameter.
        ///     returns true if valid dates/times are established and false otherwise.
        ///     Works with both .wav files containing yyyymmdd[-_]hhmmss formatted date time
        ///     or ZC files as YMddhhmm[-_]ss where Y and M may be alphanumeric
        ///     if parsing fails returns new DateTime()
        /// </summary>
        /// <param name="BareFileName"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool GetDateTimeFromFilename(string FullyQualifiedFileName, out DateTime date)
        {
            var BareFileName=Path.GetFileName(FullyQualifiedFileName);
            date = new DateTime();
            var pattern =
                @"(19|20)?([0-9]{2})[-_\s]?([0-1][0-9])[-_\s]?([0-3][0-9])[-_\s]?([0-2][0-9])[-_:\s]?([0-5][0-9])[-_:\s]?([0-5][0-9])";
            var match = Regex.Match(BareFileName, pattern);
            if (match.Success)
            {
                if (match.Groups.Count >= 5)
                {
                    var year = match.Groups[1].Value.Trim() + match.Groups[2].Value.Trim();
                    var month = match.Groups[3].Value.Trim();
                    var day = match.Groups[4].Value.Trim();
                    var hour = "00";
                    var minute = "00";
                    var second = "00";
                    if (match.Groups.Count >= 8)
                    {
                        hour = match.Groups[5].Value.Trim();
                        minute = match.Groups[6].Value.Trim();
                        second = match.Groups[7].Value.Trim();
                    }

                    // we need to force the year representation to four digit form
                    if (year.Length == 2)
                    {
                        // we only have the last two digitis of the year, so we have to deduce the century
                        if (int.TryParse(year, out int iyear))
                        {
                            var yearNow = DateTime.Now.Year - 2000; //Assume we are in the 21st century, after that needs a rewrite
                            // we now have the current short year
                            if (iyear > yearNow)
                            {
                                iyear += 1900;
                            }
                            else
                            {
                                iyear += 2000;
                            }
                            year = iyear.ToString();
                        }
                    }

                    var result = new DateTime();
                    var enGB = new CultureInfo("en-GB");
                    var extractedString = year + "/" + month + "/" + day + " " + hour + ":" + minute + ":" + second;

                    if (DateTime.TryParseExact(extractedString, "yyyy/MM/dd HH:mm:ss", null, DateTimeStyles.AssumeLocal,
                        out result))
                    {
                        ////Debug.WriteLine("Found date time of " + result + " in " + BareFileName);
                        date = result;
                        return true;
                    }
                }
            }
            // Only gets here if it did not find a normally coded filename and has already returned true
            if (BareFileName.EndsWith(".zc", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    pattern = @"([0-9A-Z]{1})([0-9A-Z]{1})([0-9]{2})([0-9]{2})([0-9]{2})_([0-9]{2})";
                    match = Regex.Match(Path.GetFileName(BareFileName), pattern);
                    if (match.Success && match.Groups.Count == 7)
                    {
                        char c = match.Groups[1].Value[0];
                        int year;
                        if (char.IsDigit(c))
                        {
                            year = 1990 + (int)(c - '0');
                        }
                        else
                        {
                            year = 2000 + (int)(c - 'A');
                        }

                        c = match.Groups[2].Value[0];
                        int month;
                        if (char.IsDigit(c))
                        {
                            month = (int)(c - '0');
                        }
                        else
                        {
                            month = 10 + (int)(c - 'A');
                        }

                        var day = int.Parse(match.Groups[3].Value.Trim());
                        var hour = int.Parse(match.Groups[4].Value.Trim());
                        var minute = int.Parse(match.Groups[5].Value.Trim());
                        var second = int.Parse(match.Groups[6].Value.Trim());
                        var result = new DateTime(year, month, day, hour, minute, second);
                        date = result;
                        return (true);
                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine("Error parsing .zc filename " + BareFileName + "; " + ex.Message);
                    return (false);
                }
            }

            //Debug.WriteLine("No datetime found in " + BareFileName);
            return false;
        }
        
        /// <summary>
        ///     Gets the duration of the file. (NB would be improved by using various Regex to parse the
        ///     filename into dates and times for .wav or .zc files
        /// </summary>
        /// <param name="fileName">
        ///     Name and path of the file.
        /// </param>
        /// <param name="wavfile">
        ///     The name and path of the wavfile.
        /// </param>
        /// <param name="fileStart">
        ///     The file start Date and Time.
        /// </param>
        /// <param name="fileEnd">
        ///     The file end Date and Time.
        /// </param>
        /// <returns>
        /// </returns>
        public static TimeSpan GetFileDatesAndTimes(string fileName, out string wavfile, out DateTime fileStart,
            out DateTime fileEnd)
        {
            fileStart = DateTime.Now;
            fileEnd = new DateTime();

            var duration = new TimeSpan(0L);
            wavfile = fileName;
            try
            {
                var wavfilename = Path.ChangeExtension(fileName, ".wav");

                var zcFileName = Path.ChangeExtension(fileName, ".zc");
                if (File.Exists(zcFileName)) fileName = zcFileName;
                if (File.Exists(wavfilename)) fileName = wavfilename; // fileName now explicitly the wav or zc filename
                                                                      // priority to the wavfile if both exist

                if ((File.Exists(wavfilename) || File.Exists(zcFileName)) && ((new FileInfo(fileName)?.Length ?? 0) > 0L))
                {
                    //var info = new FileInfo(fileName);



                    var fa = File.GetAttributes(fileName); // OK for both .wav and .zc
                    DateTime created = File.GetCreationTime(fileName);  // OK for both .wav and .zc
                    if (created.Year < 1990)
                    {
                        // files created earlier than this are likely to be corrupt or to have had an invalid or no creation date
                        created = DateTime.Now;
                    }
                    DateTime modified = File.GetLastWriteTime(fileName); //// OK for both .wav and .zc
                    if (modified.Year < 1990)
                    {
                        modified = DateTime.Now;
                    }

                    if (!Tools.GetDateTimeFromFilename(fileName, out DateTime named))  // OK for both .wav and .zc
                    {
                        named = Tools.getDateTimeFromFilename(fileName); //// OK for both .wav and .zc
                    }
                    DateTime recorded = GetDateTimeFromMetaData(fileName, out duration, out string zcTextHeader);
                    if (recorded.Year < 1990)
                    {
                        // unlikely to have been generating wamd or guano files before 1990
                        recorded = DateTime.Now;
                    }

                    // set fileStart to the earliest of the three date times since we don't which if any have been
                    // corrupted by copying since the file was recorded, but the earliest must be our best guess for
                    // the time being.
                    if (fileStart > created) fileStart = created;
                    if (fileStart > modified) fileStart = modified;
                    if (fileStart > named) fileStart = named;
                    if (fileStart > recorded) fileStart = recorded;

                    if (File.Exists(wavfilename)) // get the duration from the metadata for .wav files
                    {
                        using (BPASpectrogramM.AudioFileReaderM afr=new BPASpectrogramM.AudioFileReaderM(fileName))
                        {
                            duration = afr.FormatInfo?.Duration ?? TimeSpan.Zero;
                            fileEnd = fileStart + duration;
                            wavfile = wavfilename;
                            
                            return (duration);
                        }
                    }
                    else if (File.Exists(zcFileName)) // assume a duration of 15s for .zc files
                    {
                        duration = TimeSpan.FromSeconds(15);
                        fileEnd = fileStart + duration;
                        wavfile = zcFileName;
                        return (duration);
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                //Debug.WriteLine(ex);
            }
            fileEnd = fileStart + duration;
            return duration;
        }


        /// <summary>
        ///     Returns the path component from the fully qualified file name
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        public static string GetPath(string wavFile)
        {
            if (string.IsNullOrWhiteSpace(wavFile)) return "";
            if (wavFile.EndsWith(@"\")) return wavFile;
            if (!wavFile.Contains(@"\")) return "";

            return wavFile.Substring(0, wavFile.LastIndexOf(@"\") + 1);
        }


        /// <summary>
        ///     Extension method for IEnumerable(T) to check if the list is null or empty
        ///     before committing to a foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return !(list?.Any() ?? false);
        }

        public static void SetFolderIcon(string path, string iconPath, string folderToolTip)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            /* Remove any existing desktop.ini */
            if (File.Exists(path + @"desktop.ini")) File.Delete(path + @"desktop.ini");

            /* Write the desktop.ini */
            using (var sw = File.CreateText(path + @"desktop.ini"))
            {
                if (sw != null)
                {
                    sw.WriteLine("[.ShellClassInfo]");
                    sw.WriteLine("InfoTip=" + folderToolTip);
                    sw.WriteLine("IconResource=" + iconPath);
                    sw.WriteLine("IconIndex=0");
                    sw.Close();
                }
            }

            /* Set the desktop.ini to be hidden */
            File.SetAttributes(path + @"desktop.ini",
                File.GetAttributes(path + @"desktop.ini") | FileAttributes.Hidden);

            /* Set the path to system */
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.System);
        }


        /// <summary>
        ///     Given a filename removes the path if any
        /// </summary>
        /// <param name="textFileName"></param>
        /// <returns></returns>
        public static string StripPath(string textFileName)
        {
            if (textFileName.EndsWith(@"\")) return "";
            if (textFileName.Contains(@"\")) textFileName = textFileName.Substring(textFileName.LastIndexOf(@"\") + 1);
            ////Debug.WriteLine("Open text file:-" + textFileName);
            return textFileName;
        }

        /// <summary>
        ///     Parses a line in the format 00'00.00 into a TimeSpan the original strting has been
        ///     matched by a Regex of the form [0-9\.\']+
        /// </summary>
        /// <param name="value">
        ///     The stats.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public static TimeSpan TimeParse(string value)
        {
            var regPattern = @"([0-9]*\')?([0-9]+)[\.]?([0-9]*)";
            var minutes = 0;
            var seconds = 0;
            var millis = 0;

            var result = Regex.Match(value, regPattern);
            if (result.Success && result.Groups.Count >= 4)
            {
                // we have matched and identified the fields
                if (!string.IsNullOrWhiteSpace(result.Groups[1].Value))
                {
                    var minstr = result.Groups[1].Value.Substring(0, result.Groups[1].Value.Length - 1);
                    var r1 = int.TryParse(minstr, out minutes);
                }

                if (!string.IsNullOrWhiteSpace(result.Groups[2].Value))
                {
                    var r2 = int.TryParse(result.Groups[2].Value, out seconds);
                }

                if (!string.IsNullOrWhiteSpace(result.Groups[3].Value))
                {
                    var s = "0." + result.Groups[3].Value;
                    var r3 = double.TryParse(s, out var dm);
                    millis = (int)(dm * 1000);
                }
            }

            var ts = new TimeSpan(0, 0, minutes, seconds, millis);
            return ts;
        }



        /// <summary>
        ///     Given an external process, waits for the process to be responding and for Inputidle as well
        ///     as a static 100ms wait at the start.  If the ExternalProcess exits during the wait then the
        ///     function returns false, otherwise it returns true.  Will wait indefinitiely if the process
        ///     does not exit and never becomes idle.
        /// </summary>
        /// <param name="externalProcess"></param>
        /// <param name="marker"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static bool WaitForIdle(Process externalProcess, string marker = "", string location = "")
        {
            Debug.Write(marker);
            Thread.Sleep(100);

            for (int i = 0; i < 5; i++)
            {
                if (externalProcess.WaitForInputIdle(100)) break;
                if (externalProcess.Responding) break;
                Debug.Write(marker);
                if (externalProcess.HasExited)
                {
                    externalProcess.Close();
                    Debug.Write("Process Exited at:- " + location);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Given a string, removes any curly brackets and replaces them around all the text following
        ///     a $ if any, or around the entire string if there is no $
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static string AdjustBracketedText(string comment)
        {
            comment = comment.Replace("{", " ");
            comment = comment.Replace("}", " ");
            comment = comment.Trim();
            if (comment.Contains("$"))
                comment = comment.Replace("$", "${");
            else
                comment = "{" + comment;
            comment = comment + "}";
            return comment;
        }


        /// <summary>
        ///     Converts the double in seconds to time span.
        /// </summary>
        /// <param name="value">
        ///     The stats.
        /// </param>
        /// <returns>
        /// </returns>
        internal static TimeSpan ConvertDoubleToTimeSpan(double? value)
        {
            if (value == null) return new TimeSpan();
            var seconds = (int)Math.Floor(value.Value);
            var millis = (int)Math.Round((value.Value - seconds) * 1000.0d);

            var minutes = Math.DivRem(seconds, 60, out seconds);
            return new TimeSpan(0, 0, minutes, seconds, millis);
        }

        /// <summary>
        ///     looks in a string for the sequence .wav and truncates the string after that
        ///     then removes leading charachters up to the last \ to remove any path and pre-amble.
        ///     This should leave just the filename.wav unless there was textual preamble to the filename
        ///     which cannot be distinguished from part of the name.
        ///     returns null if no such string is found
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static string ExtractWavFilename(string description)
        {
            if (description.ToUpper().Contains(".WAV"))
            {
                var fullname = description.Substring(0, description.ToUpper().IndexOf(".WAV") + 4);
                if (fullname.Contains(@"\")) fullname = fullname.Substring(description.LastIndexOf(@"\") + 1);
                return fullname;
            }

            return null;
        }


        internal static string FormattedValuePair(string header, double? value, double? variation)
        {
            var result = header;

            if (value == null || value <= 0.0d) return "";
            result = (header ?? "") + $"{value:##0.0}";
            if (variation != null && variation >= 0.0) result = result + "+/-" + $"{variation:##0.0}";

            return result;
        }

        /// <summary>
        /// checks to see if the passed comment contains a bracketed string containing an Auto ID
        /// and if so returns the AutoID
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        internal static string getAutoIdFromComment(string comment)
        {
            string? autoID = null;
            if (comment.Contains("("))
            {
                string pattern = @"\(Auto=([^\)]+)";
                var match = Regex.Match(comment, pattern);
                if (match.Success)
                {
                    if (match.Groups != null && match.Groups.Count >= 2)
                    {
                        autoID = match.Groups[1].Value;
                    }
                }
            }
            return (autoID);
        }

        /// <summary>
        ///     returns a DateTime containing the date defined in a sessionTag of the format
        ///     [alnum]*[-_][alnum]+[-_]20yymmdd
        /// </summary>
        /// <returns></returns>
        internal static DateTime GetDateFromTag(string tag)
        {
            var result = new DateTime();

            var dateField = tag.Substring(tag.LastIndexOfAny(new[] { '-', '_' }));
            if (dateField.Length == 9)
            {
                var stryear = dateField.Substring(1, 4);
                var strmonth = dateField.Substring(5, 2);
                var strday = dateField.Substring(7, 2);
                var Year = DateTime.Now.Year;
                var Month = DateTime.Now.Month;
                var Day = DateTime.Now.Day;
                int.TryParse(stryear, out Year);
                int.TryParse(strmonth, out Month);
                int.TryParse(strday, out Day);
                result = new DateTime(Year, Month, Day);
            }

            return result;
        }


        /// <summary>
        ///     Takes a string with two values as either mean+/-variation
        ///     or as min-max, converts them tot he standard mean and
        ///     variation format as two doubles and returns those two
        ///     values.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="mean"></param>
        /// <param name="variation"></param>
        /// <returns></returns>
        internal static bool GetValuesAsMeanAndVariation(string parameters, out double mean, out double variation)
        {
            mean = 0.0d;
            variation = 0.0d;
            if (string.IsNullOrWhiteSpace(parameters)) return false;

            var match = Regex.Match(parameters, @"\s*([0-9.]+)\s*([+\-\/]*)\s*([0-9.]*)");
            /* then parse the two doubles, read the middle matched segment
             * and do the conversion as below if it is necessary,
             * then assign the values and return;
             * */

            if (match.Success)
            {
                for (var i = 1; i < match.Groups.Count; i++)
                {
                    if (i == 0) continue;
                    double.TryParse(match.Groups[i].Value, out var v);
                    switch (i)
                    {
                        case 1:
                            mean = v;
                            break;

                        case 2: break;
                        case 3:
                            variation = v;
                            break;
                    }
                }

                if (match.Groups.Count > 2)
                {
                    var sep = match.Groups[2].Value;
                    if (!sep.Contains("+/-"))
                    {
                        var temp = (mean + variation) / 2;
                        variation = Math.Max(mean, variation) - temp;
                        mean = temp;
                    }
                }

                return true;
            }

            return false;
        }

        private static bool isFirstError = false;
        /// <summary>
        ///     Writes an information string to the Error Log without the additional burden of a stack trace
        /// </summary>
        /// <param name="v"></param>
        internal static void InfoLog(string error)
        {
            var path = @"C:\BRM-Error\";
            var errorFile = Path.Combine(path , "BRM-Info-Log.txt");
            try
            {
                var build = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString()??"";

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                if(isFirstError && File.Exists(errorFile))
                {
                    File.Delete(errorFile);
                }

                using (var stream = new StreamWriter(errorFile, true))
                {
                    if (isFirstError)
                    {
                        stream.WriteLine(@"
==========================================================================================================

");
                        stream.WriteLine(build);
                        isFirstError = false;
                    }
                    stream.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":-" +
                                     error);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("\n\n**** Error writing to info log" + ex);
                File.AppendAllText(path + "FatalError.txt", "Error writing to Info Log file!!!!!!!!!!!\n");
            }
        }





        /// <summary>
        /// Opens the C:\Audacity Config file and edits the export labels folder to
        /// the current selected folder;
        /// </summary>
        /// <param name="folderPath"></param>
        internal static void SetAudacityExportFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                var info = new FileInfo(folderPath);
                folderPath = info?.Directory?.FullName ?? "";
            }

            string
                moddedFolderPath =
                    folderPath.Replace(@"\\",
                        @"\"); // first ensure that the path only contains single backslashes - which it should
            moddedFolderPath = moddedFolderPath.Replace(@"\", @"\\");
        
// then ensure that all backslashes are doubled for insertion intothe config file
            string configFile = @"C:\audacity-win-portable\Portable Settings\audacity.cfg";
            if (File.Exists(configFile) && ((new FileInfo(configFile)?.Length ?? 0)  > 0L))
            {
                bool modOk = false;
                bool modPathOk = false;
                var lines = File.ReadAllLines(configFile);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("[Directories/Export"))
                    {
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            if (lines[j].StartsWith("Default="))
                            {
                                lines[j] = $"Default={moddedFolderPath}";
                            }
                            if (lines[j].StartsWith("LastUsed"))
                            {
                                lines[j] = $"LastUsed={moddedFolderPath}";
                            }
                            if (lines[j].StartsWith("[")) // starting anoother settings category
                            {
                                i = j - 1;  // so continue by re-examining this line at the higher level
                                j = lines.Length; // and get out of the j for-loop
                            }
                        }
                    }
                    else if (lines[i].StartsWith("[Module]"))
                    {
                        if (lines[i + 1].StartsWith("mod-script-pipe"))
                        {
                            lines[i + 1] = "mod-script-pipe=1";
                        }
                        else
                        {
                            lines[i] = "[Module]\nmod-script-pipe=1";
                        }
                        modOk = true;
                    }
                    else if (lines[i].StartsWith("[ModulePath]"))
                    {
                        if (lines[i + 1].StartsWith("mod-script-pipe"))
                        {
                            lines[i + 1] = @"mod-script-pipe=C:\\audacity-win-portable\\modules\\mod-script-pipe.dll";
                        }
                        else
                        {
                            lines[i] = @"[ModulePath]
mod-script-pipe=C:\\audacity-win-portable\\modules\\mod-script-pipe.dll";
                        }
                        modPathOk = true;
                    }
                }
                if (!modOk)
                {
                    lines.Append("[Module]\nmod-script-pipe=1");
                }
                if (!modPathOk)
                {
                    lines.Append(@"[ModulePath]
mod-script-pipe=C:\\audacity-win-portable\\modules\\mod-script-pipe.dll");
                }
                File.WriteAllLines(configFile, lines);
            }
        }

        /// <summary>
        ///     changes the folder icon for the specified folder to a folder symbol with a green tick
        ///     The change may or may not be apparent until a reboot
        /// </summary>
        /// <param name="workingFolder"></param>
        internal static void SetFolderIconTick(string workingFolder)
        {
            SetFolderIcon(workingFolder, @"C:\Windows\system32\SHELL32.dll,144",
                "Data Imported to Bat Recording Manager");
        }



    //private static readonly bool HasErred;




    /// <summary>
    /// Examines file metadata for a guano or wamd section which includes information about
    /// the time the file was originally recorded.
    /// Returns the earliest of the guano or wamd timestamps or returns Now;
    /// </summary>
    /// <param name="wavfile"></param>
    /// <returns></returns>
    private static DateTime GetDateTimeFromMetaData(string wavfile, out TimeSpan duration, out string textHeader)
    {
        DateTime result = DateTime.Now;
        duration = new TimeSpan();
        DateTime guanoTime = result;
        DateTime wamdTime = result;
        textHeader = "";

        if (wavfile.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && File.Exists(wavfile))
        {
            try
            {
                var chunks = ExtractWavMetadataChunks(wavfile);

                foreach (var chunk in chunks)
                {
                    if (chunk.Identifier == "guan")
                    {
                        guanoTime = ReadGuanoTimeStamp(chunk, out duration);
                        if (guanoTime.Year < 2000) guanoTime = DateTime.Now;
                    }
                    else if (chunk.Identifier == "wamd")
                    {
                        wamdTime = ReadWAMDTimeStamp(chunk, out duration);
                        if (wamdTime.Year < 2000) wamdTime = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorLog($"Invalid wavfile {wavfile}:-{ex.Message}");
                return result;
            }

            if (result > guanoTime) result = guanoTime;
            if (result > wamdTime) result = wamdTime;
        }

        return result;
    }



    /// <summary>
    ///     Given a .wav filename (or indeed any other filename) replaces the last four characters
    ///     of the name with .txt and returns that modified string.  Does not do any explicit checks to see
    ///     if the string passed is indeed a filename, with or without a path.
    ///     If the input string is null, empty or less than 4 characters long then the function returns
    ///     an unmodified string
    /// </summary>
    /// <param name="wavFile"></param>
    /// <returns></returns>
    private static string GetMatchingTextFile(string wavFile)
        {
            if (string.IsNullOrWhiteSpace(wavFile) || wavFile.Length < 4) return wavFile;
            wavFile = wavFile.Substring(0, wavFile.Length - 4);
            wavFile = wavFile + ".txt";
            return wavFile;
        }

        /// <summary>
        ///     Given a fully qualified file name, returns the fully qualified name
        ///     of the oldest .wav file in the same folder, based on the last modified date.
        /// </summary>
        /// <param name="wavFile"></param>
        /// <returns></returns>
        private static string GetOldestFile(string wavFile)
        {
            var folder = GetPath(wavFile);
            if (!Directory.Exists(folder)) return null;
            var fileList = Directory.EnumerateFiles(folder, "*.wav");
            //var FILEList= Directory.EnumerateFiles(folder, "*.WAV");
            //fileList = fileList.Concat<string>(FILEList);
            var earliestDate = DateTime.Now;
            var file = "";
            foreach (var f in fileList)
            {
                var thisDate = File.GetLastWriteTime(f);
                if (thisDate < earliestDate)
                {
                    file = f;
                    earliestDate = thisDate;
                }
            }

            return file;
        }

        
        /// <summary>
        /// extracts the timestamp from a guano metadata chunk
        /// </summary>
        /// <param name="wfr"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        private static DateTime ReadGuanoTimeStamp(RiffChunkData chunk, out TimeSpan duration)
        {
            DateTime result = DateTime.Now;
            duration = new TimeSpan();
    
            if (chunk?.Data == null || chunk.Data.Length == 0)
                return result;

            try
            {
                string guanoChunk = System.Text.Encoding.UTF8.GetString(chunk.Data);
                var lines = guanoChunk.Split('\n');
        
                foreach (var line in lines)
                {
                    if (line.Contains("Timestamp"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1)
                        {
                            if (DateTime.TryParse(parts[1].Trim(), out DateTime dt))
                            {
                                result = dt;
                            }
                        }
                    }
                    if (line.Contains("Length"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1)
                        {
                            if (int.TryParse(parts[1].Trim(), out int seconds))
                            {
                                duration = TimeSpan.FromSeconds(seconds);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReadGuanoTimeStamp] Error: {ex.Message}");
            }

            return result;
        }
        
        /// <summary>
        /// extracts the timestamp from a wamd metadata chunk
        /// </summary>
        /// <param name="wfr"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        private static DateTime ReadWAMDTimeStamp(RiffChunkData chunk, out TimeSpan duration)
        {
            DateTime result = DateTime.Now;
            duration = TimeSpan.FromSeconds(15);
            
    if (chunk?.Data == null || chunk.Data.Length == 0)
        return result;

    try
    {
        var entries = new Dictionary<short, string>();
        var bReader = new BinaryReader(new MemoryStream(chunk.Data));

        while (bReader.BaseStream.Position < bReader.BaseStream.Length)
        {
            var type = bReader.ReadInt16();
            var size = bReader.ReadInt32();
            
            if (size < 0 || bReader.BaseStream.Position + size > bReader.BaseStream.Length)
                break;

            var bData = bReader.ReadBytes(size);
            
            if (type > 0)
            {
                try
                {
                    var data = System.Text.Encoding.UTF8.GetString(bData);
                    if (type == 0x0005)
                    {
                        if (DateTime.TryParse(data, out DateTime dt))
                        {
                            result = dt;
                            if (duration > new TimeSpan()) return dt;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ReadWAMDTimeStamp] Error parsing chunk: {ex.Message}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[ReadWAMDTimeStamp] Error: {ex.Message}");
    }

    return result;
}


        /// <summary>
        /// Copies the hmbgnew ini file from resources to be the kaleidoscope current ini file, 
        /// saving the existing on
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal static void SetKaleidoscopeIniFile()
        {
            string iniFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            iniFolder = Path.Combine(iniFolder, @"kaleidoscope");
            if (!Directory.Exists(iniFolder)) return;
            string oldIniFile = Path.Combine(iniFolder, "kaleidoscope.ini");
            string bakfile = Path.Combine(iniFolder, "kaleidoscope.bk");
            
            int index = 1;
            while(File.Exists($"{bakfile}{index}")){
                index++;

            }
            File.Move(oldIniFile, bakfile+$"{index}",overwrite:false);
            string newFile = @".\Resources\kaleidoscope.ini.hmbgnew";
            if (File.Exists(newFile))
            {
                File.Move(newFile, oldIniFile, overwrite : false);
            }

        }

        /// <summary>
        /// Replaces the hmbg ini file with the original (most recent) saved one
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal static void ResetKaleidoscopeIniFile()
        {
            string iniFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            iniFolder = Path.Combine(iniFolder, @"kaleidoscope");
            if (!Directory.Exists(iniFolder)) return;
            string oldIniFile = Path.Combine(iniFolder, "kaleidoscope.ini");
            string bakfile = Path.Combine(iniFolder, "kaleidoscope.bk");
            var fileList = Directory.EnumerateFiles(iniFolder, "kaleidoscope.bk*");
            var infoList = new List<FileInfo>();
            foreach (var file in fileList) infoList.Add(new FileInfo(file));
            if(infoList!=null && infoList.Count > 0)
            {
                var latest=infoList.OrderBy(x=>x.LastWriteTime).FirstOrDefault();
                if (latest != null)
                {
                    var file = latest.FullName;
                    if (File.Exists(file))
                    {
                        File.Move(oldIniFile, @".\Resources\kaleidoscope.ini.hmbgnew"); // save any changes
                        File.Move(file, oldIniFile); // restore the original
                        
                    }
                }
            }

        }

        /// <summary>
        /// Opens a Folder selection dialog based on the provided TopLevel instance, allows only a single selection
        /// and returns that as a valid Path having removed any leading 'file:\'.  On failure returns an empty string.
        /// </summary>
        /// <param name="topLevel">TopLevel instance from GetTopLevel(this)</param>
        /// <returns></returns>
        public static async Task<string?> GetWavFileFolderAsync()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            string? result = "";
            try
            {
                result = await PickFolder(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                //Debug.WriteLine("ERR Pick Folder Operation Cancelled");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(result))
            {
                result = Path.GetDirectoryName(result);
            }

            return result;
        }

        private static async Task<string> PickFolder(CancellationToken cancellationToken)
        {
            FileResult files = null;
            //var folderResult = await FolderPicker.Default.PickAsync(cancellationToken);
            try
            {
                Debug.WriteLine("Opening FilePicker..");
                await Task.Yield();
                await Task.Delay(50);
                files = await MainThread.InvokeOnMainThreadAsync(() =>
                    FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Pick a file" }));

                Debug.WriteLine("FilePicker closed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FilePicker Error: "+ex.Message);
            }

            string file = files?.FullPath??"";
            if (!string.IsNullOrWhiteSpace(file))
            {
                
            
                return file;
            }
            return "";
        }

        /// <summary>
        /// runs the supplied file as an external process.  file may be an .exe file or use an
        /// implied application based on file extension known to the OS
        /// </summary>
        /// <param name="helpfile"></param>
        internal static void Execute(string helpfile)
        {
            if (File.Exists(helpfile))
            {
                try
                {
                    Process ExternalProcess = new Process();
                    if (ExternalProcess == null) return;
                    if (!Path.GetExtension(helpfile).ToUpper().EndsWith("EXE"))
                    {
                        ExternalProcess.StartInfo.FileName = "cmd.exe";
                        ExternalProcess.StartInfo.Arguments = helpfile;
                    }
                    else
                    {
                        ExternalProcess.StartInfo.FileName = helpfile;
                    }
                    //externalProcess.StartInfo.Arguments = folder;
                    ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                    ExternalProcess.Start();
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"Trying to run {helpfile}:- {ex.Message}");
                    Tools.ErrorLog($"Trying to run {helpfile}:- {ex.Message}");
                }
            }
        }


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        
        /// <summary>
        ///     Assumes that a filename may include the date in the format yyyymmdd
        ///     preceded and followed by either - or _
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static DateTime? GetDateFromFilename(string fileName)
        {
            DateTime? result = null;
            if (string.IsNullOrWhiteSpace(fileName)) return result;

            var pattern = @"[_-]([0-9]{4}).?([0-9]{2}).?([0-9]{2})[_-]";
            var match = Regex.Match(fileName, pattern);
            if (match.Success)
            {
                var year = -1;
                var month = -1;
                var day = -1;

                if (match.Groups.Count > 3)
                {
                    int.TryParse(match.Groups[1].Value, out year);
                    int.TryParse(match.Groups[2].Value, out month);
                    int.TryParse(match.Groups[3].Value, out day);
                }

                if (year > 1970 && month >= 0 && month <= 12 && day >= 0 && day <= 31)
                {
                    result = new DateTime(year, month, day);

                    var hour = -1;
                    var minute = -1;
                    var secs = -1;
                    pattern = @"[_-]([0-9]{4}).?([0-9]{2}).?([0-9]{2})[_-]([0-9]{2}).?([0-9]{2}).?([0-9]{2})";
                    match = Regex.Match(fileName, pattern);
                    if (match.Success && match.Groups.Count > 6)
                    {
                        int.TryParse(match.Groups[4].Value, out hour);
                        int.TryParse(match.Groups[5].Value, out minute);
                        int.TryParse(match.Groups[6].Value, out secs);
                        if (hour >= 0 && hour <= 24 && minute >= 0 && minute <= 60 && secs >= 0 && secs <= 60)
                            result = new DateTime(year, month, day, hour, minute, secs);
                    }
                }
            }

            return result;
        }

    /// <summary>
    /// Extracts all metadata chunks from a WAV file without using NAudio
    /// Returns chunks like "guan" (Guano metadata) and "wamd" (WAMD metadata)
    /// </summary>
    /// <param name="wavFilePath">Path to the WAV file</param>
    /// <returns>List of RiffChunkData for all non-standard chunks found</returns>
    public static List<RiffChunkData> ExtractWavMetadataChunks(string wavFilePath)
    {
        var chunks = new List<RiffChunkData>();

        if (!File.Exists(wavFilePath))
        {
            Debug.WriteLine($"[ExtractWavMetadataChunks] File not found: {wavFilePath}");
            return chunks;
        }

        try
        {
            using (FileStream fs = File.OpenRead(wavFilePath))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Read and validate RIFF header
                string riffHeader = new string(reader.ReadChars(4));
                if (riffHeader != "RIFF")
                {
                    Debug.WriteLine($"[ExtractWavMetadataChunks] Invalid RIFF header: {riffHeader}");
                    return chunks;
                }

                int fileSize = reader.ReadInt32();
                string waveHeader = new string(reader.ReadChars(4));
                if (waveHeader != "WAVE")
                {
                    Debug.WriteLine($"[ExtractWavMetadataChunks] Invalid WAVE header: {waveHeader}");
                    return chunks;
                }

                // Read all chunks
                while (fs.Position < fs.Length)
                {
                    // Read chunk identifier and size
                    if (fs.Position + 8 > fs.Length) break;

                    string chunkId = new string(reader.ReadChars(4));
                    int chunkSize = reader.ReadInt32();

                    if (chunkSize < 0 || fs.Position + chunkSize > fs.Length)
                    {
                        Debug.WriteLine($"[ExtractWavMetadataChunks] Invalid chunk size: {chunkSize} for {chunkId}");
                        break;
                    }

                    // Standard chunks we skip
                    if (chunkId != "fmt " && chunkId != "data")
                    {
                        // This is a metadata/extra chunk
                        byte[] chunkData = reader.ReadBytes(chunkSize);
                        chunks.Add(new RiffChunkData
                        {
                            Identifier = chunkId.Trim(),
                            Data = chunkData,
                            Size = chunkSize
                        });

                        Debug.WriteLine($"[ExtractWavMetadataChunks] Found chunk: {chunkId} (size: {chunkSize})");
                    }
                    else
                    {
                        // Skip standard chunks
                        fs.Seek(chunkSize, SeekOrigin.Current);
                    }

                    // Handle padding (chunks are word-aligned)
                    if (chunkSize % 2 != 0)
                    {
                        fs.Seek(1, SeekOrigin.Current);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExtractWavMetadataChunks] Error: {ex.Message}");
            ErrorLog($"Error extracting WAV metadata: {ex.Message}");
        }

        return chunks;
    }
} // end of Class Tools

    //########################################################################################################################
    //########################################################################################################################

    


    /// <summary>
    ///     A simple class to accommodate the parsed and analysed contents of the wamd
    ///     metadata chunk from a .wav file.  The data in the chunk is identified by a
    ///     numerical type and contents which should be a string.  The data structure
    ///     holds items for each known type and getters return the contents by name or add
    ///     contents by type.
    /// </summary>
    public class WAMD_Data
    {
        /// <summary>
        ///     initialises the data structure with empty strings throughout
        /// </summary>
        public WAMD_Data()
        {
            model = "";
            version = "";
            header = "";
            timestamp = "";
            source = "";
            note = "";
            identification = "";
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public string comment
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get
            {
                var s = note + " " + identification;
                return s.Trim();
            }
        }

        public double duration { get; set; }
        public string header { get; private set; }
        public string identification { get; private set; }

        public Tuple<short, string> item
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            set
            {
                switch (value.Item1)
                {
                    case 1:
                        model = value.Item2;
                        break;

                    case 3:
                        version = value.Item2;
                        break;

                    case 4:
                        header = value.Item2;
                        break;

                    case 5:
                        timestamp = value.Item2;
                        break;

                    case 12:
                        identification = value.Item2;
                        break;

                    case 10:
                        note = value.Item2;
                        break;
                }
            }
        }

        public string model { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string note { get; private set; }
        public string source { get; }
        public string timestamp { get; private set; }
        public string version { get; private set; }

        public double? versionAsDouble { get; internal set; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct Lpshfoldercustomsettings
    {
        public uint dwSize;
        public uint dwMask;
        public IntPtr pvid;
        public string pszWebViewTemplate;
        public uint cchWebViewTemplate;
        public string pszWebViewTemplateVersion;
        public string pszInfoTip;
        public uint cchInfoTip;
        public IntPtr pclsid;
        public uint dwFlags;
        public string pszIconFile;
        public uint cchIconFile;
        public int iIconIndex;
        public string pszLogo;
        public uint cchLogo;
    }

    /// <summary>
    /// Represents a RIFF chunk from a WAV file
    /// </summary>
    public class RiffChunkData
    {
        public string Identifier { get; set; }
        public byte[] Data { get; set; }
        public int Size { get; set; }
    }

   


