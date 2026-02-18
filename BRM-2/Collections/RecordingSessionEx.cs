using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace BRM_2.Collections;

    public class RecordingSessionEx: RecordingSessionTable,INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string PropertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }


        public int numRecordings { get; set; } = -1;

        
        public int NumberOfRecordings
        {
            get
            {
                if (numRecordings > 0) return numRecordings;
                else
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    GetNumRecordings();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    return 1;
                }
            }

            set { numRecordings = value; OnPropertyChanged(); }
        }


        [XmlArray("Recordings")]
        [XmlArrayItem("Recording")]
        public List<RecordingEx> recordings { get; set; } = new List<RecordingEx>();

        public RecordingSessionEx() : base()
        {
        }

        public RecordingSessionEx(RecordingSessionTable rst) : base()
        {
            this.ID = rst.ID;
            this.SessionTag = rst.SessionTag;
            this.SessionStart = rst.SessionStart;
            this.SessionEnd = rst.SessionEnd;
            this.Temp = rst.Temp;
            this.Equipment = rst.Equipment;
            this.microphone = rst.microphone;
            this.Operator = rst.Operator;
            this.Location = rst.Location;
            this.LocationGPSLongitude = rst.LocationGPSLongitude;
            this.LocationGPSLatitude = rst.LocationGPSLatitude;
            this.SessionNotes = rst.SessionNotes;
            this.OriginalFilePath = rst.OriginalFilePath;
            this.Sunset = rst.Sunset;
            this.Weather = rst.Weather;
        }

        public RecordingSessionTable GetTable() {            
            RecordingSessionTable rst = new RecordingSessionTable();
            rst.ID = this.ID;
            rst.SessionTag = this.SessionTag;
            rst.SessionStart = this.SessionStart;
            rst.SessionEnd = this.SessionEnd;
            rst.Temp = this.Temp;
            rst.Equipment = this.Equipment;
            rst.microphone = this.microphone;
            rst.Operator = this.Operator;
            rst.Location = this.Location;
            rst.LocationGPSLongitude = this.LocationGPSLongitude;
            rst.LocationGPSLatitude = this.LocationGPSLatitude;
            rst.SessionNotes = this.SessionNotes;
            rst.OriginalFilePath = this.OriginalFilePath;
            rst.Sunset = this.Sunset;
            rst.Weather = this.Weather;
            return rst;
        }

        private async Task GetNumRecordings()
        {
            await GetNumRecordingsAsync();
        }

        private async Task GetNumRecordingsAsync()
        {
            var nr = await DBAccess.GetNumRecordingsForSession(ID);
            NumberOfRecordings = nr;
        }


        internal static bool IsTagUnique(string value)
        {
            throw new NotImplementedException();
        }
    }
