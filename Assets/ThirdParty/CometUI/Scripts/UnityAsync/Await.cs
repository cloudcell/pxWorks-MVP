using System;
using UnityEngine;

namespace UI
{
	// convenience methods for awaiting without access to an AsyncBehaviour
	public static class Await
	{
		public static AsyncBehaviour.UnityAwaiter NextUpdate() => UnityAsyncManager.behaviour.NextUpdate();
		public static AsyncBehaviour.UnityAwaiter NextLateUpdate() => UnityAsyncManager.behaviour.NextLateUpdate();
		public static AsyncBehaviour.UnityAwaiter NextFixedUpdate() => UnityAsyncManager.behaviour.NextFixedUpdate();
		public static AsyncBehaviour.UnityAwaiter Updates(int framesToWait) => UnityAsyncManager.behaviour.Updates(framesToWait);
		public static AsyncBehaviour.UnityAwaiter LateUpdates(int framesToWait) => UnityAsyncManager.behaviour.LateUpdates(framesToWait);
		public static AsyncBehaviour.UnityAwaiter FixedUpdates(int stepsToWait) => UnityAsyncManager.behaviour.FixedUpdates(stepsToWait);
		public static AsyncBehaviour.UnityAwaiter Seconds(float secondsToWait) => UnityAsyncManager.behaviour.Seconds(secondsToWait);
		public static AsyncBehaviour.UnityAwaiter SecondsUnscaled(float secondsToWait) => UnityAsyncManager.behaviour.SecondsUnscaled(secondsToWait);
		public static AsyncBehaviour.UnityAwaiter Until(Func<bool> condition) => UnityAsyncManager.behaviour.Until(condition);
		public static AsyncBehaviour.UnityAwaiter While(Func<bool> condition) => UnityAsyncManager.behaviour.While(condition);
		public static AsyncBehaviour.UnityAwaiter AsyncOp(AsyncOperation op) => UnityAsyncManager.behaviour.AsyncOp(op);
		public static AsyncBehaviour.UnityAwaiter Custom(CustomYieldInstruction instruction) => UnityAsyncManager.behaviour.Custom(instruction);
	}

	static class AwaitExtensions
	{
		public static AsyncBehaviour.UnityAwaiter GetAwaiter(this AsyncOperation @this) => Await.AsyncOp(@this);
		public static AsyncBehaviour.UnityAwaiter GetAwaiter(this CustomYieldInstruction @this) => Await.Custom(@this);
	}
}