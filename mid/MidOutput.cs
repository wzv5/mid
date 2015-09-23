using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace mid
{
    public static class MidOutput
    {
        [DllImport("user32.dll")]
        static extern bool keybd_event(int bVk, int bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, int lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        public static void Play(int code, int duration = 400)
        {
            var f = MidConv.CodeToFrequency(code);
            var n = MidConv.CodeToName(code);
            Console.WriteLine("代码：{0}, 频率：{1}, 唱名：{2}, 音阶：{3}, 升半音：{4}", code, f, n.Diao, n.Qu, n.Ban ? "√" : "×");

            MouseMessagePlayer(code);
        }

        public static void BeepPlayer(int code, int duration)
        {
            var f = MidConv.CodeToFrequency(code);
            Console.Beep(f, duration);
        }

        public static void KeybdPlayer(int code)
        {
            var n = MidConv.CodeToName(code);
            int k = 0;
            switch (n.Qu)
            {
                case 4:
                    k = new int[] { 65, 83, 68, 70, 71, 72, 74 }[n.Diao - 1];
                    break;
                case 5:
                    k = new int[] { 81, 87, 69, 82, 84, 89, 85 }[n.Diao - 1];
                    break;
                case 6:
                    k = new int[] { 49, 50, 51, 52, 53, 54, 55 }[n.Diao - 1];
                    break;
            }
            keybd_event(k, 0, 0, 0);
            keybd_event(k, 0, 2, 0);
        }

        public static void MouseMessagePlayer(int code, int power = 127)
        {
            if (code < 21 || code > 108) // A0 ~ C8
                return;
            var hwnd = FindWindow("FreePianoMainWindow", 0);
            if (hwnd.ToInt64() == 0)
                return;

            var n = MidConv.CodeToName(code);
            code -= 21;
            float p = power / 127;
            int y = 0;
            if (n.Ban)
            {
                y = (int)(340 + 15 * p);
            }
            else
            {
                y = (int)(360 + 25 * p);
            }
            int x = (int)(19 + code * 8.167);
            uint pos = (uint)(x | y << 16);
            PostMessage(hwnd, 512, 0, pos);
            PostMessage(hwnd, 513, 1, pos);
            // 不松开鼠标才可以看到FreePiano的界面动画，否则速度太快界面不刷新
            PostMessage(hwnd, 514, 0, pos);
        }
    }
}
