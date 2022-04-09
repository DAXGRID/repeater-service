namespace RepeaterService;

record BusPair(Bus Source, Bus Destination, Repeat Repeat);

// TODO: Bad name for now
internal static class BusPairFactory
{
    public static BusPair Create(Repeat repeat)
    {
        return new(new Bus(repeat), new Bus(repeat), repeat);
    }
}
