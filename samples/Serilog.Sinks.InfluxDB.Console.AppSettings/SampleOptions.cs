namespace Serilog.Sinks.InfluxDB.Console.AppSettings;

public class SampleOptions
{
    public const string Sample = "Sample";
    public int Number { get; }
    public SampleOptions(int number)
    {
        Number = number;
    }

    public SampleOptions()
    {
        Number = 10000;
    }
}