using System;
using System.Linq;

//--------------------
//  EXAMPLE OF USAGE
//--------------------
//
//  class Bus
//  {
//      public readonly static Signal OnMouseClick;
//      public readonly static Signal<string> ShowNotificationRequest;
//      public readonly static State<int> Gold;
//  
//      static Bus() => BusHelper.InitFields<Bus>();
//  }

namespace Signals
{
    public class BusHelper
    {
        /// <summary>
        /// Auto create fields inherited from SignalBase
        /// </summary>
        public static void InitFields<T>()
        {
	        typeof(T)
		        .GetFields()
		        .Where(fi =>
			        typeof(SignalBase).IsAssignableFrom(fi.FieldType)
			        && fi.GetValue(null) == null)
                .ToList()
		        .ForEach(fi =>
		        {
			        var signal = (SignalBase) Activator.CreateInstance(fi.FieldType);
			        signal.Name = $"{typeof(T).Name}.{fi.Name}";
                    signal.HideInLog = fi.GetCustomAttributes(typeof(HideInLogAttribute), false).Any();
					fi.SetValue(null, signal);
		        });
        }
    }
}