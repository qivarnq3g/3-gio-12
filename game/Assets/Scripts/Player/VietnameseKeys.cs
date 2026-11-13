using UnityEngine.InputSystem;

namespace BaGio12.Gameplay
{
    public static class VietnameseKeys
    {
        enum Mark { Grave, Acute, Hook, Tilde, Dot, Breve, Circumflex, Horn, Stroke, Count }

        static readonly string[] Rows =
        {
            "aàáảãạ", "ăằắẳẵặ", "âầấẩẫậ", "eèéẻẽẹ", "êềếểễệ", "iìíỉĩị",
            "oòóỏõọ", "ôồốổỗộ", "ơờớởỡợ", "uùúủũụ", "ưừứửữự", "yỳýỷỹỵ",
        };
        static readonly char[] RowBase = { 'a', 'a', 'a', 'e', 'e', 'i', 'o', 'o', 'o', 'u', 'u', 'y' };
        static readonly Mark[] RowShape = { Mark.Count, Mark.Breve, Mark.Circumflex, Mark.Count, Mark.Circumflex, Mark.Count, Mark.Count, Mark.Circumflex, Mark.Horn, Mark.Count, Mark.Horn, Mark.Count };
        static readonly char[] LetterMarks = { 'f', 's', 'r', 'x', 'j', 'w', '\0', 'w', 'd' };
        static readonly char[] NumberMarks = { '2', '1', '3', '4', '5', '8', '6', '7', '9' };

        public static Key Trigger(string before, string after, bool numberMarks)
        {
            var marks = new int[(int)Mark.Count];
            var letters = new int[128];
            var circumflexBase = '\0';
            Count(before, -1, marks, letters, ref circumflexBase);
            circumflexBase = '\0';
            Count(after, 1, marks, letters, ref circumflexBase);
            for (int m = 0; m < (int)Mark.Count; m++)
            {
                if (marks[m] <= 0) continue;
                char c = (Mark)m == Mark.Circumflex && !numberMarks ? circumflexBase : (numberMarks ? NumberMarks : LetterMarks)[m];
                return ToKey(c);
            }
            for (int i = after.Length - 1; i >= 0; i--)
            {
                char c = char.ToLowerInvariant(after[i]);
                if (c < 128 && letters[c] > 0) return ToKey(c);
            }
            return Key.None;
        }

        static void Count(string text, int sign, int[] marks, int[] letters, ref char circumflexBase)
        {
            foreach (char raw in text)
            {
                char c = char.ToLowerInvariant(raw);
                if (c == 'đ') { marks[(int)Mark.Stroke] += sign; letters['d'] += sign; continue; }
                if (c < 128) { letters[c] += sign; continue; }
                for (int r = 0; r < Rows.Length; r++)
                {
                    int tone = Rows[r].IndexOf(c);
                    if (tone < 0) continue;
                    letters[RowBase[r]] += sign;
                    if (RowShape[r] != Mark.Count) marks[(int)RowShape[r]] += sign;
                    if (RowShape[r] == Mark.Circumflex) circumflexBase = RowBase[r];
                    if (tone > 0) marks[tone - 1] += sign;
                    break;
                }
            }
        }

        static Key ToKey(char c)
        {
            if (c >= 'a' && c <= 'z') return Key.A + (c - 'a');
            if (c >= '1' && c <= '9') return Key.Digit1 + (c - '1');
            if (c == '0') return Key.Digit0;
            return Key.None;
        }
    }
}
