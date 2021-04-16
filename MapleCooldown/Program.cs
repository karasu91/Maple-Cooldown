using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linearstar.Windows.RawInput;
using CommonFunctions;
using System.Linq.Expressions;
using ConsoleHotKey;
using System.Runtime.Remoting.Channels;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MapleCooldown
{
    public static class Program
    {
        /* Program heartbeat */
        public static Queue<string> KeyQueue = new Queue<string>();
        public static Keys ResetKey;
        public static List<Skill> SkillContainer = new List<Skill>();
        public static UI ui = new UI();
        private static CustomLib.Timer _timer = new CustomLib.Timer();

        public static void ReadAppConfigJson()
        {
            try
            {
                var mypath = Path.Combine(Environment.CurrentDirectory, "app.config.json");
                string json = File.ReadAllText(mypath);

                Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(json);
                SkillContainer = myDeserializedClass.skills;

                var mypath2 = Path.Combine(Environment.CurrentDirectory, "ui.config.json");
                string json2 = File.ReadAllText(mypath2);
                var ui_root = JsonConvert.DeserializeObject<RootUI>(json2);
                ui = ui_root.UI;
                ResetKey = CustomLib.Str2Key(ui.resetKey).Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void SendKey(ushort key)
        {
            Console.WriteLine("Sending scancode {0}", key);
            Input[] inputs =
            {
                new Input
                {
                    type = (int) InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = key,
                            dwFlags = (uint)KeyEventF.Scancode,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            };

            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input))) != 0)
                Console.WriteLine("Sent {0} successfully!", CustomLib.Str2Key(inputs[0].u.ki.wScan.ToString()));
            else
                Console.WriteLine("Failed to send key");
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        private static void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            /* TODO: Enqueue only if active window equals a game's window */
            //if (e.Key.ToString() ==

            KeyQueue.Enqueue(e.Key.ToString());
            Console.WriteLine("keycode = {0}", e.Key);
            if (e.Key != ResetKey)
                SendKey(VKey_2_ScanCode.KeycodeTable[e.Key]);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        ///
        [STAThread]
        private static void Main()
        {
            /* Read skill cooldown settings from ./app.config.json */
            ReadAppConfigJson();

            var form = new Form1();
            /* Initialize cooldown elements (pictureboxes) for UI */
            foreach (Skill s in SkillContainer)
                form.AddCooldownElement(s);

            /* Keep UI responsive by externalizing it on another thread. */
            Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(form);
            });

            SkillContainer.ForEach(p => Console.WriteLine(p.ToString()));

            /* Wait for UI to start TODO: do this in a better way. */
            _timer.NOP_lowCPU(1000);

            foreach (Skill s in SkillContainer)
            {
                var _key = (Keys)CustomLib.Str2Key(s.keyboardKey);
                Console.WriteLine("Registering {0} as a hotkey", _key);
                HotKeyManager.RegisterHotKey(_key, KeyModifiers.NoRepeat);
                HotKeyManager.RegisterHotKey(_key, KeyModifiers.Shift);
            }
            HotKeyManager.RegisterHotKey(Keys.Escape, KeyModifiers.NoRepeat);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);

            /* Begin polling user keys, and set skills on cooldown correspondingly. */
            int sleepMilliseconds = 100;
            List<string> keysHandled = new List<string>();

            while (true)
            {
                if (KeyQueue.Contains(ResetKey.ToString()))
                {
                    SkillContainer.ForEach(p => p.ResetState());
                    KeyQueue.Clear();
                }

                /* Read key from external key FIFO-buffer. Key is read externally so program is not blocked. */
                foreach (var Key in KeyQueue)
                {
                    KeyQueue.ToList().ForEach(q => Console.WriteLine(q.ToString()));

                    if (!keysHandled.Contains(Key)) // Do not handle same key more than once
                    {
                        /* Print status of all skills on spacebar input */
                        if (Key == ConsoleKey.Spacebar.ToString())
                        {
                            Console.Clear();
                            foreach (Skill s in SkillContainer)
                            {
                                Console.WriteLine(s.ToString());
                            }
                            continue;
                        }

                        /* Get list of skills that are bound to the pressed key */
                        var skillsActivated = SkillContainer.Where(p => p.keyboardKey.Equals(Key.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                        /* Set the skill on cooldown; set timer to maximum cooldown value and begin decrementing it.
                         * IsActive must be set so that the timer will start running down. */
                        if (skillsActivated.Count() > 0)
                        {
                            skillsActivated.ForEach(p =>
                            {
                                if (p.isOnCooldown == false)
                                    p.cooldownRemaining = p.cooldownSeconds;
                            });
                            skillsActivated.ForEach(p => p.isOnCooldown = true);
                            keysHandled.Add(Key);
                        }
                    }
                }
                KeyQueue.Clear();
                keysHandled.Clear();
                UpdateCooldowns(sleepMilliseconds);
                _timer.NOP_lowCPU(sleepMilliseconds);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        private static void UpdateCooldowns(float milliseconds)
        {
            foreach (Skill s in SkillContainer.Where(_ => _.isOnCooldown))
            {
                s.cooldownRemaining -= milliseconds / 1000f;
                if (s.cooldownRemaining <= 0.0f)
                {
                    s.isOnCooldown = false;
                    s.cooldownRemaining = 0f;
                }
            }
        }

        [Flags]
        private enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        private enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInput
        {
            public readonly uint uMsg;
            public readonly ushort wParamL;
            public readonly ushort wParamH;
        }

        private struct Input
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public readonly MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public readonly HardwareInput hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public readonly uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public readonly int dx;
            public readonly int dy;
            public readonly uint mouseData;
            public readonly uint dwFlags;
            public readonly uint time;
            public readonly IntPtr dwExtraInfo;
        }
    }
}