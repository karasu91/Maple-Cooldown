using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using static MapleCooldown.Program;
using static CommonFunctions.CustomLib;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Drawing.Drawing2D;
using CommonFunctions;
using System.Runtime.InteropServices;

namespace MapleCooldown
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public const int HT_CAPTION = 0x2;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public int rectangleSize = Program.ui.imageSize;
        private static List<KeyValuePair<PictureBox, Skill>> _pictureBoxes = new List<KeyValuePair<PictureBox, Skill>>();
        private static Thread _uiThread;

        // px
        private string _fontFamily = ui.font;

        private int _fontSize = ui.fontSize;
        private int _iconOffset = 5;
        private int windowPosX = ui.windowPosX;
        private int windowPosY = ui.windowPosY;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // px
        public void AddCooldownElement(Skill s)
        {
            var picture = new PictureBox
            {
                Name = "skill" + _numberOfCooldowns,
                Size = new Size(rectangleSize, rectangleSize),
                Location = new Point(_numberOfCooldowns * (rectangleSize + _iconOffset), 0),
                Image = Image.FromFile(s.iconPath),
                SizeMode = PictureBoxSizeMode.StretchImage,
            };
            picture.MouseDown += Form1_MouseDown;

            picture.Paint += new PaintEventHandler((sender, e) =>
            {
                //Console.WriteLine("X: {0}, Y: {1}", picture.Location.X.ToString(), picture.Location.Y.ToString());
                var cd = s.cooldownRemaining;

                picture.Visible = cd <= 0;

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                string text = "";
                if (cd <= 0.0f) 
                    text = "";
                else if (cd <= 5.1f && cd > 0.0f) 
                    text = Math.Round(cd, 1, MidpointRounding.AwayFromZero).ToString(); 
                else 
                    text = Math.Ceiling(cd).ToString();

                var font = new Font(_fontFamily, _fontSize / 2, FontStyle.Regular);
                SizeF textSize = e.Graphics.MeasureString(text, font);

                PointF outline_loc = new PointF();
                PointF outline_loc2 = new PointF();
                PointF outline_loc3 = new PointF();
                PointF outline_loc4 = new PointF();

                Brush outline_brush = Brushes.Black;
                float outline_offset = 1.0f;

                /* Add outlines for text. Done by shifting black outlines to up, left, down and right. */
                outline_loc.X = (picture.Width / 2) - (textSize.Width / 2) - outline_offset;
                outline_loc.Y = (picture.Height / 2f) - (textSize.Height / 2f) - outline_offset;
                outline_loc2.X = (picture.Width / 2) - (textSize.Width / 2) + outline_offset;
                outline_loc2.Y = (picture.Height / 2f) - (textSize.Height / 2f) + outline_offset;
                outline_loc3.X = (picture.Width / 2) - (textSize.Width / 2) + outline_offset;
                outline_loc3.Y = (picture.Height / 2f) - (textSize.Height / 2f) - outline_offset;
                outline_loc4.X = (picture.Width / 2) - (textSize.Width / 2) - outline_offset;
                outline_loc4.Y = (picture.Height / 2f) - (textSize.Height / 2f) + outline_offset;

                PointF locationToDraw2 = new PointF();
                locationToDraw2.X = (picture.Width / 2) - (textSize.Width / 2);
                locationToDraw2.Y = (picture.Height / 2f) - (textSize.Height / 2f);

                e.Graphics.DrawString(text, font, outline_brush, outline_loc);
                e.Graphics.DrawString(text, font, outline_brush, outline_loc2);
                e.Graphics.DrawString(text, font, outline_brush, outline_loc3);
                e.Graphics.DrawString(text, font, outline_brush, outline_loc4);
                e.Graphics.DrawString(text, font, Brushes.White, locationToDraw2);
            });

            picture.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    s.ResetState();
                }
            };

            this.Controls.Add(picture);
            _pictureBoxes.Add(new KeyValuePair<PictureBox, Skill>(picture, s));
        }

        /// <summary>
        ///  For cross-referencing, executes actions on another thread's scope (avoids cross-reference exceptions in multithreaded applications)
        /// </summary>
        /// <param name="A">Action describing the function</param>
        public void DoAction(Action A)
        {
            Invoke(new Action(() =>
            {
                A();
            }));
        }

        public void UpdateUIElements()
        {
            var _timer = new CustomLib.Timer();
            while (true)
            {
                DoAction(() =>
                {
                    foreach (var pic in _pictureBoxes)
                    {
                        var picBox = pic.Key;
                        var skillAssociated = pic.Value;

                        picBox.Refresh();

                        var cdRemain = skillAssociated.cooldownRemaining;
                        picBox.Visible = cdRemain <= 0 || skillAssociated.isOnCooldown == false;

                    }
                    TopMost = true;
                });
                _timer.NOP_lowCPU(100); // 10 times per second
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            TopLevel = true;
            TopMost = true;
            Visible = false;
            FormBorderStyle = FormBorderStyle.None;
            _uiThread = new Thread(UpdateUIElements);
            _uiThread.Start();
            Location = new Point(windowPosX - SkillContainer.Count() * rectangleSize, windowPosY);

            this.FormClosing += (_, __) =>
            {
                // serialize JSON directly to a file
                using (StreamWriter file = File.CreateText(Path.Combine(Environment.CurrentDirectory, "ui2.config.json")))
                {
                    Program.ui.windowPosX = this.Location.X;
                    Program.ui.windowPosY = this.Location.Y;
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, Program.ui);
                };
            };
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private int _numberOfCooldowns => _pictureBoxes.Count(); // Amount of cooldowns assigned to UI
    }
}