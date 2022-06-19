using GenLauncherNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GenLauncherNet
{
    public class GridControls
    {
        public NetworkInfoButton _NetworkInfo { get; }
        public UpdateButton _UpdateButton { get; }
        public ProgressBar _ProgressBar { get; }
        public InfoTextBlock _InfoTextBlock { get; }
        public VersionTextBox _VersionTextBlock { get; }
        public ComboBox _ComboBox { get; }
        public Image _Image { get; set; }
        public NameTextBox _Name { get; set; }
        public Border _ImageBorder { get; set; }
        public ChangeLogButton _ChangeLogButton { get; set; }
        public RadioButton _MyFavorite { get; set; }
        public System.Windows.Shapes.Rectangle _FavoriteRectangle { get; set; }

        public GridControls(Grid controlGrid)
        {
            foreach (var children in controlGrid.Children)
            {
                if (children is ComboBox)
                    _ComboBox = children as ComboBox;
                if (children is ProgressBar)
                    _ProgressBar = children as ProgressBar;
                if (children is InfoTextBlock)
                    _InfoTextBlock = children as InfoTextBlock;
                if (children is NetworkInfoButton)
                    _NetworkInfo = children as NetworkInfoButton;
                if (children is UpdateButton)
                    _UpdateButton = children as UpdateButton;
                if (children is VersionTextBox)
                    _VersionTextBlock = children as VersionTextBox;
                if (children is NameTextBox)
                    _Name = children as NameTextBox;
                if (children is Border)
                    _ImageBorder = children as Border;
                if (children is Image)
                    _Image = children as Image;
                if (children is ChangeLogButton)
                    _ChangeLogButton = children as ChangeLogButton;
                if (children is RadioButton)
                    _MyFavorite = children as RadioButton;

                if (children is System.Windows.Shapes.Rectangle)
                    _FavoriteRectangle = children as System.Windows.Shapes.Rectangle;
            }
        }
    }
}
