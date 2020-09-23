using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Signals
{
	/// <summary>
	/// Network service should implement this interface to enable networked Signals.
	/// </summary>
	public interface INetworkBusService
	{
		void RaiseSignal(int signalId, byte[] data);
		event Action<int, byte[]> OnSignalCallback;
	}

	/// <summary>
	/// Enables sending Signals via network.
	/// Needed signals must be marked as [NetworkedSignal].
	/// [NetworkedSignal] Signal will be called for all network players.
	/// 
	/// This class provides just bridge between Bus and network service. 
	/// To make it working you need create network service and implement INetworkBusService there.
	/// Then call method Init<Bus>(service) to enable bridge.
	/// </summary>
	public class BusToNetworkBridge
	{
		static readonly List<SignalInfo> signalIdToSignalInfo = new List<SignalInfo>();
		static readonly HashSet<INetworkBusService> services = new HashSet<INetworkBusService>();

		/// <summary>
		/// Call this method to add Bus to networking service.
		/// After this signal marked as [NetworkedSignal] will be shared via network.
		/// </summary>
		public static void Init<TBus>(INetworkBusService service)
		{
			if (!services.Contains(service))
			{
				service.OnSignalCallback += Service_OnSignalCallback;
				services.Add(service);
			}

			var rrSignalBaseType = typeof(SignalBase);
			var networkedSignalAttributeType = typeof(NetworkedSignalAttribute);
			var iSerializedSignalType = typeof(ISerializedSignal);

            //get all fields of type Signal
            typeof(TBus)
                .GetFields()
                .Where(fi =>
                    rrSignalBaseType.IsAssignableFrom(fi.FieldType)
                    && iSerializedSignalType.IsAssignableFrom(fi.FieldType)
                    && fi.GetCustomAttributes(networkedSignalAttributeType, true).Any())
                .ToList()
				.ForEach(fi =>
				{
					var e = (ISerializedSignal)fi.GetValue(null);
					Subscribe(e, service);
				});
		}

		private static void Service_OnSignalCallback(int signalId, byte[] bytes)
		{
			if (signalId < 0 || signalId >= signalIdToSignalInfo.Count)
				return;//is not my signalId

			var info = signalIdToSignalInfo[signalId];
			info.IsFired = true;

            if (SignalBase.LogSignals)
            {
                if (info.Signal is SignalBase e && !e.HideInLog)
                    Debug.LogError($"[Received from Network] <color=blue>{e.Name}</color>");
            }

			try
			{
				info.Signal.PublishSerialized(bytes);
			}
			finally
			{
				info.IsFired = false;
			}
		}

		private static void Subscribe(ISerializedSignal eBase, INetworkBusService service)
		{
			var signalId = signalIdToSignalInfo.Count();
			var info = new SignalInfo(eBase);

			eBase.SubscribeSerialized((bytes) =>
			{
				if (!info.IsFired)
					service.RaiseSignal(signalId, bytes);
			});

			signalIdToSignalInfo.Add(info);
		}

		private class SignalInfo
		{
			public ISerializedSignal Signal;
			public bool IsFired;

			public SignalInfo(ISerializedSignal signal)
			{
				Signal = signal;
			}
		}
	}
}