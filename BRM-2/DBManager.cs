namespace BRM_2;
    internal static class DBManager
    {
        


        public static string defaultDatabase = Path.Combine(FileSystem.AppDataDirectory, "BRMLiteM.db");

        public static string workingDatabase = Path.Combine(FileSystem.AppDataDirectory, "BRMLiteM.db");

        private static string _dbEncryptionKey = SecureStorage.GetAsync("dbKey").Result;

        private static bool isBatListLoaded = false;

        private static bool LoadingBats = false;

        public async static Task<SQLiteAsyncConnection> GetConnection()
        {
           // System.Diagnostics.Debugger.Break();  // Force debugger to pause here
            SQLiteConnection.EnsureInitialized();

            //var db=new SQLiteAsyncConnection(workingDatabase);
            var db = new SQLiteAsyncConnection(workingDatabase);
            

                if (!isBatListLoaded && !LoadingBats)
                {
                    LoadingBats = true;
                    try
                    {

                        //        _ = await db.CreateTablesAsync(CreateFlags.None, new Type[] { typeof(BatTable), typeof(BatTag), typeof(Call),
                        //typeof(IdedBatTable),typeof(LabelledSegmentTable),
                        //typeof(Meta), typeof(RecordingTable),typeof(RecordingSessionTable)});
                        await db.CreateTablesAsync(CreateFlags.None, new Type[] { typeof(BatTable), typeof(BatTag), typeof(Call),
                typeof(IdedBatTable),typeof(LabelledSegmentTable),
                typeof(Meta), typeof(RecordingTable),typeof(RecordingSessionTable)});
                        /*
                        Debug.WriteLine("DB Connection");
                        _ = await db.CreateTableAsync<BatTable>();
                        Debug.WriteLine("Bat");
                        _ = await db.CreateTableAsync<BatTag>();
                        Debug.WriteLine("BatTag");
                        _ = await db.CreateTableAsync<Call>();
                        Debug.WriteLine("Call");
                        _ = await db.CreateTableAsync<IdedBatTable>();
                        Debug.WriteLine("IdedBat");
                        _ = await db.CreateTableAsync<LabelledSegmentTable>();
                        Debug.WriteLine("Segment");
                        _ = await db.CreateTableAsync<Meta>();
                        Debug.WriteLine("Meta");
                        _ = await db.CreateTableAsync<RecordingTable>();
                        Debug.WriteLine("Recording");
                        _ = await db.CreateTableAsync<RecordingSessionTable>();
                        Debug.WriteLine($"{nameof(RecordingSessionTable)}");
                        */
                        //db = null;
                        //db = new SQLiteConnection(workingDatabase);

                        await PreloadBatReference(db);
                        isBatListLoaded = true;

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        isBatListLoaded = false;
                    }
                    finally
                    {

                        LoadingBats = false;
                    }
                }
                Debug.WriteLine("Bats Loaded");
            

            return new SQLiteAsyncConnection(workingDatabase);

        }

        /// <summary>
        /// Reads the BatReference data from an xml file in Resources and installs the definitions
        /// in the database if the database does not already contain bat references
        /// </summary>
        /// <param name="db"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static async Task PreloadBatReference(SQLiteAsyncConnection db)
        {
#if DEBUG
            CheckBatReference(); //just for debugging
#endif
            var batList =  await db.Table<BatTable>().ToListAsync();
            Debug.WriteLine($"Database has {batList.Count} bats");
            if (!batList.Any())
            {
                var xmlBats =  await GetBatReference();
                Debug.WriteLine($"BatReference sourced {xmlBats.Count} bats");
                foreach (var batXL in xmlBats ?? new List<XElement>())
                {
                     DBAccess.InsertBatElementAsync(batXL, db);
                        
                    
                }
                batList =  await db.Table<BatTable>().ToListAsync();
                Debug.WriteLine($"Database has added {batList.Count} bats");
            }
           
        }

        /// <summary>
        /// checks that we have a BatReference File in the package
        /// </summary>
        private static void CheckBatReference()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "BRM_2.Resources.Data.BatReferenceXMLFile.xml";
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) { Debug.WriteLine("No Streasm"); return; }
                using (StreamReader reader = new StreamReader(stream))
                {

                    if (reader == null) { Debug.WriteLine("No Reader"); return; }
                    string content = reader.ReadToEnd();
                }
            }
            //Debug.WriteLine(content);
        }

        private static async Task<List<XElement>> GetBatReference()
        {
            List<XElement> result= new List<XElement>();
            
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "BRM_2.Resources.Data.BatReferenceXMLFile.xml";
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) { Debug.WriteLine("No Streasm"); return (result); }
                    var xData = await XElement.LoadAsync(stream, LoadOptions.None, new CancellationToken());
                    result = xData.Descendants("Bat").ToList<XElement>();
                }
            }
            catch (Exception ex)
            {
                var toast = Toast.Make(ex.Message);
                toast?.Show(new CancellationToken());
            }
            return(result   );
        }

       

    }