using System.Text;
using System.IO.Ports;

namespace CommHelper;
class Program {
    SerialPort _comm;
    static void Main(string[] args) {
        Gtk.Application.Init();
        new Program();
        Gtk.Application.Run();
    }
    public Program() {
        var win = new Gtk.Window("GtkCommHelper");
        win.SetDefaultSize(700, 500);
        //窗体关闭后退出应用
        win.DeleteEvent += (s, e) => {
            if (_comm != null) {
                _comm.Close();
            }
            Gtk.Application.Quit();
        };
        win.WindowPosition = Gtk.WindowPosition.Center;
        win.BorderWidth = 20;
        win.Resizable = true;

        Gtk.HBox hBoxConfig = new Gtk.HBox();
        Gtk.ComboBox comboBox = new Gtk.ComboBox(SerialPort.GetPortNames());
        comboBox.Active = 0;
        hBoxConfig.PackStart(comboBox, false, false, 0);

        Gtk.Button refreshComboBox = new Gtk.Button(new Gtk.Image(Gtk.Stock.Refresh, Gtk.IconSize.Button));
        refreshComboBox.Clicked += (s, e) => {
            var newList = new Gtk.ListStore(typeof(string));
            comboBox.Model = newList;
            foreach (var port in SerialPort.GetPortNames()) {
                newList.AppendValues(port);
            }
            comboBox.Active = 0;
        };
        hBoxConfig.PackStart(refreshComboBox, false, false, 5);

        hBoxConfig.PackStart(new Gtk.Label("波特率:"), false, false, 10);
        Gtk.Entry entryBaudrate = new Gtk.Entry("115200");
        entryBaudrate.WidthChars = 8;
        hBoxConfig.PackStart(entryBaudrate, false, false, 0);

        Gtk.HBox hBox = new Gtk.HBox(true, 70);

        Gtk.Button buttonClear = new Gtk.Button("Clear");
        Gtk.Button buttonSend = new Gtk.Button("Send");

        hBox.Add(buttonClear);
        hBox.Add(buttonSend);

        Gtk.VBox vBox = new Gtk.VBox();

        Gtk.ScrolledWindow scrolledWindow = new Gtk.ScrolledWindow();
        Gtk.TextView textView = new Gtk.TextView();
        textView.WrapMode = Gtk.WrapMode.Word;
        textView.Editable = false;
        var endMark = textView.Buffer.CreateMark("endMark", textView.Buffer.EndIter, false);
        scrolledWindow.Add(textView);

        Gtk.Entry entry = new Gtk.Entry();
        entry.Activated += (s, e) => {
            buttonSend.Click();
        };

        StringBuilder stringBuilder = new StringBuilder();
        buttonClear.Clicked += (s, e) => {
            stringBuilder.Clear();
            textView.Buffer.Text = stringBuilder.ToString();
        };
        GLib.Timeout.Add(500, new GLib.TimeoutHandler(() => {
            if (_comm != null && _comm.IsOpen) {
                stringBuilder.Append(_comm.ReadExisting());
                if (textView.Buffer.Text != stringBuilder.ToString()) {
                    textView.Buffer.Text = stringBuilder.ToString();
                    textView.ScrollToMark(endMark, 0, false, 0, 0);
                    //textView.ScrollToIter(textView.Buffer.EndIter, 0, false, 0, 0);
                }
            }
            return true;
        }));
        buttonSend.Clicked += (s, e) => {
            Gtk.TreeIter treeIter;
            comboBox.GetActiveIter(out treeIter);

            if (comboBox.Model.GetValue(treeIter, 0)==null) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(win,
                Gtk.DialogFlags.DestroyWithParent, Gtk.MessageType.Warning,
                Gtk.ButtonsType.Close, "No uart port detected!");
                md.Run();
                md.Destroy();
                return;
            }

            int baudrate = 115200;
            int.TryParse(entryBaudrate.Text, out baudrate);
            
            if (_comm == null) {
                _comm = new SerialPort((string)comboBox.Model.GetValue(treeIter, 0), baudrate);
            }
            else if (_comm.PortName != (string)comboBox.Model.GetValue(treeIter, 0)) {
                _comm.Close();
                _comm = new SerialPort((string)comboBox.Model.GetValue(treeIter, 0), baudrate);
            }
            else if (_comm.BaudRate != baudrate) {
                _comm.BaudRate = baudrate;
            }
            try {
                if (!_comm.IsOpen) {
                    _comm.Open();
                }
                _comm.WriteLine(entry.Text);
            }
            catch (Exception ex) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(win,
                Gtk.DialogFlags.DestroyWithParent, Gtk.MessageType.Warning,
                Gtk.ButtonsType.Close, ex.Message);
                md.Run();
                md.Destroy();
            }
            entry.Text = "";
        };

        vBox.PackStart(hBoxConfig, false, false, 0);

        var framReturn = new Gtk.Frame("返回：") {
                scrolledWindow
            };
        scrolledWindow.Margin = 5;
        vBox.Add(framReturn);

        var framInput = new Gtk.Frame("输入：") {
                entry
            };
        entry.Margin = 5;
        vBox.PackStart(framInput, false, false, 5);
        vBox.PackStart(hBox, false, false, 30);

        win.Add(vBox);

        win.ShowAll();
    }
}