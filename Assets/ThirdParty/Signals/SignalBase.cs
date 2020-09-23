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
	/// <summary>
	/// Base class for Signals
	/// </summary>
	public abstract class SignalBase
	{
        private protected const string LogLineColor = "blue";//"#9fbfc9"; - not visible in light skin of Unity Editor

		/// <summary>Write signals to Console</summary>
		public static bool LogSignals { get; set; } = true;

		/// <summary>Stop any signals</summary>
		public static bool SuppressSignals { get; set; }

		/// <summary>Name of the Signal</summary>
		public string Name { get; set; }

        /// <summary>Do not show the signal in log</summary>
        public bool HideInLog { get; set; }

        protected readonly LinkedList<ISubscriberInfoInternal> subscribers = new LinkedList<ISubscriberInfoInternal>();
		protected bool isFired;

		public Type GetTypeExplicit() => GetType();

		protected virtual void Publish()
		{
			if (!CanPublish)
				return;

			isFired = true;

			//log
			if (LogSignals && !HideInLog)
				Debug.Log($"[Fired] {ToLogString()}\r\n<color=grey>({subscribers.Count} subscribers)</color>");

			//enumerate subscribers
			var node = subscribers.First;
			while (node != null)
			{
				var next = node.Next;

				if (TryRemoveDestroyedSubscribers(next, ref node))
					continue;

				if (node.Value.IsSubscribedComponent 
                        && node.Value.CallOnlyWhenActive 
                        && !node.Value.IsActive)
				{
					node = next;
					continue;
				}

				InvokeCallback(node);

				node = next;
			}

			isFired = false;
		}

        protected bool CanPublish => !SuppressSignals && !isFired;

		protected abstract bool TryInvoke(ISubscriberInfoInternal unit);

		/// <summary>Is the Component already subscribed on this Signal?</summary>
		public bool IsComponentSubscribed(Component component)
		{
			var node = subscribers.First;
			while (node != null)
			{
				if (node.Value.IsSubscribedComponent && node.Value.SubscribedComponent == component)
					return true;

				node = node.Next;
			}

			return false;
		}

		protected bool Unsubscribe(Delegate callback)
		{
			var res = false;
			var node = subscribers.First;
			while (node != null)
			{
				if (node.Value.Action != callback)
				{
					node = node.Next;
					continue;
				}

				res = true;
				var next = node.Next;
				subscribers.Remove(node);
				node.Value.OnUnsubscribed();
				node = next;
			}

			return res;
		}

		/// <summary>
		/// Unsubscribe callback of binded Component
		/// </summary>
		public bool Unsubscribe(Component component)
		{
			var res = false;
			var node = subscribers.First;

			while (node != null)
			{
				if (!node.Value.IsSubscribedComponent || node.Value.SubscribedComponent != component)
				{
					node = node.Next;
					continue;
				}

				res = true;
				var next = node.Next;
				subscribers.Remove(node);
				node.Value.OnUnsubscribed();
				node = next;
			}

			return res;
		}

		/// <summary>
		/// Unsubscribe all callbacks
		/// </summary>
		public void UnsubscribeAll()
		{
			var node = subscribers.First;
			while (node != null)
			{
				node.Value.OnUnsubscribed();
				node = node.Next;
			}

			subscribers.Clear();
		}

		public override string ToString()
		{
			return $"{Name ?? base.ToString()}";
		}

		protected virtual string ToLogString()
		{
			return $"<color={LogLineColor}>{Name ?? base.ToString()}</color>";
		}

		private void InvokeCallback(LinkedListNode<ISubscriberInfoInternal> node1)
		{
			try
			{
				if (TryInvoke(node1.Value))
					TryRemoveInvokeOnceSignal(node1);
			}
			catch (Exception ex)
			{
                Debug.LogException(ex);
				TryRemoveInvokeOnceSignal(node1);
			}
		}

		private void TryRemoveInvokeOnceSignal(LinkedListNode<ISubscriberInfoInternal> linkedListNode)
		{
			if (!linkedListNode.Value.InvokeOnce)
				return;

			subscribers.Remove(linkedListNode);
			linkedListNode.Value.OnUnsubscribed();
		}

		private bool TryRemoveDestroyedSubscribers(LinkedListNode<ISubscriberInfoInternal> next, ref LinkedListNode<ISubscriberInfoInternal> linkedListNode1)
		{
			if (!linkedListNode1.Value.IsDestroyed)
				return false;

			subscribers.Remove(linkedListNode1);
			linkedListNode1 = next;
			return true;
		}
	}

	/// <summary>
	/// Info about subscriber
	/// </summary>
	public interface ISubscriberInfoInternal
	{
		bool IsDestroyed { get; }
		bool IsActive { get; }
		bool CallOnlyWhenActive { get; }
		bool InvokeOnce { get; }
		Delegate Action { get; }
		Delegate Condition { get; }
		Component SubscribedComponent { get; }
		bool IsSubscribedComponent { get; }
		void OnUnsubscribed();
	}

	/// <summary>
	/// The signal can pass arguments as serilized byte array.
	/// This interface helps to use signals with unknown types of arguments - for example in network services.
	/// </summary>
	public interface ISerializedSignal
	{
		void SubscribeSerialized(Action<byte[]> callback);
		void PublishSerialized(byte[] data);
	}

	/// <summary>
	/// Stores callback and other subscriber's info
	/// </summary>
	public abstract class SubscriberInfoBase<T> : ISubscriberInfoInternal where T : SubscriberInfoBase<T>
	{
		protected Delegate action;
		protected Delegate condition;

		private Component subscribedComponent;
		private bool isSubscribedComponent;
		private bool callOnlyWhenActive = true;
		private bool invokeOnce;
		private event Action onUnsubscribed;
		private SignalBase parentSignal;

		protected SubscriberInfoBase(Delegate action, SignalBase parentSignal)
		{
			this.parentSignal = parentSignal;
			this.action = action;
		}

		/// <summary>When the Component will be destroyed, subscriber will be auto unsubscribed.</summary>
		public T JoinWith(Component component)
		{
			subscribedComponent = component;
			isSubscribedComponent = true;

			return (T)this;
		}

        /// <summary>Call subscriber when the Component is inactive</summary>
        public T CallWhenInactive(bool callIfInactive = true)
        {
            if (!isSubscribedComponent)
                throw new Exception($"You can not call {nameof(CallWhenInactive)} if component is not joined");

            this.callOnlyWhenActive = !callIfInactive;

            return (T)this;
        }

        /// <summary>Execute the Callback only once. After this subscriber will be auto unsubscribed.</summary>
        public T InvokeOnce()
		{
			invokeOnce = true;
			return (T)this;
		}

		/// <summary>Execute the Callback after Unsubscribe</summary>
		public T OnUnsubscribed(Action callback)
		{
			onUnsubscribed += callback;
			return (T)this;
		}

		protected T Condition(Delegate condition)
		{
			if (this.condition != null)
				throw new Exception("You can not assign more then one condition!");

			this.condition = condition;
			return (T)this;
		}

		#region ISubscriberInfoInternal

		Delegate ISubscriberInfoInternal.Action => action;
		Delegate ISubscriberInfoInternal.Condition => condition;
		bool ISubscriberInfoInternal.IsDestroyed => isSubscribedComponent && (!subscribedComponent || !subscribedComponent.gameObject);
		bool ISubscriberInfoInternal.IsActive => isSubscribedComponent && subscribedComponent.gameObject.activeInHierarchy && ((subscribedComponent is MonoBehaviour));
		bool ISubscriberInfoInternal.CallOnlyWhenActive => callOnlyWhenActive;
		bool ISubscriberInfoInternal.InvokeOnce => invokeOnce;
		bool ISubscriberInfoInternal.IsSubscribedComponent => isSubscribedComponent;
		Component ISubscriberInfoInternal.SubscribedComponent => subscribedComponent;

		void ISubscriberInfoInternal.OnUnsubscribed()
		{
			try
			{
				onUnsubscribed?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			};
		}

		#endregion
	}

	/// <summary>Binding direction</summary>
	public enum BindDirection
	{
		/// <summary>Component receives data and sends data to Bus</summary>
		TwoWay = 0,
		/// <summary>Component only receives data from Bus</summary>
		OnlySubscribe = 1,
		/// <summary>Component only sends data to Bus</summary>
		OnlyPublish = 2
	}
}