using System;
using System.Collections.Generic;
using UnityEngine;

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
	internal interface IState
	{
	}

	public class State<T, TK> : Signal<T, TK>, IState, IEquatable<State<T, TK>>
	{
		public (T, TK) Value
		{
			get => (Data1, Data2);
			set => Publish(value.Item1, value.Item2);
		}

		/// <summary>
		/// Returns previous Value, before changing.
		/// This property have value only inside subscriber's callbacks.
		/// </summary>
		public (T, TK) PrevValue { get; protected set; }

		/// <summary>
		/// Assign value and do not call Publish.
		/// </summary>
		public void Assign(T data1, TK data2)
		{
			Data1 = data1;
			Data2 = data2;
		}

		public override void Publish(T data1, TK data2)
		{
			if (!CanPublish)
				return;

			PrevValue = (Data1, Data2);

			Assign(data1, data2);
			Publish();

			PrevValue = default;
		}

        public override SubscriberInfo BindInternal(Component comp, BindDirection way, Action<T, TK> callback)
        {
            if (way != BindDirection.OnlyPublish)
            try
            {
                callback(Data1, Data2);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }            

            return base.BindInternal(comp, way, callback);
        }

        /// <summary>
        /// Converts <see cref="State{T,TK}"/> to T implicitly
        /// </summary>
        public static implicit operator T(State<T, TK> state)
		{
			return state.Data1;
		}

		/// <summary>
		/// Converts <see cref="State{T,TK}"/> to TK implicitly
		/// </summary>
		public static implicit operator TK(State<T, TK> state)
		{
			return state.Data2;
		}

		/// <summary>
		/// Converts <see cref="State{T,TK}"/> to (T,TK) implicitly
		/// </summary>
		public static implicit operator (T, TK)(State<T, TK> state)
		{
			return (state.Data1, state.Data2);
		}

		public static State<T, TK> operator +(State<T, TK> state, (T, TK) data)
		{
			state.Value = data;
			return state;
		}

		public static bool operator ==(State<T, TK> state, State<T, TK> otherState)
		{
			if (state == null)
			{
                Debug.LogError($"{nameof(state)} is NULL, comparing via \"==\" with {typeof(T)}:[{otherState}]");
				return false;
			}

			return state.Equals(otherState);
		}
		
		public static bool operator ==(State<T, TK> state, T val)
		{
			return state != null && Equals(state.Data1, val);
		}

		public static bool operator ==(State<T, TK> state, TK val)
		{
			return state != null && Equals(state.Data2, val);
		}

		public static bool operator !=(State<T, TK> state, T val)
		{
			return state != null && !Equals(state.Data1, val);
		}

		public static bool operator !=(State<T, TK> state, TK val)
		{
			return state != null && !Equals(state.Data2, val);
		}

		public static bool operator !=(State<T, TK> state, State<T, TK> otherState)
		{
			if (!(state == null))
				return !state.Equals(otherState);

            Debug.LogError($"{nameof(state)} is NULL, comparing via \"!=\" with {typeof(T)}:[{otherState}]");
			return false;
		}

		public bool Equals(State<T, TK> other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((State<T, TK>)obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}

	/// <summary>
	/// Stores Value and calls Publish when value was assigned.
	/// Implements Publisher-Subscriber pattern.
	/// </summary>
	public class State<T> : Signal<T>, IState
	{
		/// <summary>Default constructor</summary>
		public State()
		{
		}

		/// <summary>
		/// Creates the State<T> with value, w/o calling of Publish
		/// </summary>
		public State(T data)
		{
			Data = data;
		}

		/// <summary>Assign Value and Publish it</summary>
		public override void Publish(T data)
		{
			if (!CanPublish)
				return;

			PrevValue = Data;

			Data = data;
			Publish();

			PrevValue = default;
		}

		/// <summary>Assign Value and Publish it, if value was changed</summary>
		public void PublishIfChanged(T data)
		{
			if (!Equals(Data, data))
				Publish(data);
		}

		public override SubscriberInfo BindInternal(Component comp, BindDirection way, Action<T> callback)
		{
			if (way != BindDirection.OnlyPublish)
            try
            {
                callback(Data);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return base.BindInternal(comp, way, callback);
		}

		/// <summary>
		/// Returns Value of State.
		/// Assigns value and calls Publish().
		/// </summary>
		public T Value
		{
			get => Data;
			set => Publish(value);
		}

		/// <summary>
		/// Returns previous Value, before changing.
		/// This property have value only inside subscriber's callbacks.
		/// </summary>
		public T PrevValue { get; protected set; }

		/// <summary>
		/// Assign value and do not call Publish.
		/// </summary>
		public void Assign(T value)
		{
			Data = value;
		}

		/// <summary>
		/// Converts State<T> to T implicitly
		/// </summary>
		public static implicit operator T(State<T> state)
		{
			return state.Data;
		}

		/// <summary>
		/// Allows to assign value to State<T> by following way: MyState += value;
		/// </summary>
		public static State<T> operator +(State<T> state, T val)
		{
			state.Value = val;
			return state;
		}

		public static bool operator ==(State<T> state, T val)
		{
			if (state != null)
				return Equals(state.Data, val);

            Debug.LogWarning($"{nameof(state)} is NULL, comparing via \"==\" with {typeof(T)}:[{val}]");
			return Equals(null, val);
		}

		public static bool operator !=(State<T> state, T val)
		{
			if (state != null)
				return !Equals(state.Data, val);

            Debug.LogWarning($"{nameof(state)} is NULL, comparing via \"==\" with {typeof(T)}:[{val}]");
			return Equals(null, val);
		}

		protected bool Equals(State<T> other)
		{
			return EqualityComparer<T>.Default.Equals(Value, other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((State<T>)obj);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T>.Default.GetHashCode(Value);
		}

		public override string ToString()
		{
			var name = Name ?? $"State<{typeof(T).Name}>";
			return $"{name}: {Data}";
		}

		protected override string ToLogString()
		{
			var name = Name ?? $"State<{typeof(T).Name}>";
			return $"<color={LogLineColor}>{name}</color>: {Data}";
		}
	}
}