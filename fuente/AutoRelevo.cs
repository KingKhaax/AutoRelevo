using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[StructLayout(LayoutKind.Sequential)]
struct PropertyKey { public Guid fmtid; public uint pid; }

[StructLayout(LayoutKind.Sequential)]
struct PropVariant { public ushort vt; public ushort r1; public ushort r2; public ushort r3; public IntPtr p; public IntPtr p2; }

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator
{
    [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
    [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
    [PreserveSig] int RegisterEndpointNotificationCallback(IMMNotificationClient client);
    [PreserveSig] int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int Item(uint index, out IMMDevice device);
}

[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice
{
    [PreserveSig] int Activate(ref Guid iid, int context, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object result);
    [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore store);
    [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    [PreserveSig] int GetState(out int state);
}

[ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int GetAt(uint index, out PropertyKey key);
    [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
    [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
    [PreserveSig] int Commit();
}

[ComImport, Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMNotificationClient
{
    [PreserveSig] int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string id, int state);
    [PreserveSig] int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string id);
    [PreserveSig] int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string id);
    [PreserveSig] int OnDefaultDeviceChanged(int flow, int role, [MarshalAs(UnmanagedType.LPWStr)] string id);
    [PreserveSig] int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string id, PropertyKey key);
}

[ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPolicyConfig
{
    [PreserveSig] int GetMixFormat();
    [PreserveSig] int GetDeviceFormat();
    [PreserveSig] int ResetDeviceFormat();
    [PreserveSig] int SetDeviceFormat();
    [PreserveSig] int GetProcessingPeriod();
    [PreserveSig] int SetProcessingPeriod();
    [PreserveSig] int GetShareMode();
    [PreserveSig] int SetShareMode();
    [PreserveSig] int GetPropertyValue();
    [PreserveSig] int SetPropertyValue();
    [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);
    [PreserveSig] int SetEndpointVisibility();
}

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
class MMDeviceEnumeratorCom { }

[ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
class PolicyConfigClient { }

class Dev
{
    public string Id;
    public string Name;
    public int State;

    public override string ToString()
    {
        return State == 1 ? Name : Name + "  [" + Audio.StateText(State) + "]";
    }
}

static class Audio
{
    [DllImport("ole32.dll")]
    static extern int PropVariantClear(ref PropVariant value);

    static IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();

    public static IMMDeviceEnumerator Enumerator { get { return enumerator; } }

    public static string StateText(int state)
    {
        if (state == 1) return "activo";
        if (state == 2) return "desactivado";
        if (state == 4) return "no conectado";
        if (state == 8) return "no presente";
        return "desconocido";
    }

    static string NameOf(IMMDevice device)
    {
        IPropertyStore store;
        if (device.OpenPropertyStore(0, out store) != 0) return "?";
        PropertyKey key = new PropertyKey();
        key.fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");
        key.pid = 14;
        PropVariant value;
        if (store.GetValue(ref key, out value) != 0) return "?";
        string name = value.vt == 31 ? Marshal.PtrToStringUni(value.p) : "?";
        PropVariantClear(ref value);
        return name;
    }

    static int Rank(int state)
    {
        if (state == 1) return 0;
        if (state == 2) return 1;
        if (state == 4) return 2;
        return 3;
    }

    public static List<Dev> List()
    {
        List<Dev> result = new List<Dev>();
        IMMDeviceCollection collection;
        if (enumerator.EnumAudioEndpoints(0, 15, out collection) != 0) return result;
        uint count;
        collection.GetCount(out count);
        for (uint i = 0; i < count; i++)
        {
            IMMDevice device;
            if (collection.Item(i, out device) != 0) continue;
            Dev dev = new Dev();
            device.GetId(out dev.Id);
            device.GetState(out dev.State);
            dev.Name = NameOf(device);
            if (dev.Name != "?") result.Add(dev);
        }
        result.Sort(delegate (Dev a, Dev b)
        {
            int order = Rank(a.State).CompareTo(Rank(b.State));
            return order != 0 ? order : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
        });
        return result;
    }

    public static int StateOf(string id)
    {
        IMMDevice device;
        if (enumerator.GetDevice(id, out device) != 0) return 0;
        int state;
        if (device.GetState(out state) != 0) return 0;
        return state;
    }

    public static string NameById(string id)
    {
        IMMDevice device;
        if (id == null || enumerator.GetDevice(id, out device) != 0) return "?";
        return NameOf(device);
    }

    public static string DefaultId()
    {
        IMMDevice device;
        if (enumerator.GetDefaultAudioEndpoint(0, 0, out device) != 0) return null;
        string id;
        device.GetId(out id);
        return id;
    }

    public static void SetDefault(string id)
    {
        IPolicyConfig policy = (IPolicyConfig)new PolicyConfigClient();
        for (int role = 0; role < 3; role++)
        {
            int hr = policy.SetDefaultEndpoint(id, role);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }
    }
}

class Notifier : IMMNotificationClient
{
    public Action OnChange;
    public Action OnDefault;

    public int OnDeviceStateChanged(string id, int state) { if (OnChange != null) OnChange(); return 0; }
    public int OnDeviceAdded(string id) { if (OnChange != null) OnChange(); return 0; }
    public int OnDeviceRemoved(string id) { if (OnChange != null) OnChange(); return 0; }
    public int OnDefaultDeviceChanged(int flow, int role, string id) { if (OnDefault != null) OnDefault(); return 0; }
    public int OnPropertyValueChanged(string id, PropertyKey key) { return 0; }
}

class MainForm : Form
{
    ComboBox primary = new ComboBox();
    ComboBox secondary = new ComboBox();
    Button toggle = new Button();
    Label status = new Label();
    System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer();
    Notifier notifier = new Notifier();
    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
    bool on;
    string lastError = "";
    static readonly Color Bg = Color.FromArgb(245, 249, 250);
    static readonly Color Navy = Color.FromArgb(16, 31, 58);
    static readonly Color Blue = Color.FromArgb(47, 134, 182);
    static readonly Color Orange = Color.FromArgb(227, 124, 58);

    public MainForm()
    {
        Text = "AutoRelevo";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(480, 400);
        BackColor = Bg;
        Icon = new System.Drawing.Icon(typeof(MainForm).Assembly.GetManifestResourceStream("icon.ico"), SystemInformation.SmallIconSize);

        PictureBox logo = new PictureBox();
        logo.Image = Image.FromStream(typeof(MainForm).Assembly.GetManifestResourceStream("logo.png"));
        logo.SizeMode = PictureBoxSizeMode.Zoom;
        logo.SetBounds(140, 8, 200, 120);
        Controls.Add(logo);

        Label l1 = new Label();
        l1.Text = "Dispositivo de salida PRINCIPAL:";
        l1.ForeColor = Navy;
        l1.SetBounds(20, 140, 440, 20);
        Controls.Add(l1);

        primary.DropDownStyle = ComboBoxStyle.DropDownList;
        primary.BackColor = Color.White;
        primary.ForeColor = Navy;
        primary.SetBounds(20, 163, 440, 24);
        Controls.Add(primary);

        Label l2 = new Label();
        l2.Text = "Dispositivo de salida SECUNDARIO (solo cuando el principal este apagado o desactivado):";
        l2.ForeColor = Navy;
        l2.SetBounds(20, 203, 440, 34);
        Controls.Add(l2);

        secondary.DropDownStyle = ComboBoxStyle.DropDownList;
        secondary.BackColor = Color.White;
        secondary.ForeColor = Navy;
        secondary.SetBounds(20, 240, 440, 24);
        Controls.Add(secondary);

        toggle.SetBounds(20, 285, 440, 48);
        toggle.Font = new Font(Font.FontFamily, 16, FontStyle.Bold);
        toggle.FlatStyle = FlatStyle.Flat;
        toggle.FlatAppearance.BorderSize = 0;
        toggle.Cursor = Cursors.Hand;
        Controls.Add(toggle);

        status.ForeColor = Navy;
        status.SetBounds(20, 343, 440, 48);
        Controls.Add(status);

        debounce.Interval = 600;
        debounce.Tick += delegate { debounce.Stop(); Evaluate(); };

        primary.DropDown += delegate { Reload(primary); };
        secondary.DropDown += delegate { Reload(secondary); };
        primary.SelectionChangeCommitted += delegate { Changed(); };
        secondary.SelectionChangeCommitted += delegate { Changed(); };
        toggle.Click += delegate { Toggle(); };

        notifier.OnChange = delegate { Post(delegate { debounce.Stop(); debounce.Start(); }); };
        notifier.OnDefault = delegate { Post(RefreshStatus); };
    }

    void Post(Action action)
    {
        if (IsHandleCreated && !IsDisposed) BeginInvoke(action);
    }

    string Selected(ComboBox box)
    {
        Dev dev = box.SelectedItem as Dev;
        return dev == null ? null : dev.Id;
    }

    void Reload(ComboBox box)
    {
        string current = Selected(box);
        List<Dev> devices = Audio.List();
        box.Items.Clear();
        foreach (Dev dev in devices)
        {
            int index = box.Items.Add(dev);
            if (dev.Id == current) box.SelectedIndex = index;
        }
        if (current != null && Selected(box) == null) KeepMissing(box, current);
    }

    void KeepMissing(ComboBox box, string id)
    {
        Dev missing = new Dev();
        missing.Id = id;
        missing.Name = "Dispositivo guardado (no encontrado)";
        missing.State = 8;
        box.SelectedIndex = box.Items.Add(missing);
    }

    void Select(ComboBox box, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        foreach (object item in box.Items)
        {
            if (((Dev)item).Id == id) box.SelectedItem = item;
        }
        if (Selected(box) == null) KeepMissing(box, id);
    }

    void LoadConfig()
    {
        string idPrimary = null, idSecondary = null;
        if (File.Exists(configPath))
        {
            foreach (string line in File.ReadAllLines(configPath))
            {
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key == "principal") idPrimary = value;
                else if (key == "secundario") idSecondary = value;
                else if (key == "on") on = value == "1";
            }
        }
        Reload(primary);
        Reload(secondary);
        Select(primary, idPrimary);
        Select(secondary, idSecondary);
        if (Selected(primary) == null || Selected(secondary) == null) on = false;
    }

    void SaveConfig()
    {
        File.WriteAllLines(configPath, new string[] {
            "principal=" + Selected(primary),
            "secundario=" + Selected(secondary),
            "on=" + (on ? "1" : "0")
        });
    }

    void Changed()
    {
        SaveConfig();
        Evaluate();
    }

    void Toggle()
    {
        if (!on)
        {
            string p = Selected(primary), s = Selected(secondary);
            if (p == null || s == null)
            {
                MessageBox.Show("Elige el dispositivo principal y el secundario.", "AutoRelevo");
                return;
            }
            if (p == s)
            {
                MessageBox.Show("El principal y el secundario no pueden ser el mismo.", "AutoRelevo");
                return;
            }
        }
        on = !on;
        SaveConfig();
        Evaluate();
    }

    void Evaluate()
    {
        lastError = "";
        if (on)
        {
            string p = Selected(primary), s = Selected(secondary);
            string target = null;
            if (p != null && Audio.StateOf(p) == 1) target = p;
            else if (s != null && Audio.StateOf(s) == 1) target = s;
            try
            {
                if (target != null && Audio.DefaultId() != target) Audio.SetDefault(target);
            }
            catch (Exception ex)
            {
                lastError = "Error al cambiar la salida: " + ex.Message;
            }
        }
        RefreshStatus();
    }

    void RefreshStatus()
    {
        toggle.Text = on ? "ON" : "OFF";
        toggle.BackColor = on ? Blue : Orange;
        toggle.ForeColor = Color.White;
        string text = "Salida actual: " + Audio.NameById(Audio.DefaultId());
        if (!on) text += "\nDesactivado: no cambia ninguna salida.";
        if (lastError.Length > 0) text += "\n" + lastError;
        status.Text = text;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        LoadConfig();
        Audio.Enumerator.RegisterEndpointNotificationCallback(notifier);
        Evaluate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Audio.Enumerator.UnregisterEndpointNotificationCallback(notifier);
        SaveConfig();
        base.OnFormClosing(e);
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        bool created;
        using (Mutex mutex = new Mutex(true, "AutoRelevo_portable", out created))
        {
            if (!created) return;
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }
}

