using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	// manages all AsyncBehaviours including low overhead update calls
	public class UnityAsyncManager : MonoBehaviour
	{
		public static int frameCount;
		public static uint fixedStepCount;
		public static float time;
		public static float unscaledTime;

		public static AsyncBehaviour behaviour { get; private set; }

		static List<IAsyncBehaviour> updates;
		static List<IAsyncBehaviour> lateUpdates;
		static List<IAsyncBehaviour> fixedUpdates;

		[RuntimeInitializeOnLoadMethod]
		static void Initialize()
		{
			var anchor = new GameObject("UnityAsync Manager").AddComponent<UnityAsyncManager>();
			anchor.gameObject.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(anchor.gameObject);

			frameCount = 1;
			fixedStepCount = 1;
			time = Time.time;
			unscaledTime = Time.unscaledTime;

			updates = new List<IAsyncBehaviour>(128);
			lateUpdates = new List<IAsyncBehaviour>(32);
			fixedUpdates = new List<IAsyncBehaviour>(32);

			behaviour = anchor.gameObject.AddComponent<AsyncBehaviour>();
		}

		public static void RegisterUpdate(IAsyncBehaviour b)
		{
			b.updateIndex = updates.Count;
			updates.Add(b);
		}

		public static void RegisterLateUpdate(IAsyncBehaviour b)
		{
			b.lateUpdateIndex = lateUpdates.Count;
			lateUpdates.Add(b);
		}

		public static void RegisterFixedUpdate(IAsyncBehaviour b)
		{
			b.fixedUpdateIndex = fixedUpdates.Count;
			fixedUpdates.Add(b);
		}

		public static void UnregisterUpdate(IAsyncBehaviour b)
		{
			int count = updates.Count;
			int i = b.updateIndex;

			if(count > 1)
			{
				var toSwap = updates[count - 1];
				updates[i] = toSwap;
				toSwap.updateIndex = i;
				updates.RemoveAt(count - 1);
			}
			else
			{
				updates.RemoveAt(i);
			}
		}

		public static void UnregisterLateUpdate(IAsyncBehaviour b)
		{
			int count = fixedUpdates.Count;
			int i = b.lateUpdateIndex;

			if(count > 1)
			{
				var toSwap = lateUpdates[count - 1];
				lateUpdates[i] = toSwap;
				toSwap.lateUpdateIndex = i;
				lateUpdates.RemoveAt(count - 1);
			}
			else
			{
				lateUpdates.RemoveAt(i);
			}
		}

		public static void UnregisterFixedUpdate(IAsyncBehaviour b)
		{
			int count = fixedUpdates.Count;
			int i = b.fixedUpdateIndex;

			if(count > 1)
			{
				var toSwap = fixedUpdates[count - 1];
				fixedUpdates[i] = toSwap;
				toSwap.fixedUpdateIndex = i;
				fixedUpdates.RemoveAt(count - 1);
			}
			else
			{
				fixedUpdates.RemoveAt(i);
			}
		}

		void Update()
		{
			// = Time.frameCount;
			time = Time.time;
			unscaledTime = Time.unscaledTime;

			for(int i = 0; i < updates.Count; ++i)
			{
				if(!updates[i].Update())
				{
					int count = updates.Count;

					if(count > 1)
					{
						var toSwap = updates[count - 1];
						updates[i] = toSwap;
						toSwap.updateIndex = i;
						updates.RemoveAt(count - 1);
						--i;
					}
					else
					{
						updates.RemoveAt(i);
					}
				}
			}

			++frameCount;
		}

		void LateUpdate()
		{
			for(int i = 0; i < lateUpdates.Count; ++i)
			{
				if(!lateUpdates[i].LateUpdate())
				{
					int count = lateUpdates.Count;

					if(count > 1)
					{
						var toSwap = lateUpdates[count - 1];
						lateUpdates[i] = toSwap;
						toSwap.lateUpdateIndex = i;
						lateUpdates.RemoveAt(count - 1);
						--i;
					}
					else
					{
						lateUpdates.RemoveAt(i);
					}
				}
			}
		}

		void FixedUpdate()
		{
			for(int i = 0; i < fixedUpdates.Count; ++i)
			{
				if(!fixedUpdates[i].FixedUpdate())
				{
					int count = fixedUpdates.Count;

					if(count > 1)
					{
						var toSwap = fixedUpdates[count - 1];
						fixedUpdates[i] = toSwap;
						toSwap.fixedUpdateIndex = i;
						fixedUpdates.RemoveAt(count - 1);
						--i;
					}
					else
					{
						fixedUpdates.RemoveAt(i);
					}
				}
			}

			++fixedStepCount;
		}
	}
}