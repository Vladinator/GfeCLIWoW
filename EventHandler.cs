namespace gfecliwow
{
    public class EventHandlerArgs : EventArgs
    {
        public string Name { get; }
        public IDictionary<string, object?> Data { get; }
        public EventHandlerArgs(string name, IDictionary<string, object?> data)
        {
            Name = name;
            Data = data;
        }
    }

    class EventHandler
    {
        public static event EventHandler<EventHandlerArgs>? OnEvent;

        private static void InvokeEvent(string name, dynamic? data)
        {
            OnEvent?.Invoke(null, new EventHandlerArgs(name, data));
        }

        private static readonly Dictionary<string, Action<string[], string[]>> eventHandlers = new();

        public static void RegisterHandler(string eventName, Action<string[], string[]> handler)
        {
            eventHandlers[eventName] = handler;
        }

        public static void ProcessEvent(LogReaderEventArgs e)
        {
            string eventName = e.Data[0];
            if (eventHandlers.TryGetValue(eventName, out var handler))
            {
                handler(e.Data[1..], e.Data);
            }
        }

        static EventHandler()
        {

            //var ENCOUNTER_START = new Dictionary<string, Type>
            //{
            //    { "encounterID", typeof(int) },
            //    { "encounterName", typeof(string) },
            //    { "difficultyID", typeof(int) },
            //    { "groupSize", typeof(int) },
            //    { "instanceID", typeof(int) }
            //};

            //RegisterHandler("ENCOUNTER_START", (data, payload) => { InvokeEvent(payload[0], EventDataHandler.Unpack(data, ENCOUNTER_START)); });

            var ENCOUNTER_END = new Dictionary<string, Type>
            {
                { "encounterID", typeof(int) },
                { "encounterName", typeof(string) },
                { "difficultyID", typeof(int) },
                { "groupSize", typeof(int) },
                { "success", typeof(int) },
                { "fightTime", typeof(double) }
            };

            RegisterHandler("ENCOUNTER_END", (data, payload) => { InvokeEvent(payload[0], EventDataHandler.Unpack(data, ENCOUNTER_END)); });

        }
    }
}
