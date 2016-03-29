using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;

namespace MOE
{
    static class Loader
    {
        public static void Button(Button button, string waitText)
        {
            if (((string)button.Content).Length < waitText.Length + 3)
                button.Content += ".";
            else
                button.Content = waitText;

            DispatcherFrame frame = new DispatcherFrame(true);
            Dispatcher.CurrentDispatcher.BeginInvoke
            (
            DispatcherPriority.Background,
            (SendOrPostCallback)delegate(object arg)
            {
                var f = arg as DispatcherFrame;
                f.Continue = false;
            },
            frame
            );
            Dispatcher.PushFrame(frame);

            Thread.Sleep(500);
        }

        public static void Label(Label label)
        {
            string labelText = (string)label.Content;

            StringBuilder tmp = new StringBuilder(labelText);
            if (labelText.IndexOf('.') < labelText.IndexOf(' '))
            {
                tmp.Replace(' ', '.', labelText.IndexOf(' '), 1);
            }
            else
            {
                tmp.Replace('.', ' ', labelText.IndexOf('.'), 1);
            }
            label.Content = tmp.ToString();


            DispatcherFrame frame = new DispatcherFrame(true);
            Dispatcher.CurrentDispatcher.BeginInvoke
            (
            DispatcherPriority.Background,
            (SendOrPostCallback)delegate(object arg)
            {
                var f = arg as DispatcherFrame;
                f.Continue = false;
            },
            frame
            );
            Dispatcher.PushFrame(frame);

            Thread.Sleep(150);
        }

        public static void WaitLabel(Label label, List<string> symbols)
        {
            int index = symbols.IndexOf((string)label.Content) + 1;
            if (index >= symbols.Count)
                index = 0;
            label.Content = symbols[index];


            DispatcherFrame frame = new DispatcherFrame(true);
            Dispatcher.CurrentDispatcher.BeginInvoke
            (
            DispatcherPriority.Background,
            (SendOrPostCallback)delegate(object arg)
            {
                var f = arg as DispatcherFrame;
                f.Continue = false;
            },
            frame
            );
            Dispatcher.PushFrame(frame);

            Thread.Sleep(150);
        }
    }

}
