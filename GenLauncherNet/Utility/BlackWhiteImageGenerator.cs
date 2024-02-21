using System;
using System.Drawing;

namespace GenLauncherNet
{
    public static class BlackWhiteImageGenerator
    {
        public static Bitmap GenerateBlackWhiteBitMapImageFromPath(string path)
        {
            try
            {
                var input = new Bitmap(path);

                var output = new Bitmap(input.Width, input.Height);

                for (int j = 0; j < input.Height; j++)
                    for (int i = 0; i < input.Width; i++)
                    {
                        UInt32 pixel = (UInt32)(input.GetPixel(i, j).ToArgb());
                        float R = (float)((pixel & 0x00FF0000) >> 16);
                        float G = (float)((pixel & 0x0000FF00) >> 8);
                        float B = (float)(pixel & 0x000000FF);

                        R = G = B = (R + G + B) / 3.0f;

                        UInt32 newPixel = 0xFF000000 | ((UInt32)R << 16) | ((UInt32)G << 8) | ((UInt32)B);

                        output.SetPixel(i, j, System.Drawing.Color.FromArgb((int)newPixel));
                    }
                return output;
            }
            catch
            {
                //TODO logger
                return null;
            }
        }
    }
}
