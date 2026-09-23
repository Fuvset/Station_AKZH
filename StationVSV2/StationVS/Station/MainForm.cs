using System.IO.Ports;

namespace StationApp;

// =====================================================================
//  ГЛАВНОЕ ОКНО
// =====================================================================

class MainForm : Form
{
    static readonly Color Bg = Color.FromArgb(143, 191, 186);
    static readonly Color Pink = Color.FromArgb(219, 0, 105);
    static readonly Color BtnLight = Color.FromArgb(238, 238, 238);
    static readonly Color BtnOn = Color.FromArgb(120, 220, 140);
    static readonly Color ModeOn = Color.FromArgb(52, 152, 219);
    static readonly Color ModeOff = Color.FromArgb(128, 128, 128);

    readonly Station st = new();
    readonly SchemePanel scheme;

    SerialPort port;
    bool updatingPorts;
    bool shuntMode = true;     // категория маршрутов: маневровые / поездные
    bool engineering = true;   // режим схемы: инженерный / простой
    bool updatingCombo;

    Label lblArduino;
    ComboBox cmbPort;
    Button btnTrain, btnShunt, btnSimpleView, btnEngView, btnSetRoute;
    ComboBox cmbFrom, cmbTo;
    FlowLayoutPanel switchFlow;
    ListBox lstRoutes, lstLog;
    Label lblActive;

    readonly Dictionary<int, Button> swBtns = new();

    public MainForm()
    {
        Text = "Станция";
        BackColor = Bg;
        Font = new Font("Bahnschrift", 10f);
        MinimumSize = new Size(1050, 760);
        Size = new Size(1500, 950);
        WindowState = FormWindowState.Maximized;

        scheme = new SchemePanel(st) { Dock = DockStyle.Fill, Engineering = engineering };
        BuildUi();

        st.Changed += RefreshUi;
        st.Log += AddLog;
        st.SwitchMoved += (n, plus) => Send($"SW{n}{(plus ? '+' : '-')}");
        st.SignalChanged += (idx, open) => Send($"SG{idx}:{(open ? 1 : 0)}");

        //RebuildFromList();
        RefreshPorts();
        SetStatus(false, null);
        RefreshUi();
        AddLog("Программа запущена");
    }

