namespace BRM_2;

public class BatSummary
{
    public BatSummary() { }

    public BatSummary(LabelledSegmentTable segment) { }

    public string BatName { get; set; } = "";

    public TimeSpan BatDuration { get; set; }=new TimeSpan();

    public bool ByAutoId { get; set; } = false;

    public int NumSegments { get; set; } = 0;

    public int Passes { get => Tools.ToPasses(BatDuration); }

    public double TotalDuration { get => BatDuration.TotalSeconds; }

    public double AvgDuration { get => TotalDuration / NumSegments; }

    public override String ToString()
    {
        string result;
         result=$"({BatName} - {Tools.ToPasses(BatDuration)} passes in {NumSegments} Total:- {BatDuration.TotalSeconds:0.00}s)";
        
        return result ;
    }
}
