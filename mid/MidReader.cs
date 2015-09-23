using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace mid
{
    public class MidReader
    {
        private Stream _s;
        private List<TrackState> _tracks = new List<TrackState>();
        public MidHeader Header { get; private set; }

        public MidReader(Stream s)
        {
            _s = s;
            Header = MidReader.ReadHeader(s);
            for (int i = 0; i < Header.TrackCount; i++)
            {
                var t = new TrackState();
                t.ID = i;
                t.Stream = new MemoryStream(MidReader.ReadTrack(s));
                t.NextEvent = MidReader.ReadEvent(t.Stream);
                if (t.NextEvent != null) t.TicksToNextEvent = t.NextEvent.DeltaTime;
                _tracks.Add(t);
            }
        }

        public Tuple<MidEvent, int> GetNextEvent()
        {
            int trackID = 0;
            int ticksToNextEvent = int.MaxValue;
            MidEvent evt = null;
            for (int i = 0; i < _tracks.Count; i++)
            {
                if (_tracks[i].NextEvent != null && _tracks[i].TicksToNextEvent < ticksToNextEvent)
                {
                    trackID = i;
                    ticksToNextEvent = _tracks[i].TicksToNextEvent;
                    evt = _tracks[i].NextEvent;
                }
            }

            if (evt == null)
            {
                return null;
            }

            _tracks[trackID].NextEvent = MidReader.ReadEvent(_tracks[trackID].Stream);
            if (_tracks[trackID].NextEvent != null)
            {
                _tracks[trackID].TicksToNextEvent += _tracks[trackID].NextEvent.DeltaTime;
            }
            for (int i = 0; i < _tracks.Count; i++)
            {
                _tracks[i].TicksToNextEvent -= ticksToNextEvent;
            }
            evt.DeltaTime = ticksToNextEvent;
            return new Tuple<MidEvent, int>(evt, trackID);
        }

        private class TrackState
        {
            public int ID;
            public Stream Stream;
            public MidEvent NextEvent;
            public int TicksToNextEvent;
        }

        private static MidHeader ReadHeader(Stream s)
        {
            if (!s.Read(4).SequenceEqual(new byte[] { 0x4D, 0x54, 0x68, 0x64 }))
            {
                throw new InvalidDataException("不是有效的mid文件");
            }
            s.ReadInt32();
            var header = new MidHeader();
            header.FormatType = s.ReadInt16();
            header.TrackCount = s.ReadInt16();
            header.TicksPerBeat = s.ReadInt16();
            if ((header.TicksPerBeat & 0x8000) != 0)
            {
                throw new InvalidDataException("不支持SMTPE时间格式");
            }
            return header;
        }

        private static byte[] ReadTrack(Stream s)
        {
            if (!s.Read(4).SequenceEqual(new byte[] { 0x4D, 0x54, 0x72, 0x6B }))
            {
                throw new InvalidDataException("数据格式不正确");
            }
            var chunklen = s.ReadInt32();
            return s.Read(chunklen);
        }

        private static MidEvent ReadEvent(Stream s)
        {
            try
            {
                return _ReadEvent(s);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static MidEvent _ReadEvent(Stream s)
        {
            var evt = new MidEvent();
            evt.DeltaTime = s.ReadVarInt();
            evt.EventType = (byte)s.Read(1)[0];
            if ((evt.EventType & 0xf0) == 0xf0)
            {
                // 系统事件、元数据
                switch (evt.EventType)
                {
                    // meta event
                    case 0xff:
                        evt.SubType = (byte)s.Read(1)[0];
                        evt.EventData = s.Read(s.ReadVarInt());
                        break;
                    // sysEx
                    case 0xf0:
                    // dividedSysEx
                    case 0xf7:
                        evt.EventData = s.Read(s.ReadVarInt());
                        break;
                    default:
                        throw new InvalidDataException("未知事件类型");
                }
            }
            else if (evt.EventType <= 0x7f)
            {
                // 独立音符
                evt.EventData = s.Read(1);
            }
            else
            {
                // channel事件
                evt.SubType = (byte)(evt.EventType & 0x0f);  // 子类型保存channel值
                evt.EventType &= 0xf0;
                switch (evt.EventType)
                {
                    case 0x80:  // 松开音符
                    case 0x90:  // 按下音符
                    case 0xa0:  // 触后音符
                    case 0xb0:  // 控制器
                    case 0xe0:  // 滑音
                        evt.EventData = s.Read(2);
                        break;
                    case 0xc0:  // 改变乐器
                    case 0xd0:  // 触后通道
                        evt.EventData = s.Read(1);
                        break;
                    default:
                        throw new InvalidDataException("未知事件类型");
                }
            }
            return evt;
        }
    }

    public class MidEvent
    {
        public int DeltaTime;
        public byte EventType;
        public byte SubType;
        public byte[] EventData;
    }

    public class MidHeader
    {
        public short FormatType;
        public short TrackCount;
        public short TicksPerBeat;
    }
}
