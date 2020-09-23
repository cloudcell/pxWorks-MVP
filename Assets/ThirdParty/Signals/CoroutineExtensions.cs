using System;
using System.Collections;
using UnityEngine;

//--------------------
//  EXAMPLE OF USAGE
//--------------------
//
// IEnumerator MyCoroutine()
// {
//     yield return Bus.MyState.Wait();
//     Debug.Log("MyState published " + Bus.MyState.Value);
//
//     var waiter = Bus.MySignal.GetWaiter();
//     yield return waiter;
//     Debug.Log("MySignal published " + waiter.Data);
// }

namespace Signals
{
    public interface IWaiter : IEnumerator
    {
        bool Invoked { get; }
    }

    /// <summary>Helps to join signals and coroutines</summary>
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Use this method to wait of Signal in coroutine.
        /// Usage: yield return MySignal.Wait();
        /// </summary>
        public static IEnumerator Wait(this Signal e, Func<bool> condition = null)
        {
            var invoked = false;
            e.SubscribeRaw(() => invoked = true)
                .Condition(() => condition == null || condition())
                .InvokeOnce();
            while (!invoked)
                yield return null;
        }

        /// <summary>
        /// Use this method to wait of State in coroutine.
        /// Usage: yield return MyState.Wait();
        /// </summary>
        public static IEnumerator Wait<T>(this State<T> e, Func<T, bool> condition = null)
        {
            var invoked = false;
            e.SubscribeRaw((data) => invoked = true)
                .Condition((data) => condition == null || condition(data))
                .InvokeOnce();
            while (!invoked)
                yield return null;
        }

        /// <summary>
        /// Use this method to wait of Signal<T> in coroutine.
        /// Usage: 
        /// var waiter = MySignal.GetWaiter();
        /// yield waiter;
        /// </summary>
        public static Waiter GetWaiter(this Signal e, Func<bool> condition = null)
        {
            return new Waiter(e, condition);
        }

        /// <summary>
        /// Use this method to wait of Signal<T> in coroutine.
        /// Usage: 
        /// var waiter = MySignal.GetWaiter();
        /// yield waiter;
        /// var data = waiter.Data;
        /// </summary>
        public static Waiter<T> GetWaiter<T>(this Signal<T> e, Func<T, bool> condition = null)
        {
            return new Waiter<T>(e, condition);
        }

        /// <summary>
        /// Use this method to wait of Signal<T1, T2> in coroutine.
        /// Usage: 
        /// var waiter = MySignal.GetWaiter();
        /// yield waiter;
        /// var data1 = waiter.Data1;
        /// </summary>
        public static Waiter<T1, T2> GetWaiter<T1, T2>(this Signal<T1, T2> e, Func<T1, T2, bool> condition = null)
        {
            return new Waiter<T1, T2>(e, condition);
        }
    }

    /// <summary>
    /// Use this class to wait Signal in coroutines.
    /// Implements IEnumerator interface.
    /// </summary>
    public class Waiter : IWaiter
    {
        public bool Invoked { get; private set; }

        public Waiter(Signal e, Func<bool> condition = null)
        {
            e.SubscribeRaw(() =>
            {
                Invoked = true;
            })
            .Condition(() => condition == null || condition())
            .InvokeOnce();
        }

        object IEnumerator.Current => null;

        bool IEnumerator.MoveNext()
        {
            return !Invoked;
        }

        void IEnumerator.Reset()
        {
        }
    }

    /// <summary>
    /// Use this class to wait Signal in coroutines.
    /// Implements IEnumerator interface.
    /// </summary>
    public class Waiter<T> : IWaiter
    {
        public T Data { get; private set; } = default;
        public bool Invoked { get; private set; }

        public Waiter(Signal<T> e, Func<T, bool> condition = null)
        {
            e.SubscribeRaw((data) =>
            {
                Invoked = true;
                Data = data;
            })
            .Condition((data) => condition == null || condition(data))
            .InvokeOnce();
        }

        object IEnumerator.Current => null;

        bool IEnumerator.MoveNext()
        {
            return !Invoked;
        }

        void IEnumerator.Reset()
        {
        }
    }

    /// <summary>
    /// Use this class to wait Signal in coroutines.
    /// Implements IEnumerator interface.
    /// </summary>
    public class Waiter<T1, T2> : IWaiter
    {
        public T1 Data1 { get; private set; } = default;
        public T2 Data2 { get; private set; } = default;
        public bool Invoked { get; private set; }

        public Waiter(Signal<T1, T2> e, Func<T1, T2, bool> condition = null)
        {
            e.SubscribeRaw((data1, data2) =>
            {
                Invoked = true;
                Data1 = data1;
                Data2 = data2;
            })
            .Condition((data1, data2) => condition == null || condition(data1, data2))
            .InvokeOnce();
        }

        object IEnumerator.Current => null;

        bool IEnumerator.MoveNext()
        {
            return !Invoked;
        }

        void IEnumerator.Reset()
        {
        }
    }
}