using System.Windows.Controls;

namespace GenLauncherNet
{
    public class GridControls
    {
        public NetworkInfoButton _NetworkInfo { get; }
        public UpdateButton _UpdateButton { get; }
        public UpdateButton _SupportButton { get; }
        public ProgressBar _ProgressBar { get; }
        public InfoTextBlock _InfoTextBlock { get; }
        public VersionTextBox _VersionTextBlock { get; }
        public ComboBox _ComboBox { get; }
        public Image _Image { get; set; }
        public NameTextBox _Name { get; set; }
        public Border _ImageBorder { get; set; }
        public ChangeLogButton _ChangeLogButton { get; set; }
        public System.Windows.Shapes.Rectangle _DragAndDropRectangle { get; set; }
        public System.Windows.Shapes.Rectangle _UpdateRectangle { get; set; }        

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
            }

            _DragAndDropRectangle = controlGrid.FindName("DragAndDropBackground") as System.Windows.Shapes.Rectangle;
            _UpdateRectangle = controlGrid.FindName("UpdateRectangle") as System.Windows.Shapes.Rectangle;
            _UpdateButton = controlGrid.FindName("Update") as UpdateButton;
            _SupportButton = controlGrid.FindName("Support") as UpdateButton;
        }
    }
}
