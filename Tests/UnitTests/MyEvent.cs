namespace AzureBusDepot.UnitTests
{
    public class MyEvent
    {
        public int Id { get; set; }

        public string Name { get; set; }

        // For the sake of the tests please don't mutate this instance!
        public static readonly MyEvent Default = new MyEvent {Id = 1, Name = "1"};
    }
}
