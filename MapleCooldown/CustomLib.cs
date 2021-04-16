using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace CommonFunctions
{
    public static class CustomLib
    {
        /// <summary>
        /// Convert System.String object into Keycode
        /// </summary>
        /// <param name="str">Keycode as string to be parsed</param>
        /// <returns></returns>
        public static Keys? Str2Key(string str)
        {
            Keys key;
            if (Enum.TryParse(str, out key))
                return key;
            else
                return null;
        }

        public class Timer
        {
            public static bool hasRun = false;

            public static void DisplayTimerProperties()
            {
                long timeAdjustment, timeIncrement = 0;
                bool timeAdjustmentDisabled;

                if (GetSystemTimeAdjustment(out timeAdjustment, out timeIncrement,
                                            out timeAdjustmentDisabled))
                {
                    if (!timeAdjustmentDisabled)
                        Console.WriteLine("System clock resolution: {0:N3} milliseconds",
                                          timeIncrement / 10000.0);
                    else
                        Console.WriteLine("Unable to determine system clock resolution.");
                }

                // Display the timer frequency and resolution.
                if (Stopwatch.IsHighResolution)
                {
                    Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
                }
                else
                {
                    Console.WriteLine("Operations timed using the DateTime class.");
                }

                long frequency = Stopwatch.Frequency;
                Console.WriteLine("  Timer frequency in ticks per second = {0}",
                    frequency);
                long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
                Console.WriteLine("  Timer is accurate within {0} nanoseconds",
                    nanosecPerTick);
            }

            /// <summary>
            /// Block thread execution for specified amount of time. Timer resolution can achieve up to 10 MHz accuracy
            /// </summary>
            /// <param name="durationSeconds">Amount of seconds to block operation for</param>
            public void NOP_highCPU(double durationMilliseconds)
            {
                if (!hasRun)
                {
                    DisplayTimerProperties();
                    hasRun = true;
                }
                var durationTicks = Math.Round(durationMilliseconds / 1000f * Stopwatch.Frequency);
                var sw = Stopwatch.StartNew();
                /* Wait for timer to elapse */
                while (sw.ElapsedTicks < durationTicks)
                {
                    //Thread.Sleep(0); probs not needed
                }
            }

            /// <summary>
            /// Wait for N seconds using non CPU-heavy block-method.
            /// </summary>
            /// <param name="durationSeconds"></param>
            public void NOP_lowCPU(int durationMilliSeconds)
            {
                if (!hasRun)
                {
                    DisplayTimerProperties();
                    hasRun = true;
                }
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                var aTimer = new System.Timers.Timer(durationMilliSeconds);
                aTimer.Elapsed += (sender, e) => resetEvent.Set();
                aTimer.AutoReset = false;
                aTimer.Start();

                resetEvent.WaitOne(); // This blocks the thread until resetEvent is set
                resetEvent.Close();
                aTimer.Stop();
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetSystemTimeAdjustment(out long lpTimeAdjustment,
                                              out long lpTimeIncrement,
                                              out bool lpTimeAdjustmentDisabled);
        }
    }
}