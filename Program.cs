﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Collections;

namespace LyndaCaptionToSrtConvertor
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderPath="";
            int index = 0;
            foreach (string arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    index++;
                    continue;
                }

                switch (arg.ToUpper())
                {
                    case "/D": // Directory 
                        folderPath = args[index + 1];
                        Console.WriteLine("Directory with captions: " + folderPath);
                        break;
                }
            }
            //directory with srt files, will get from console in real use case
            if (String.IsNullOrEmpty(folderPath)) {
                folderPath = "..\\tests";
                Console.WriteLine("defaulting to ..\\tests directory. Please input full subtitle folder path in the /D parameter.");
            }
                if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Directory not found. Press any key to exit");
                // Console app
                Console.ReadKey();
                System.Environment.Exit(1);
            }

            foreach (string entry in Directory.EnumerateFiles(folderPath, "*.caption", SearchOption.AllDirectories))
            {
                string filePath = entry;
                if (!File.Exists(filePath))
                    throw new FileNotFoundException();
                //rea all file in memory
                string content = File.ReadAllText(filePath);

                //crude replacement of characters that are not plain text. File has NUL, SOX, ACK and other non printing ASCII / binary chars
                //this first option works best but there are characters missed by the regexp that will be deleted...regexp must be improved
                //string output = Regex.Replace(content, "[^a-zA-Z0-9 \\[\\]\\(\\)\\s\\r\\n\\t''\"\":;,\\.?!\\@\\#\\$\\%\\^\\&\\*]", string.Empty);

                //second option, delete all non-printing ASCII chars
                //string output = removeBinaryContent(content);

                //third option, delete all non-UTF8 ASCII chars
                //string output = asAscii(content);

                //fourth option, delete all non-UTF8 ASCII printable chars by regexp
                string output = Regex.Replace(content, @"[^\u0020-\u007F]+", "");

                //remove all info at start of file used by Lynda desktop app to link subtitle to video
                if (output.IndexOf("[0") > 0)
                {
                    output = output.Substring(output.IndexOf("[0"));
                }

                //split full formatted text in subtitle sections
                string[] phrases = Regex.Split(output, @"(?=\[[0-9])");

                string start;
                string text;
                string[] subline;
                ArrayList timestamps = new ArrayList();
                ArrayList captions = new ArrayList();
                for (int i=0;i<phrases.Length;i++)
                {
                    try
                    {
                        //get timestamp and text separately
                        subline = Regex.Split(phrases[i], @"(?<=\[[0-9:,.]+\])");
                        if (subline.Length == 2)
                        {
                            //separator for miliseconds is ',' in srt, '.' in .caption 
                            start = Regex.Replace(subline[0],"\\.", ",");
                            start = start.Substring(1, start.Length - 2);
                            text = subline[1];
                            timestamps.Add(start);
                            captions.Add(text);
                        }
                    }
                    finally {
                    }
                }
                string filename = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".srt";
                buildSrt(timestamps, captions, filename);
                //got list of timestamps here and list of texts, start building the .srt file
                Console.WriteLine("Done " + filename);
            }
            
            Console.WriteLine("Press any key to exit the program...");
            Console.ReadKey();
        }

        //remove all byte characters not containing text or timestamps from caption.
        //assumes all text is ASCII
        static string removeBinaryContent(string aString)
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(aString);
            StringBuilder sb = new StringBuilder(strBytes.Length);
            foreach (char c in strBytes.Select(b => (char)b))
            {
                if (c < '\u0020' || c == '\u007F') {}
                else if (c > '\u007F') { sb.AppendFormat(@"\u{0:X4}", (ushort)c); }
                else /* 0x20-0x7E */      { sb.Append(c); }
            }
            return sb.ToString();
        }

        static string asAscii(string aString)
        {
            return Encoding.ASCII.GetString(
                Encoding.Convert(
                    Encoding.UTF8,
                    Encoding.GetEncoding(
                        Encoding.ASCII.EncodingName,
                        new EncoderReplacementFallback(string.Empty),
                        new DecoderExceptionFallback()
                        ),
                    Encoding.UTF8.GetBytes(aString)
                )
            );
        }

        static bool buildSrt(ArrayList timestamps,ArrayList captions,string path)
        {
            StreamWriter writer = new StreamWriter(path);
            //SRT is perhaps the most basic of all subtitle formats.
            //It consists of four parts, all in text..

            //1.A number indicating which subtitle it is in the sequence.
            //2.The time that the subtitle should appear on the screen, and then disappear.
            //3.The subtitle itself.
            //4.A blank line indicating the start of a new subtitle.

            //1
            //00:02:17,440-- > 00:02:20,375
            //and here goes the text, after which there's a blank line

            //last iinput in array is a single timestamp with no text, used only to see where the end of the last caption is
            for (int i=0;i<timestamps.Count-1;i++)
            {
                writer.WriteLine(i+1);
                writer.WriteLine(timestamps[i]+ " --> " + timestamps[i+1]);
                writer.WriteLine(captions[i]);
                writer.WriteLine();
            }
            writer.Close();
            return true;
        }

    }
}