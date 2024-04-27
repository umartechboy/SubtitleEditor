namespace SubtitleEditor.SectionDef
{
    public class common
    {
        public static string timeToString(int secs)
        {
            string s = (secs / 3600).ToString().PadLeft(2, '0') + ":" +
                (secs / 60 % 60).ToString().PadLeft(2, '0') + ":" +
                (secs % 60).ToString().PadLeft(2, '0');
            return s;
        }
        public static string timeToString(double secsD)
        {
            var secs = (int)secsD;
            string s = (secs / 3600).ToString().PadLeft(2, '0') + ":" +
                (secs / 60 % 60).ToString().PadLeft(2, '0') + ":" +
                (secs % 60).ToString().PadLeft(2, '0');
            s += "." + Math.Round((secsD - secs) * 30).ToString().PadLeft(2, '0');
            return s;
        }

        public static int stringToSeconds(string time)
        {
            var prts = time.Split(new char[] { ':' });
            int ans = 0;
            if (prts.Length == 3) ans = Convert.ToInt16(prts[0]) * 3600 + Convert.ToInt16(prts[1]) * 60 + Convert.ToInt16(prts[2]);
            if (prts.Length == 2) ans = Convert.ToInt16(prts[0]) * 60 + Convert.ToInt16(prts[1]);
            if (prts.Length == 1) ans = Convert.ToInt16(prts[0]);
            return ans;
        }

        public static bool ListsSame(List<int> l1, List<int> l2, bool recursive = true)
        {
            if (recursive) { l1.Sort(); l2.Sort(); }
            bool ans = true;
            foreach (var i1 in l1)
            {
                if (l2.IndexOf(i1) != l1.IndexOf(i1) || l2.LastIndexOf(i1) != l1.LastIndexOf(i1))
                    ans = false;
            }
            if (ans == false) return false;
            if (recursive)
                return ListsSame(l2, l1, false);
            return ans;
        }
    }
}
