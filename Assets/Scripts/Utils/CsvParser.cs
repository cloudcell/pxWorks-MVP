using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Templates
{
    public class CsvParser
    {
        public char Separator { get; set; }
        public char Quote { get; set; }
        public bool TrimLineAtEnd { get; set; }

        public List<string> ColumnNames { get; private set; }
        public IEnumerable<List<string>> Rows { get; private set; }

        public CsvParser()
        {
            Separator = '\t';
            Quote = '"';
        }

        public static char AutoDetectSeparator(string fileName, Encoding enc)
        {
            fileName = fileName.Split(';')[0];
            using (StreamReader sr = new StreamReader(fileName, enc))
            while (sr.Peek() >= 0)
                return AutoDetectSeparator(sr.ReadLine());

            return ',';
        }

        public static char AutoDetectSeparator(string s)
        {
            //если есть табуляции - скорее всего это и есть разделитель
            if (s.Contains("\t")) return '\t';
            //считаем число запятых и точек с запятыми
            int semicolonCount = 0;
            int commaCount = 0;
            foreach (char c in s)
                if (c == ';') semicolonCount++;
                else
                    if (c == ',') commaCount++;
            //точек с запятыми больше чем запятых
            if (semicolonCount > commaCount) return ';';
            return ',';
        }

        #region Parse methods

        public void Parse(string fileName, Encoding enc, bool parseColumnNames = true, bool autoDetectSeparator = false)
        {
            if (autoDetectSeparator)
                Separator = AutoDetectSeparator(fileName, enc);

            if (parseColumnNames)
            {
                ColumnNames = Parse(ReadLines(fileName, enc)).FirstOrDefault();
                Rows = Parse(ReadLines(fileName, enc)).Skip(1);
            }else
                Rows = Parse(ReadLines(fileName, enc));
        }

        public void Parse(Stream stream, Encoding enc, bool parseColumnNames = true)
        {
            if (parseColumnNames)
            {
                ColumnNames = Parse(ReadLines(stream, enc)).FirstOrDefault();
                stream.Position = 0;
                Rows = Parse(ReadLines(stream, enc)).Skip(1);
            }
            else
                Rows = Parse(ReadLines(stream, enc));
        }

        public void Parse(string text, bool parseColumnNames = true, bool autoDetectSeparator = false)
        {
            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Parse(lines, parseColumnNames, autoDetectSeparator);
        }

        public void Parse(IEnumerable<string> lines, bool parseColumnNames = true, bool autoDetectSeparator = false)
        {
            if (autoDetectSeparator)
                Separator = AutoDetectSeparator(lines.FirstOrDefault());

            if (parseColumnNames)
            {
                ColumnNames = Parse(lines).FirstOrDefault();
                Rows = Parse(lines).Skip(1);
            }
            else
                Rows = Parse(lines);
        }

        #endregion

        private IEnumerable<string> ReadLines(Stream stream, Encoding enc)
        {
            using (StreamReader sr = new StreamReader(stream, enc))
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    if (TrimLineAtEnd) line = line.TrimEnd();
                    yield return line;
                }
        }

        private IEnumerable<string> ReadLines(string fileName, Encoding enc)
        {
            using (StreamReader sr = new StreamReader(fileName, enc))
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    if (TrimLineAtEnd) line = line.TrimEnd();
                    yield return line;
                }
        }

        private IEnumerable<List<string>> Parse(IEnumerable<string> lines)
        {
            var e = lines.GetEnumerator();
            while (e.MoveNext())
                yield return ParseLine(e);
        }

        private List<string> ParseLine(IEnumerator<string> e)
        {
            var items = new List<string>();
            foreach (string token in GetToken(e))
                items.Add(token);
            return items;
        }

        private IEnumerable<string> GetToken(IEnumerator<string> e)
        {
            string token = "";
            State state = State.outQuote;

        again:
            foreach (char c in e.Current)
                switch (state)
                {
                    case State.outQuote:
                        if (c == Separator)
                        {
                            yield return token;
                            token = "";
                        }
                        else
                            if (c == Quote)
                                state = State.inQuote;
                            else
                                token += c;
                        break;
                    case State.inQuote:
                        if (c == Quote)
                            state = State.mayBeOutQuote;
                        else
                            token += c;
                        break;
                    case State.mayBeOutQuote:
                        if (c == Quote)
                        {
                            //кавычки внутри кавычек
                            state = State.inQuote;
                            token += c;
                        }
                        else
                            if (c != Separator)
                            {
                                //кавычки внутри кавычек
                                state = State.inQuote;
                                token += Quote;
                                token += c;
                            }
                            else
                            {
                                state = State.outQuote;
                                goto case State.outQuote;
                            }
                        break;
                }

            //разрыв строки внутри кавычек
            if (state == State.inQuote && e.MoveNext())
            {
                token += Environment.NewLine;
                goto again;
            }

            yield return token;
        }

        enum State { outQuote, inQuote, mayBeOutQuote }
    }
}
