using System;
using System.Globalization;

namespace Signals
{
    [Serializable]
    public class BusSignal
    {
        public string SignalName;
        public BusSignalArgType ArgType;
        public string Argument;

        public void Publish()
        {
            switch (ArgType)
            {
                case BusSignalArgType.None:
                    ScopeBus.Signal(SignalName).Publish();
                    return;

                case BusSignalArgType.String:
                {
                    ScopeBus.Signal<string>(SignalName).Publish(Argument);
                    return;
                }

                case BusSignalArgType.Int:
                {
                    if (int.TryParse(Argument, out var res))
                        ScopeBus.Signal<int>(SignalName).Publish(res);
                    return;
                }

                case BusSignalArgType.Float:
                {
                    if (float.TryParse(Argument, NumberStyles.Float, CultureInfo.InvariantCulture, out var res))
                        ScopeBus.Signal<float>(SignalName).Publish(res);
                    return;
                }
            }
        }
    }

    public enum BusSignalArgType
    {
        None, String, Int, Float
    }
}