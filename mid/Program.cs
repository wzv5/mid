using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace mid
{
    class Program
    {
        static void Main(string[] args)
        {
            ReadMid(args[0]);
        }

        static void ReadMid(string path)
        {
            long n = 0;
            double bpm = 120;
            int delay = 0;
            int time = 0;
            using (var fs = File.OpenRead(path))
            {
                var mid = new MidReader(fs);
                var h = mid.Header;
                Console.WriteLine("音轨格式：{0}", h.FormatType);
                Console.WriteLine("音轨数：{0}", h.TrackCount);
                Console.WriteLine("基本时间（每拍Tick数）：{0}", h.TicksPerBeat);
                Console.WriteLine("===================");
                while (true)
                {
                    var evt = mid.GetNextEvent()?.Item1;
                    if (evt == null)
                        return;
                    delay = (int)(1000.0 * evt.DeltaTime / h.TicksPerBeat / (bpm / 60));
                    time += delay;
                    if (delay > 5) Thread.Sleep(delay);
                    {
                        var c0 = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("[{0}] ", n);
                        Console.ForegroundColor = ConsoleColor.Green;
                        TimeSpan ts = new TimeSpan(0, 0, 0, 0, time);
                        Console.Write("[{0}] ", ts.ToString(@"mm\:ss\.ff"));
                        Console.ForegroundColor = c0;
                    }
                    ++n;
                    bool isOutEvent = false;
                    int code = 0;
                    int power = 0;
                    if (evt.EventType <= 0x7f)
                    {
                        code = evt.EventType;
                        power = evt.EventData[0];
                        isOutEvent = true;
                    }
                    else if (evt.EventType == 0x90)
                    {
                        code = evt.EventData[0];
                        power = evt.EventData[1];
                        isOutEvent = true;
                    }
                    else if (evt.EventType == 0xff && evt.SubType == 0x51)
                    {
                        var sd = new List<byte>();
                        sd.Add(0);
                        sd.AddRange(evt.EventData);
                        sd.Reverse();
                        double tt = BitConverter.ToInt32(sd.ToArray(), 0) / 1000.0;    // 默认 500 ms
                        bpm = 60000.0 / tt;
                        var c0 = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("速度：{0} BPM", (int)bpm);
                        Console.ForegroundColor = c0;
                    }
                    else
                    {
                        Console.WriteLine("事件：{0:x}, {1:x}, {2}", evt.EventType, evt.SubType, BitConverter.ToString(evt.EventData));
                    }
                    if (isOutEvent)
                    {
                        var c0 = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        var name = MidConv.CodeToName(code);
                        Console.WriteLine("音符：{0}{1}{2}", name.Ban ? "#" : "", new char[] { 'C', 'D', 'E', 'F', 'G', 'A', 'B' }[name.Diao - 1], name.Qu);
                        Console.ForegroundColor = c0;
                        if (power > 0) MidOutput.MouseMessagePlayer(code, power);
                    }
                }
            }
        }
        /*
        static void ReadChunk(byte[] data)
        {
            using (BinaryReader r = new BinaryReader(new MemoryStream(data)))
            {
                while (true)
                {
                    var t = ReadNum(r);
                    if (t > 0)
                    {
                        int tt = (int)(1000.0 * t / tpn / (bpm / 60));
                        Console.WriteLine("延时：{0}，{1} ms", t, tt);
                        if (isplay && tt < 5000) Thread.Sleep((int)tt);
                    }
                    byte c = 0;
                    try
                    {
                        c = r.ReadByte();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    
                    if (c <= 0x7f)
                    {
                        // 音符
                        var power = r.ReadByte();
                        Console.WriteLine("独立音符：{0}，力度：{1}", c, power);
                        if (power > 10 && isplay)
                        {
                            MidOutput.MouseMessagePlayer(c, power);
                        }
                    }
                    else if (c == 0xff)
                    {
                        // 其他格式
                        var a = r.ReadByte();
                        var l = ReadNum(r);
                        var meatadata = r.ReadBytes((int)l);

                        if (a == 0x00)
                        {
                            Console.WriteLine("设置音轨：{0}", ReadUInt16(meatadata));
                        }
                        else if (a == 0x01)
                        {
                            Console.WriteLine("歌词备注：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x02)
                        {
                            Console.WriteLine("歌曲版权：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x03)
                        {
                            Console.WriteLine("歌曲标题：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x04)
                        {
                            Console.WriteLine("乐器名称：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x05)
                        {
                            Console.WriteLine("歌词：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x06)
                        {
                            Console.WriteLine("标记：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x07)
                        {
                            Console.WriteLine("开始点：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x7f)
                        {
                            Console.WriteLine("音序特定信息：{0}", Encoding.GetEncoding("GBK").GetString(meatadata));
                        }
                        else if (a == 0x51)
                        {
                            var sd = new List<byte>();
                            sd.Add(0);
                            sd.AddRange(meatadata);
                            uint tt = ReadUInt32(sd.ToArray()) / 1000;    // 默认 500 ms
                            bpm = 60000 / tt;
                            Console.WriteLine("速度：{0} BPM（Beat Per Minute）", (int)bpm);

                        }
                        else if (a == 0x58)
                        {
                            // 默认 4/4，24，8
                            Console.WriteLine("节拍：{0} / {1}, {2}, {3}",meatadata[0], Math.Pow(2, meatadata[1]), meatadata[2], meatadata[3]);
                        }
                        else if (a == 0x59)
                        {
                            Console.WriteLine("调号：{0}", BitConverter.ToString(meatadata));
                        }
                        else if (a == 0x2f)
                        {
                            Console.WriteLine("音轨结束");
                            return;
                        }
                    }
                    else if (c == 0xf0)
                    {
                        // 系统码
                        var list = new List<byte>();
                        while (true)
                        {
                            var cc = r.ReadByte();
                            if (cc == 0xf7)
                            {
                                break;
                            }
                            list.Add(cc);
                        }
                        Console.WriteLine("系统码：{0}", BitConverter.ToString(list.ToArray(), 0));
                    }
                    else
                    {
                        var cc = c >> 4;
                        if (cc == 0x8)
                        {
                            // 松开音符
                            Console.WriteLine("松开音符：音轨{0}，音符{1}，力度：{2}", c & 0xf, r.ReadByte(), r.ReadByte());
                        }
                        else if (cc == 0x9)
                        {
                            // 按下音符
                            var code = r.ReadByte();
                            var power = r.ReadByte();
                            Console.WriteLine("按下音符：音轨{0}，音符{1}，力度：{2}", c & 0xf, code, power);
                            if (isplay && power > 10) MidOutput.MouseMessagePlayer(code, power);
                        }
                        else if (cc == 0xa)
                        {
                            // 触后音符
                            Console.WriteLine("触后音符：音轨{0}，音符{1}，力度：{2}", c & 0xf, r.ReadByte(), r.ReadByte());
                        }
                        else if (cc == 0xb)
                        {
                            // 控制器
                            Console.WriteLine("控制器：音轨{0}，号码{1}，参数：{2}", c & 0xf, r.ReadByte(), r.ReadByte());
                        }
                        else if (cc == 0xc)
                        {
                            // 改变乐器
                            Console.WriteLine("改变乐器：{0}", r.ReadByte());
                        }
                        else if (cc == 0xd)
                        {
                            // 触后通道
                            Console.WriteLine("触后通道：{0}", r.ReadByte());
                        }
                        else if (cc == 0xe)
                        {
                            // 滑音
                            Console.WriteLine("滑音：{0}，{1}", r.ReadByte(), r.ReadByte());
                        }
                        else
                        {
                            Console.WriteLine("未知控制码：{0}", c);
                        }
                    }
                }
            }
        }
        */
    }
}
