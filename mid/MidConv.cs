using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mid
{
    public static class MidConv
    {
        public static int CodeToFrequency(int code)
        {
            double a = (code - 48 - 21) / 12.0;
            a = Math.Pow(2, a);
            a = a * 440;
            return (int)a;
        }

        public static GuDingYinMing CodeToName(int code)
        {
            int diao = 0;
            int qu = 0;
            bool ban = false;

            qu = code / 12;
            code -= qu * 12;
            diao = new int[] { 1, 1, 2, 2, 3, 4, 4, 5, 5, 6, 6, 7 }[code];
            ban = new int[] { 1, 3, 6, 8, 10 }.Contains(code);
            return new GuDingYinMing(diao, qu, ban);
        }

        public static int NameToCode(GuDingYinMing name)
        {
            int a = new int[] { 0, 2, 4, 5, 7, 9, 11 }[name.Diao - 1];
            if (name.Ban && new int[] { 1, 2, 4, 5, 6 }.Contains(name.Diao))
                ++a;
            a += (name.Qu + 1) * 12;
            return a;
        }

        public class GuDingYinMing
        {
            public int Diao;
            public int Qu;
            public bool Ban;

            public GuDingYinMing(int diao, int qu, bool ban = false)
            {
                Diao = diao;
                Qu = qu;
                Ban = ban;
            }
        }
    }
}
