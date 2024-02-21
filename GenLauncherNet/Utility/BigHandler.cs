using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenLauncherNet
{
    public static class BinaryReaderExtension
    {
        public static string ReadFourCc(this BinaryReader reader, bool bigEndian = false)
        {
            var a = (char)reader.ReadByte();
            var b = (char)reader.ReadByte();
            var c = (char)reader.ReadByte();
            var d = (char)reader.ReadByte();

            return bigEndian
                ? new string(new[] { d, c, b, a })
                : new string(new[] { a, b, c, d });
        }
    }

    public static class BigHandler
    {
        public static bool IsBigArchive(string filePath)
        {
            if (!File.Exists(filePath))
                throw new ArgumentException("Cannot find file " + filePath);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                {
                    using (var reader = new BinaryReader(fileStream, Encoding.ASCII, true))
                    {
                        try
                        {
                            //Special case for empty archives/ placeholder archives
                            if (reader.BaseStream.Length < 4)
                            {
                                var a = reader.ReadByte();
                                var b = reader.ReadByte();

                                if (a == '?' && b == '?')
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            var fourCc = reader.ReadFourCc();
                            switch (fourCc)
                            {
                                case "BIGF":
                                    return true;

                                case "BIG4":
                                    return true;

                                default:
                                    return false;
                            }
                        }
                        catch
                        {
                            //TODO logger
                            return false;
                        }
                    }
                }
            }
            catch
            {
                //TODO logger
                return false;
            }
        }

        //Code by MementoMori
        private class BigReader : BinaryReader
        {
            public BigReader(Stream stream) : base(stream, Encoding.ASCII) { }
            /// <summary>
            /// Read string
            /// </summary>
            /// <param name="charCount">Char count</param>
            /// <returns>Return string</returns>
            public string ReadString(int charCount)
            {
                StringBuilder stringBuilder = new StringBuilder(charCount);
                for (int i = 0; i < charCount; i++)
                    stringBuilder[i] = this.ReadChar();
                return stringBuilder.ToString();
            }
            /// <summary>
            /// Read string
            /// </summary>
            /// <param name="ch">Null terminated char</param>
            /// <returns></returns>
            public string ReadString(char ch)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (; ; )
                {
                    //Проверку на конец потока
                    char c = this.ReadChar();
                    if (c == ch)
                        return stringBuilder.ToString();
                    else
                        stringBuilder.Append(c);
                }
            }
            public uint ReadUintBigEndian() => BitConverter.ToUInt32(this.ReadBytes(4).Reverse().ToArray(), 0);            
            public uint ReadUintLittleEndian() => BitConverter.ToUInt32(this.ReadBytes(4), 0);
        }

        public static bool FileContainsGameDataIni(string bigName)
        {
            var namesList = new List<string>();

            BigReader bigReader = new BigReader(new FileStream(bigName, FileMode.Open, FileAccess.Read));
            bigReader.ReadBytes(4);
            bigReader.ReadUintLittleEndian();
            uint _entryCount = bigReader.ReadUintBigEndian();
            bigReader.ReadUintBigEndian();

            for (uint i = 0; i < _entryCount ; i++)
            {
                bigReader.ReadUintLittleEndian();
                bigReader.ReadUintLittleEndian();
                string name = bigReader.ReadString(Encoding.ASCII.GetChars(new byte[] { 0 })[0]);
                namesList.Add(name);
                if (name.ToLower().Contains("gamedata.ini"))
                {
                    bigReader.Close();
                    bigReader.Dispose();
                    return true;
                }

            }
            bigReader.Close();
            bigReader.Dispose();

            return false;
        }

        public static void SetCameraHeight(string bigName, int height)
        {
            BigReader bigReader = new BigReader(new FileStream(bigName, FileMode.Open, FileAccess.Read));
            bigReader.ReadBytes(4);
            bigReader.ReadUintLittleEndian();
            uint _entryCount = bigReader.ReadUintBigEndian();
            bigReader.ReadUintBigEndian();
            for (uint i = 0; i < _entryCount; i++)
            {
                uint offset = bigReader.ReadUintBigEndian();
                uint length = bigReader.ReadUintBigEndian();
                string name = bigReader.ReadString(Encoding.ASCII.GetChars(new byte[] { 0 })[0]);
                if (String.Equals(name, @"Data\INI\GameData.ini", StringComparison.OrdinalIgnoreCase))
                {
                    bigReader.BaseStream.Position = offset;
                    {
                        StringBuilder sb = new StringBuilder((int)length);
                        for (int k = 0; k < length; k++)
                            sb.Append(bigReader.ReadChar());
                        string text = sb.ToString();
                        int pos = text.IndexOf("MaxCameraHeight", 3000) + 14;
                        pos = text.IndexOf('=', pos) + 1;

                        bigReader.Close();
                        bigReader.Dispose();
                        pos += (int)offset;

                        string sHeight = height.ToString() + ".00";
                        using (FileStream fs = new FileStream(bigName, FileMode.Open, FileAccess.Write))
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            bw.BaseStream.Position = pos;
                            for (int k = 0; k < sHeight.Length; k++)
                                bw.Write(sHeight[k]);
                        }
                    }
                    break;
                }
            }
        }
    }
}
