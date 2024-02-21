using System.Windows.Media;

namespace GenLauncherNet
{
    public class ColorsInfo
    {
        public SolidColorBrush GenLauncherBorderColor;
        public SolidColorBrush GenLauncherInactiveBorder;
        public SolidColorBrush GenLauncherInactiveBorder2;
        public SolidColorBrush GenLauncherActiveColor;
        public SolidColorBrush GenLauncherDarkFillColor;
        public SolidColorBrush GenLauncherDarkBackGround;
        public SolidColorBrush GenLauncherLightBackGround;
        public SolidColorBrush GenLauncherDefaultTextColor;
        public SolidColorBrush GenLauncherDownloadTextColor;

        public Color GenLauncherListBoxSelectionColor2;
        public Color GenLauncherListBoxSelectionColor1;
        public Color GenLauncherButtonSelectionColor;

        public ImageBrush GenLauncherBackgroundImage;

        public ColorsInfo()
        {

        }

        public ColorsInfo(ColorsInfoString colorsInfoString)
        {
            GenLauncherBorderColor = GetColorBrushFromString(colorsInfoString.GenLauncherBorderColor);
            GenLauncherInactiveBorder = GetColorBrushFromString(colorsInfoString.GenLauncherInactiveBorder);
            GenLauncherInactiveBorder2 = GetColorBrushFromString(colorsInfoString.GenLauncherInactiveBorder2);
            GenLauncherActiveColor = GetColorBrushFromString(colorsInfoString.GenLauncherActiveColor);
            GenLauncherDarkFillColor = GetColorBrushFromString(colorsInfoString.GenLauncherDarkFillColor);
            GenLauncherDarkBackGround = GetColorBrushFromString(colorsInfoString.GenLauncherDarkBackGround);
            GenLauncherLightBackGround = GetColorBrushFromString(colorsInfoString.GenLauncherLightBackGround);
            GenLauncherDefaultTextColor = GetColorBrushFromString(colorsInfoString.GenLauncherDefaultTextColor);
            GenLauncherDownloadTextColor = GetColorBrushFromString(colorsInfoString.GenLauncherDownloadTextColor);

            GenLauncherListBoxSelectionColor2 = GetColorFromString(colorsInfoString.GenLauncherListBoxSelectionColor1);
            GenLauncherListBoxSelectionColor1 = GetColorFromString(colorsInfoString.GenLauncherListBoxSelectionColor2);
            GenLauncherButtonSelectionColor = GetColorFromString(colorsInfoString.GenLauncherButtonSelectionColor);
        }

        public ColorsInfo(string border, string inactiveBorder, string inactiveBorder2, string activeColor, string darkFill, string darkBackground, string lightBackground, string text, string text2, string sColor2, string sColor1, string bColor)
        {
            GenLauncherBorderColor = GetColorBrushFromString(border);
            GenLauncherInactiveBorder = GetColorBrushFromString(inactiveBorder);
            GenLauncherInactiveBorder2 = GetColorBrushFromString(inactiveBorder2);
            GenLauncherActiveColor = GetColorBrushFromString(activeColor);
            GenLauncherDarkFillColor = GetColorBrushFromString(darkFill);
            GenLauncherDarkBackGround = GetColorBrushFromString(darkBackground);
            GenLauncherLightBackGround = GetColorBrushFromString(lightBackground);
            GenLauncherDefaultTextColor = GetColorBrushFromString(text);
            GenLauncherDownloadTextColor = GetColorBrushFromString(text2);

            GenLauncherListBoxSelectionColor2 = GetColorFromString(sColor2);
            GenLauncherListBoxSelectionColor1 = GetColorFromString(sColor1);
            GenLauncherButtonSelectionColor = GetColorFromString(bColor);
        }

        private SolidColorBrush GetColorBrushFromString(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        }

        private Color GetColorFromString(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }

    public class ColorsInfoString
    {
        public string GenLauncherBorderColor;
        public string GenLauncherInactiveBorder;
        public string GenLauncherInactiveBorder2;
        public string GenLauncherActiveColor;
        public string GenLauncherDarkFillColor;
        public string GenLauncherDarkBackGround;
        public string GenLauncherLightBackGround;
        public string GenLauncherDefaultTextColor;
        public string GenLauncherDownloadTextColor;

        public string GenLauncherListBoxSelectionColor2;
        public string GenLauncherListBoxSelectionColor1;
        public string GenLauncherButtonSelectionColor;

        public string GenLauncherBackgroundImageLink;

        public ColorsInfoString()
        {

        }

        public ColorsInfoString(string border, string inactiveBorder, string inactiveBorder2, string activeColor, string darkFill, string darkBackground, string lightBackground, string text, string text2, string sColor2, string sColor1, string bColor)
        {
            GenLauncherBorderColor = border;
            GenLauncherInactiveBorder = inactiveBorder;
            GenLauncherInactiveBorder2 = inactiveBorder2;
            GenLauncherActiveColor = activeColor;
            GenLauncherDarkFillColor = darkFill;
            GenLauncherDarkBackGround = darkBackground;
            GenLauncherLightBackGround = lightBackground;
            GenLauncherDefaultTextColor = text;
            GenLauncherDownloadTextColor = text2;

            GenLauncherListBoxSelectionColor2 = sColor2;
            GenLauncherListBoxSelectionColor1 = sColor1;
            GenLauncherButtonSelectionColor = bColor;
        }
    }
}
