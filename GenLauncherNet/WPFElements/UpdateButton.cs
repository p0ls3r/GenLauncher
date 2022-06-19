using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GenLauncherNet
{
    public class UpdateButton : Button
    {
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            //Shitiest shit ever
            //Selecting unselected listbox element when install/update button is pressed

            var grid = (Grid)((UpdateButton)e.Source).Parent;
            var visualElement = VisualTreeHelper.GetParent(grid);

            while (!(visualElement is ListBoxItem))
            {
                visualElement = VisualTreeHelper.GetParent(visualElement);
            }

            var listboxItem = visualElement as ListBoxItem;

            if (listboxItem!= null && !listboxItem.IsSelected)
            {
                IsHitTestVisible = false;

                Action action = () =>
                {
                    MouseSimulator.ClickLeftMouseButton();
                    IsHitTestVisible = true;
                };
                this.Dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
            }
        }

        public class MouseSimulator
        {
            [DllImport("user32.dll", SetLastError = true)]
            static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

            [StructLayout(LayoutKind.Sequential)]
            struct INPUT
            {
                public SendInputEventType type;
                public MouseKeybdhardwareInputUnion mkhi;
            }
            [StructLayout(LayoutKind.Explicit)]
            struct MouseKeybdhardwareInputUnion
            {
                [FieldOffset(0)]
                public MouseInputData mi;

                [FieldOffset(0)]
                public KEYBDINPUT ki;

                [FieldOffset(0)]
                public HARDWAREINPUT hi;
            }
            [StructLayout(LayoutKind.Sequential)]
            struct KEYBDINPUT
            {
                public ushort wVk;
                public ushort wScan;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }
            [StructLayout(LayoutKind.Sequential)]
            struct HARDWAREINPUT
            {
                public int uMsg;
                public short wParamL;
                public short wParamH;
            }
            struct MouseInputData
            {
                public int dx;
                public int dy;
                public uint mouseData;
                public MouseEventFlags dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }
            [Flags]
            enum MouseEventFlags : uint
            {
                MOUSEEVENTF_MOVE = 0x0001,
                MOUSEEVENTF_LEFTDOWN = 0x0002,
                MOUSEEVENTF_LEFTUP = 0x0004,
                MOUSEEVENTF_RIGHTDOWN = 0x0008,
                MOUSEEVENTF_RIGHTUP = 0x0010,
                MOUSEEVENTF_MIDDLEDOWN = 0x0020,
                MOUSEEVENTF_MIDDLEUP = 0x0040,
                MOUSEEVENTF_XDOWN = 0x0080,
                MOUSEEVENTF_XUP = 0x0100,
                MOUSEEVENTF_WHEEL = 0x0800,
                MOUSEEVENTF_VIRTUALDESK = 0x4000,
                MOUSEEVENTF_ABSOLUTE = 0x8000
            }
            enum SendInputEventType : int
            {
                InputMouse,
                InputKeyboard,
                InputHardware
            }

            public static void ClickLeftMouseButton()
            {
                INPUT mouseDownInput = new INPUT();
                mouseDownInput.type = SendInputEventType.InputMouse;
                mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
                SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

                INPUT mouseUpInput = new INPUT();
                mouseUpInput.type = SendInputEventType.InputMouse;
                mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
                SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
            }
        }
    }
}
