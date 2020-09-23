using System;
using System.Collections.Generic;

//--------------------
//  EXAMPLE OF USAGE
//--------------------
//
// Scope.Signal<int>("MySignal").Subscribe(...);
// Scope.Signal<int>("MySignal").Publish(...);
//

namespace Signals
{
	/// <summary>
	/// Lightweight Bus with String as key of signal.
	/// </summary>
	public static class ScopeBus
	{
		private static readonly Dictionary<ScopeKey, SignalBase> keyToSignal = new Dictionary<ScopeKey, SignalBase>();

		/// <summary>Get Signal by Key</summary>
		public static Signal Signal(string scopeKey)
		{
			var key = new ScopeKey(typeof(Signal), scopeKey);
			return GetOrCreateSignal<Signal>(key);
		}

		/// <summary>Get Signal by Key</summary>
		public static Signal<T> Signal<T>(string scopeKey)
		{
			var key = new ScopeKey(typeof(Signal<T>), scopeKey);
			return GetOrCreateSignal<Signal<T>>(key);
		}

		/// <summary>Get Signal by Key</summary>
		public static Signal<T1, T2> Signal<T1, T2>(string scopeKey)
		{
			var key = new ScopeKey(typeof(Signal<T1, T2>), scopeKey);
			return GetOrCreateSignal<Signal<T1, T2>>(key);
		}

		/// <summary>Get State by Key</summary>
		public static State<T> State<T>(string scopeKey)
		{
			var key = new ScopeKey(typeof(State<T>), scopeKey);
			return GetOrCreateSignal<State<T>>(key);
		}

        /// <summary>Get State by Key</summary>
        public static State<T, TK> State<T, TK>(string scopeKey)
        {
            var key = new ScopeKey(typeof(State<T, TK>), scopeKey);
            return GetOrCreateSignal<State<T, TK>>(key);
        }

        /// <summary>
        /// Unsubscribe all callbacks of all Signals and clear cache
        /// </summary>
        public static void UnsubscribeAndRemoveAll()
		{
			foreach (var e in keyToSignal.Values)
				e.UnsubscribeAll();

			keyToSignal.Clear();
		}

		/// <summary>
		/// Unsubscribe all callbacks and remove Signal from cache
		/// </summary>
		public static bool UnsubscribeAndRemoveSignal(string scopeKey)
		{
			var key = new ScopeKey(typeof(Signal), scopeKey);
			return UnsubscribeAndRemove(key);
		}

		/// <summary>
		/// Unsubscribe all callbacks and remove Signal from cache
		/// </summary>
		public static bool UnsubscribeAndRemoveSignal<T>(string scopeKey)
		{
			var key = new ScopeKey(typeof(Signal<T>), scopeKey);
			return UnsubscribeAndRemove(key);
		}

		/// <summary>
		/// Unsubscribe all callbacks and remove Signal from cache
		/// </summary>
		public static bool UnsubscribeAndRemoveSignal<T1, T2>(string scopeKey)
		{
			var key = new ScopeKey(typeof(Signal<T1, T2>), scopeKey);
			return UnsubscribeAndRemove(key);
		}

		/// <summary>
		/// Unsubscribe all callbacks and remove State from cache
		/// </summary>
		public static bool UnsubscribeAndRemoveState<T>(string scopeKey)
		{
			var key = new ScopeKey(typeof(State<T>), scopeKey);
			return UnsubscribeAndRemove(key);
		}

		#region Private

		private static T GetOrCreateSignal<T>(ScopeKey key) where T : SignalBase, new()
		{
			if (!keyToSignal.TryGetValue(key, out var e))
				keyToSignal[key] = e = new T { Name = key.Key };

			return (T)e;
		}

		private static bool UnsubscribeAndRemove(ScopeKey key)
		{
			if (!keyToSignal.TryGetValue(key, out var e))
				return false;

			e.UnsubscribeAll();
			keyToSignal.Remove(key);
			return true;
		}

		struct ScopeKey
		{
			private Type SignalType;
			public string Key;

			public ScopeKey(Type signalType, string key)
			{
				SignalType = signalType;
				Key = key ?? throw new Exception("ScopeKey can not be null");
			}

			public override bool Equals(object obj)
			{
				if (!(obj is ScopeKey))
				{
					return false;
				}

				var key = (ScopeKey)obj;
				return EqualityComparer<Type>.Default.Equals(SignalType, key.SignalType) &&
					   Key == key.Key;
			}

			public override int GetHashCode()
			{
				var hashCode = -614245237;
				hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(SignalType);
				hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Key);
				return hashCode;
			}
		}

		#endregion
	}
}