    // ---------------- построение интерфейса ----------------
    void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Bg };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
        Controls.Add(root);

        // ----- верхняя панель -----
        var top = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
        root.Controls.Add(top, 0, 0);

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 10, 0, 0)
        };
        lblArduino = new Label
        {
            AutoSize = true,
            BackColor = Color.White,
            ForeColor = Color.Red,
            Font = new Font("Bahnschrift", 10f, FontStyle.Bold),
            Padding = new Padding(3),
            Margin = new Padding(3, 3, 12, 3)
        };
        cmbPort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Margin = new Padding(3, 4, 12, 3) };
        cmbPort.DropDown += (_, _) => RefreshPorts();
        cmbPort.SelectedIndexChanged += (_, _) =>
        {
            if (!updatingPorts) Connect(cmbPort.SelectedItem?.ToString());
        };
        left.Controls.Add(lblArduino);
        left.Controls.Add(cmbPort);

        // переключатель "Простой / Инженерный"
        btnSimpleView = SmallModeButton("Простой", () => SetView(false));
        btnEngView = SmallModeButton("Инженерный", () => SetView(true));
        left.Controls.Add(btnSimpleView);
        left.Controls.Add(btnEngView);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 12, 10, 0)
        };
        right.Controls.Add(PinkButton("Отменить маршрут", () => st.CancelLast()));
        right.Controls.Add(PinkButton("Убрать всё", () => st.ClearAll()));
        right.Controls.Add(PinkButton("Проверка", RunTest));
        right.Controls.Add(PinkButton("Выключить", () => Close()));

        top.Controls.Add(right);
        top.Controls.Add(left);

        // ----- схема -----
        root.Controls.Add(scheme, 0, 1);

        // ----- нижняя часть -----
        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Bg };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 540));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(bottom, 0, 2);
        


        // колонка 1: поездные и маневринные
        var routesCell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Bg };

        var modes = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Bg,
            Padding = new Padding(0),
            Margin = new Padding(0, 70, 0, 0)
        };

        btnTrain = ModeButton("ПОЕЗДНЫЕ", () => SetCategory(false));
        btnShunt = ModeButton("МАНЕВРОВЫЕ", () => SetCategory(true));

        btnTrain.Anchor = AnchorStyles.Left;
        btnShunt.Anchor = AnchorStyles.Left;

        modes.Controls.Add(btnTrain, 0, 0);
        modes.Controls.Add(btnShunt, 0, 1);

        routesCell.Controls.Add(modes, 0, 0);

        bottom.Controls.Add(routesCell, 0, 0);


        bottom.Controls.Add(routesCell, 0, 0);

        // колонка 2: стрелки вручную
        var swCell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Bg };
        swCell.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        swCell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        swCell.Controls.Add(new Label
        {
            Text = "Стрелки (вручную)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Bahnschrift", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 55, 55)
        }, 0, 0);
        switchFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Bg
        };
        foreach (var n in st.Switches.Keys.OrderBy(k => k))
        {
            int num = n;
            var b = new Button
            {
                Size = new Size(92, 30),
                Margin = new Padding(2),
                FlatStyle = FlatStyle.Flat,
                BackColor = BtnLight,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => st.ToggleSwitch(num);
            swBtns[num] = b;
            switchFlow.Controls.Add(b);
        }
        swCell.Controls.Add(switchFlow, 0, 1);
        bottom.Controls.Add(swCell, 1, 0);

        // колонка 3: активные маршруты + журнал
        var logCell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Bg };
        logCell.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        logCell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var activePanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Bg };
        lblActive = new Label
        {
            AutoSize = true,
            Location = new Point(6, 4),
            ForeColor = Color.FromArgb(30, 45, 45),
            Font = new Font("Bahnschrift", 11f)
        };
        activePanel.Controls.Add(lblActive);
        logCell.Controls.Add(activePanel, 0, 0);

        lstLog = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(225, 240, 238),
            Font = new Font("Consolas", 9.5f),
            IntegralHeight = false
        };
        logCell.Controls.Add(lstLog, 0, 1);
        bottom.Controls.Add(logCell, 2, 0);
    }

    Button PinkButton(string text, Action click)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            BackColor = Pink,
            ForeColor = Color.White,
            Font = new Font("Bahnschrift", 9f, FontStyle.Bold),
            Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(6, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => click();
        return b;
    }

    Button ModeButton(string text, Action click)
    {
        var b = new Button
        {
            Text = text,
            Width = 400,
            Height = 90,
            Dock = DockStyle.None,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Bahnschrift", 12f, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10),
            UseVisualStyleBackColor = false
        };

        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => click();

        return b;
    }

    Button SmallModeButton(string text, Action click)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Bahnschrift", 9f, FontStyle.Bold),
            Padding = new Padding(6, 2, 6, 2),
            Margin = new Padding(0, 4, 6, 0),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => click();
        return b;
    }

    // ---------------- режим отображения схемы ----------------
    void SetView(bool eng)
    {
        engineering = eng;
        scheme.Engineering = eng;
        scheme.Invalidate();
        RefreshUi();
    }

    // ---------------- категория маршрутов ----------------
    void SetCategory(bool shunting)
    {
        shuntMode = shunting;
        //RebuildFromList();
        RefreshUi();
    }

    // "Откуда" — список уникальных точек отправления для текущей категории,
    // в порядке появления в модели (примерно слева направо по схеме).
    //void RebuildFromList()
    //{
    //    updatingCombo = true;
    //    cmbFrom.Items.Clear();
    //    foreach (var name in st.Routes.Where(r => r.Shunting == shuntMode).Select(r => r.From).Distinct())
    //        cmbFrom.Items.Add(name);
    //    if (cmbFrom.Items.Count > 0) cmbFrom.SelectedIndex = 0;
    //    updatingCombo = false;
    //    RebuildToList();
    //}

    // "Куда" — точки назначения, доступные из выбранного "Откуда" в текущей категории.
    void RebuildToList()
    {
        cmbTo.Items.Clear();
        string from = cmbFrom.SelectedItem?.ToString();
        if (from != null)
            foreach (var r in st.Routes.Where(r => r.Shunting == shuntMode && r.From == from))
                cmbTo.Items.Add(r.To);
        if (cmbTo.Items.Count > 0) cmbTo.SelectedIndex = 0;
        RebuildRouteListBox();
    }

    // общий список "От → До" для текущей категории — можно установить/отменить двойным щелчком.
    void RebuildRouteListBox()
    {
        lstRoutes.Items.Clear();
        foreach (var r in st.Routes.Where(r => r.Shunting == shuntMode))
            lstRoutes.Items.Add(r);   // ListBox показывает Route.ToString() == Name
    }

    void SetOrCancelFromCombo()
    {
        string from = cmbFrom.SelectedItem?.ToString();
        string to = cmbTo.SelectedItem?.ToString();
        if (from == null || to == null) return;
        var route = st.Find(from, to, shuntMode);
        if (route == null) { AddLog($"Маршрут {from} → {to} не найден"); return; }
        if (st.Active.Contains(route)) st.Cancel(route);
        else st.TrySetRoute(route);
    }

    void SetOrCancelFromList()
    {
        if (lstRoutes.SelectedItem is not Route route) return;
        if (st.Active.Contains(route)) st.Cancel(route);
        else st.TrySetRoute(route);
    }

    void RefreshUi()
    {
        btnTrain.BackColor = shuntMode ? ModeOff : ModeOn;
        btnShunt.BackColor = shuntMode ? ModeOn : ModeOff;
        btnSimpleView.BackColor = engineering ? ModeOff : ModeOn;
        btnEngView.BackColor = engineering ? ModeOn : ModeOff;

        foreach (var (n, b) in swBtns)
        {
            var sw = st.Switches[n];
            b.Text = $"Стр. {n}  {(sw.Plus ? "+" : "−")}";
            b.BackColor = sw.Locked ? Color.FromArgb(255, 225, 90)
                        : sw.Plus ? BtnLight : Color.FromArgb(255, 214, 153);
        }

        lblActive.Text = st.Active.Count == 0
            ? "Активные маршруты:\n  —"
            : "Активные маршруты:\n" + string.Join("\n", st.Active.Select((r, i) => $"  {i + 1}. {r.Name}"));
    }

    void AddLog(string msg)
    {
        lstLog.Items.Add($"{DateTime.Now:HH:mm:ss}  {msg}");
        while (lstLog.Items.Count > 300) lstLog.Items.RemoveAt(0);
        lstLog.TopIndex = lstLog.Items.Count - 1;
    }

    void RunTest()
    {
        AddLog("Проверка ламп");
        st.TestMode = true;
        st.NotifyChanged();
        Send("TEST");
        var t = new System.Windows.Forms.Timer { Interval = 1500 };
        t.Tick += (_, _) =>
        {
            t.Stop();
            t.Dispose();
            st.TestMode = false;
            st.NotifyChanged();
        };
        t.Start();
    }

    // ---------------- Arduino ----------------
    void RefreshPorts()
    {
        updatingPorts = true;
        try
        {
            string current = cmbPort.SelectedItem?.ToString() ?? "";
            var names = SerialPort.GetPortNames().OrderBy(x => x).ToList();
            cmbPort.Items.Clear();
            cmbPort.Items.Add("");
            foreach (var n in names) cmbPort.Items.Add(n);
            int idx = cmbPort.Items.IndexOf(current);
            cmbPort.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally { updatingPorts = false; }
    }

    void Connect(string name)
    {
        Disconnect();
        if (string.IsNullOrEmpty(name)) { SetStatus(false, null); return; }
        try
        {
            port = new SerialPort(name, 9600) { NewLine = "\n", WriteTimeout = 500, ReadTimeout = 500 };
            port.Open();
            SetStatus(true, name);
            AddLog($"Подключено к {name}");
        }
        catch (Exception ex)
        {
            port = null;
            SetStatus(false, null);
            AddLog("Ошибка порта: " + ex.Message);
        }
    }

    void Disconnect()
    {
        try { if (port != null && port.IsOpen) port.Close(); } catch { }
        port?.Dispose();
        port = null;
    }

    void SetStatus(bool ok, string name)
    {
        lblArduino.Text = ok ? $"Arduino: подключено ({name})" : "Arduino: нет соединения";
        lblArduino.ForeColor = ok ? Color.FromArgb(0, 140, 60) : Color.Red;
    }

    void Send(string cmd)
    {
        if (port != null && port.IsOpen)
        {
            try { port.WriteLine(cmd); AddLog("TX: " + cmd); }
            catch (Exception ex) { AddLog("Ошибка отправки: " + ex.Message); }
        }
        else AddLog("(нет связи) " + cmd);
    }

    private void InitializeComponent()
    {

    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Disconnect();
        base.OnFormClosing(e);
    }
}
