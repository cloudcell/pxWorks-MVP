// Easing2Curve: An editor window to create animation curve from easing functions:  
// http://diegogiacomelli.com.br/easing-2-curve-an-editor-window-to-create-animation-curve-from-easing-functions/

// Original easings from https://github.com/giacomelli/Doog

using System;
using System.Linq;
using UnityEngine;

namespace CometUI
{
    /// <summary>
    /// Define an interface to easing function.
    /// </summary>
    /// <remarks>
    /// http://easings.net
    /// </remarks>
    public interface IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <returns>The easing to the time.</returns>
        /// <param name="time">Time.</param>
        float Calculate(float time);
    }

    /// <summary>
    /// Availables easings.
    /// </summary>
    /// <remarks>
    /// All those easing are coded with help from:
    /// * https://gist.github.com/gre/1650294
    /// * http://easings.net
    /// * https://github.com/cinder/Cinder/blob/3fc0c0f8ae268fa0589e412d19c7372951cef447/include/cinder/Easing.h#L375
    /// * https://github.com/acron0/Easings/blob/master/Easings.cs
    /// Thank you guys!
    /// </remarks>
    public static class Easing
    {
        static Easing()
        {
            All = typeof(Easing)
                .Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && typeof(IEasing).IsAssignableFrom(t))
                .Select(t => Activator.CreateInstance(t) as IEasing)
                .OrderBy(t => t.GetType().Name)
                .ToArray();
        }

        /// <summary>
        /// Gets all eansings.
        /// </summary>
        /// <value>All.</value>
        public static IEasing[] All { get; private set; }

    }

    /// <summary>
    /// Easing constants.
    /// </summary>
    public static class EasingConstants
    {
        /// <summary>
        /// The half of PI used in some easings.
        /// </summary>
        public const float HalfPI = (float)Mathf.PI / 2.0f;
    }

    /// <summary>
    /// Easing extensions methods.
    /// </summary>
    public static class EeasingExtensions
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="easing">The easing</param>
        /// <param name="start">The start value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="time">The current time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public static float Calculate(this IEasing easing, float start, float target, float time)
        {
            return start + (target - start) * easing.Calculate(time);
        }
    }

    /// <summary>
    /// An InBack easing
    /// </summary>
    public class InBackEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time * time * time - time * (float)Mathf.Sin(time * Mathf.PI);
        }
    }

    /// <summary>
    /// An OutBack easing.
    /// </summary>
    public class OutBackEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            float f = (1 - time);
            return 1 - (f * f * f - f * (float)Mathf.Sin(f * Mathf.PI));
        }
    }

    /// <summary>
    /// An InOutBack easing.
    /// </summary>
    public class InOutBackEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            if (time < 0.5f)
            {
                float f = 2 * time;
                return 0.5f * (f * f * f - f * (float)Mathf.Sin(f * Mathf.PI));
            }
            else
            {
                float f = (1 - (2 * time - 1));
                return 0.5f * (1 - (f * f * f - f * (float)Mathf.Sin(f * Mathf.PI))) + 0.5f;
            }
        }
    }


    /// <summary>
    /// An InBounce easing.
    /// </summary>
    public class InBounceEasing : IEasing
    {
        internal static float GetBounce(float t)
        {
            if (t < 4 / 11.0f)
            {
                return (121 * t * t) / 16.0f;
            }
            else if (t < 8 / 11.0f)
            {
                return (363 / 40.0f * t * t) - (99 / 10.0f * t) + 17 / 5.0f;
            }
            else if (t < 9 / 10.0f)
            {
                return (4356 / 361.0f * t * t) - (35442 / 1805.0f * t) + 16061 / 1805.0f;
            }
            else
            {
                return (54 / 5.0f * t * t) - (513 / 25.0f * t) + 268 / 25.0f;
            }
        }

        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return 1 - GetBounce(1 - time);
        }
    }

    /// <summary>
    /// An OutBounce easing.
    /// </summary>
    public class OutBounceEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return InBounceEasing.GetBounce(time);
        }
    }

    /// <summary>
    /// An InOutBounce easing.
    /// </summary>
    public class InOutBounceEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            if (time < 0.5f)
            {
                return 0.5f * (1 - InBounceEasing.GetBounce(1 - time * 2));
            }
            else
            {
                return 0.5f * InBounceEasing.GetBounce(time * 2 - 1) + 0.5f;
            }
        }
    }

    /// <summary>
    /// An InCirc easing.
    /// </summary>
    public class InCircEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return 1 - (float)Mathf.Sqrt(1 - (time * time));
        }
    }

    /// <summary>
    /// An OutCirc easing.
    /// </summary>
    public class OutCircEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return (float)Mathf.Sqrt((2 - time) * time);
        }
    }

    /// <summary>
    /// An InOutCirc easing.
    /// </summary>
    public class InOutCircEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            if (time < 0.5f)
            {
                return 0.5f * (1 - (float)Mathf.Sqrt(1 - 4 * (time * time)));
            }
            else
            {
                return 0.5f * ((float)Mathf.Sqrt(-((2 * time) - 3) * ((2 * time) - 1)) + 1);
            }
        }
    }


    /// <summary>
    /// An InCubic easing: accelerating from zero velocity.
    /// </summary>
    public class InCubicEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time * time * time;
        }
    }

    /// <summary>
    /// An OutCubic easing: decelerating to zero velocity.
    /// </summary>
    public class OutCubicEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return (--time) * time * time + 1;
        }
    }

    /// <summary>
    /// An InCubic easing: acceleration until halfway, then deceleration.
    /// </summary>
    public class InOutCubicEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {

            return time < .5 ? 4 * time * time * time : (time - 1) * (2 * time - 2) * (2 * time - 2) + 1;
        }
    }

    /// <summary>
    /// An InElastic easing: accelerating from zero velocity.
    /// </summary>
    public class InElasticEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return (float)(Mathf.Sin(13 * EasingConstants.HalfPI * time) * Mathf.Pow(2, 10 * (time - 1)));
        }
    }

    /// <summary>
    /// An OutElastic easing: decelerating to zero velocity.
    /// </summary>
    public class OutElasticEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return (float)(Mathf.Sin(-13 * EasingConstants.HalfPI * (time + 1)) * Mathf.Pow(2, -10 * time) + 1);
        }
    }

    /// <summary>
    /// An InOutElastic easing.
    /// </summary>
    public class InOutElasticEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            if (time < 0.5f)
            {
                return (float)(0.5f * Mathf.Sin(13 * EasingConstants.HalfPI * (2 * time)) * Mathf.Pow(2, 10 * ((2 * time) - 1)));
            }
            else
            {
                return (float)(0.5f * (Mathf.Sin(-13 * EasingConstants.HalfPI * ((2 * time - 1) + 1)) * Mathf.Pow(2, -10 * (2 * time - 1)) + 2));
            }
        }
    }

    /// <summary>
    /// An InExpo easing.
    /// </summary>
    public class InExpoEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time == (0.0f) ? time : (float)Mathf.Pow(2, 10 * (time - 1));
        }
    }

    /// <summary>
    /// An OutExpo easing.
    /// </summary>
    public class OutExpoEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time == (1.0f) ? time : 1 - (float)Mathf.Pow(2, -10 * time);
        }
    }

    /// <summary>
    /// An InOutExpo easing.
    /// </summary>
    public class InOutExpoEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            if (time == (0.0f) || time == (1.0f))
            {
                return time;
            }

            if (time < 0.5f)
            {
                return 0.5f * (float)Mathf.Pow(2, (20 * time) - 10);
            }
            else
            {
                return -0.5f * (float)Mathf.Pow(2, (-20 * time) + 10) + 1;
            }
        }
    }

    /// <summary>
    /// A linear easing with no easing and no acceleration.
    /// </summary>
    public class LinearEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time;
        }
    }

    /// <summary>
    /// An InQuad easing: accelerating from zero velocity.
    /// </summary>
    public class InQuadEasing : IEasing
    {
        public float Calculate(float time)
        {
            return time * time;
        }
    }

    /// <summary>
    /// An OutQuad easing: decelerating to zero velocity.
    /// </summary>
    public class OutQuadEasing : IEasing
    {
        public float Calculate(float time)
        {
            return time * (2 - time);
        }
    }

    /// <summary>
    /// An InOutQuad easing: acceleration until halfway, then deceleration.
    /// </summary>
    public class InOutQuadEasing : IEasing
    {
        public float Calculate(float time)
        {
            return time < .5 ? 2 * time * time : -1 + (4 - 2 * time) * time;
        }
    }


    /// <summary>
    /// An InQuart easing: accelerating from zero velocity.
    /// </summary>
    public class InQuartEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time * time * time * time;
        }
    }

    /// <summary>
    /// An OutQuart easing: decelerating to zero velocity.
    /// </summary>
    public class OutQuartEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return 1 - (--time) * time * time * time;
        }
    }

    /// <summary>
    /// An InQuart easing: acceleration until halfway, then deceleration.
    /// </summary>
    public class InOutQuartEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {

            return time < .5 ? 8 * time * time * time * time : 1 - 8 * (--time) * time * time * time;
        }
    }

    /// <summary>
    /// An InQuint easing: accelerating from zero velocity.
    /// </summary>
    public class InQuintEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return time * time * time * time * time;
        }
    }

    /// <summary>
    /// An OutQuint easing: decelerating to zero velocity.
    /// </summary>
    public class OutQuintEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return 1 + (--time) * time * time * time * time;
        }
    }

    /// <summary>
    /// An InOutQuint easing: acceleration until halfway, then deceleration.
    /// </summary>
    public class InOutQuintEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {

            return time < .5 ? 16 * time * time * time * time * time : 1 + 16 * (--time) * time * time * time * time;
        }
    }



    /// <summary>
    /// An InSin easing: accelerating from zero velocity.
    /// </summary>
    public class InSinEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return (float)(1 + Mathf.Sin(Mathf.PI / 2 * time - Mathf.PI / 2));
        }
    }

    /// <summary>
    /// An OutSin easing: decelerating to zero velocity.
    /// </summary>
    public class OutSinEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {
            return (float)Mathf.Sin(Mathf.PI / 2 * time);
        }
    }

    /// <summary>
    /// An InSin easing: acceleration until halfway, then deceleration.
    /// </summary>
    public class InOutSinEasing : IEasing
    {
        /// <summary>
        /// Calculate the easing to the specified time.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>
        /// The easing to the time.
        /// </returns>
        public float Calculate(float time)
        {

            return (float)(1 + Mathf.Sin(Mathf.PI * time - Mathf.PI / 2)) / 2;
        }
    }
}