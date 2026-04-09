namespace BRM_2
{
    /// <summary>
    /// A class to contain data for a Herts Atlas fromat report entry
    /// </summary>
    internal class HertsAtlasItem
    {
        private string file;
        private DateTime date;
        private string location;
        private double latitude;
        private double longitude;
        private string GridRef;
        private string species;
        private int passes;
        private string equipment;
        private string comment;
        private string observer;

        internal HertsAtlasItem(string file, DateTime date, string location, double latitude, double longitude, string gridRef, string species, int passes, string equipment, string comment, string observer)
        {
            this.file = file;
            this.date = date;
            this.location = location;
            this.latitude = latitude;
            this.longitude = longitude;
            GridRef = gridRef;
            this.species = species;
            this.passes = passes;
            this.equipment = equipment;
            this.comment = comment;
            this.observer = observer;
        }

        internal static string Headers()
        {
            return $"{nameof(file)}," +
                $"{nameof(date)}," +
                $"{nameof(location)}," +
                $"{nameof(latitude)}," +
                $"{nameof(longitude)}," +
                $"{nameof(GridRef)}," +
                $"{nameof(species)}," +
                $"{nameof(passes)}," +
                $"{nameof(equipment)}," +
                $"{nameof(comment)}," +
                $"{nameof(observer)}";

        }

        internal string ToString()
        {
            return $"{file},{date.ToString("yyyy-MM-dd")},{location},{latitude},{longitude},{GridRef},{species},{passes},{equipment},{comment},{observer}";
        }
    }
}
