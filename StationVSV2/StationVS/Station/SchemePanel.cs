using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace StationApp;

// =====================================================================
//  ПАНЕЛЬ С ОТРИСОВКОЙ СХЕМЫ
// =====================================================================

class SchemePanel : Panel
{
    readonly Station st;

    float scale = 1;
    PointF off;

    // Первый выбранный светофор
    Signal selectedSignal = null;


    // Простой / инженерный режим
    public bool Engineering { get; set; } = true;

    readonly System.Drawing.Font fLabel =
    new System.Drawing.Font("Arial", 12f, System.Drawing.FontStyle.Bold);

    readonly System.Drawing.Font fSwitch =
        new System.Drawing.Font("Arial", 10f, System.Drawing.FontStyle.Bold);

    readonly System.Drawing.Font fHelp =
        new System.Drawing.Font("Arial", 9f);

    readonly StringFormat center = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    static readonly Color TextColor = Color.FromArgb(55, 65, 65);

    // Цвет недоступного направления
    static readonly Color DisabledSignalColor =
        Color.FromArgb(145, 145, 145);

    public SchemePanel(Station station)
    {
        st = station;

        DoubleBuffered = true;
        ResizeRedraw = true;

        BackColor = Color.FromArgb(143, 191, 186);

        st.Changed += () => Invalidate();
    }

    // =================================================================
    //  МАСШТАБИРОВАНИЕ
    // =================================================================

    void UpdateTransform()
    {
        scale = Math.Min(
            Width / Station.W,
            Height / Station.H
        );

        if (scale <= 0)
            scale = 1;

        off = new PointF(
            (Width - Station.W * scale) / 2f,
            (Height - Station.H * scale) / 2f
        );
    }

    PointF ToDesign(Point p)
    {
        return new PointF(
            (p.X - off.X) / scale,
            (p.Y - off.Y) / scale
        );
    }

    // =================================================================
    //  ОТРИСОВКА
    // =================================================================

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint =
            System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        UpdateTransform();

        g.TranslateTransform(off.X, off.Y);
        g.ScaleTransform(scale, scale);

        // =============================================================
        // ПУТЬ
        // =============================================================

        using (var basePen = new Pen(Color.White, 4f))
        {
            foreach (var pts in st.Decor)
                g.DrawLines(basePen, pts);

            foreach (var pts in st.Segs.Values)
                g.DrawLines(basePen, pts);
        }

        // =============================================================
        // СТРЕЛКИ
        // =============================================================

        using (var thick = new Pen(
                   Color.White,
                   7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })

        using (var thin = new Pen(
                   Color.FromArgb(200, 214, 210),
                   2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })
        {
            foreach (var sw in st.Switches.Values)
            {
                if (sw.PlusSeg != null &&
                    st.Segs.TryGetValue(sw.PlusSeg, out var pPts))
                {
                    g.DrawLines(
                        sw.Plus ? thick : thin,
                        pPts
                    );
                }

                if (sw.MinusSeg != null &&
                    st.Segs.TryGetValue(sw.MinusSeg, out var mPts))
                {
                    g.DrawLines(
                        !sw.Plus ? thick : thin,
                        mPts
                    );
                }
            }
        }

        // =============================================================
        // УСТАНОВЛЕННЫЕ МАРШРУТЫ
        // =============================================================

        foreach (var r in st.Active)
        {
            using var p = new Pen(
                r.Shunting
                    ? Color.FromArgb(255, 235, 90)
                    : Color.FromArgb(255, 235, 90),
                5f
            );

            foreach (var id in r.Segs)
            {
                if (st.Segs.TryGetValue(id, out var pts))
                    g.DrawLines(p, pts);
            }
        }

        // =============================================================
        // ЗАНЯТЫЕ УЧАСТКИ
        // =============================================================

        using (var occ = new Pen(
                   Color.FromArgb(230, 30, 30),
                   5f))
        {
            foreach (var id in st.Occupied)
            {
                if (st.Segs.TryGetValue(id, out var pts))
                    g.DrawLines(occ, pts);
            }
        }

        // =============================================================
        // СТРЕЛКИ: НОМЕРА
        // =============================================================

        foreach (var sw in st.Switches.Values)
        {
            if (Engineering)
            {
                string txt = sw.Num + (sw.Plus ? "+" : "−");

                var size = g.MeasureString(
                    txt,
                    fSwitch
                );

                var rect = new RectangleF(
                    sw.Label.X - size.Width / 2 - 2,
                    sw.Label.Y - size.Height / 2,
                    size.Width + 4,
                    size.Height
                );

                if (sw.Locked)
                {
                    using var b = new SolidBrush(
                        Color.FromArgb(255, 225, 90)
                    );

                    g.FillRectangle(b, rect);
                }

                using var textBrush = new SolidBrush(
                    sw.Plus
                        ? TextColor
                        : Color.FromArgb(170, 60, 0)
                );

                g.DrawString(
                    txt,
                    fSwitch,
                    textBrush,
                    sw.Label,
                    center
                );
            }
            else
            {
                var c = sw.Locked
                    ? Color.FromArgb(255, 200, 60)
                    : Color.FromArgb(80, 100, 100);

                using var b = new SolidBrush(c);

                g.FillEllipse(
                    b,
                    sw.Label.X - 3.5f,
                    sw.Label.Y - 3.5f,
                    7,
                    7
                );
            }
        }

        // =============================================================
        // СВЕТОФОРЫ
        // =============================================================

        foreach (var s in st.Signals)
            DrawSignal(g, s);

        // =============================================================
        // ПОДСКАЗКА
        // =============================================================

        using (var hb = new SolidBrush(
                   Color.FromArgb(70, 85, 85)))
        {
            g.DrawString(
                "ЛКМ по светофору → выбрать направление   |   ЛКМ по стрелке → перевести   |   ПКМ по пути → занять / освободить",
                fHelp,
                hb,
                10,
                Station.H - 24
            );
        }
    }

    // =================================================================
    //  ОТРИСОВКА СВЕТОФОРА
    // =================================================================

    void DrawSignal(Graphics g, Signal s)
    {
        // =============================================================
        // ЛАМПЫ
        // =============================================================

        if (s.Shunting)
        {
            bool on = s.Open || st.TestMode;

            Lamp(
                g,
                s.Lamp,
                on
                    ? Color.White
                    : Color.FromArgb(128, 128, 128),
                9f
            );

            if (Engineering)
            {
                Lamp(
                    g,
                    new PointF(
                        s.Lamp.X,
                        s.Lamp.Y + 20
                    ),
                    Color.FromArgb(200, 40, 40),
                    6f
                );
            }
        }
        else
        {
            bool redOn = !s.Open || st.TestMode;
            bool greenOn = s.Open || st.TestMode;

            Lamp(
                g,
                s.Lamp,
                redOn
                    ? Color.Red
                    : Color.FromArgb(100, 20, 20),
                9f
            );

            Lamp(
                g,
                new PointF(
                    s.Lamp.X + 21,
                    s.Lamp.Y
                ),
                greenOn
                    ? Color.LimeGreen
                    : Color.FromArgb(128, 128, 128),
                9f
            );

            if (Engineering)
            {
                Lamp(
                    g,
                    new PointF(
                        s.Lamp.X + 10,
                        s.Lamp.Y + 20
                    ),
                    st.TestMode
                        ? Color.Gold
                        : Color.FromArgb(120, 110, 40),
                    6f
                );
            }
        }

        // =============================================================
        // ЦВЕТ ТЕКСТА
        // =============================================================

        Color textColor = TextColor;

        // Если уже выбран первый светофор,
        // показываем доступные направления чёрным,
        // а недоступные — серым.
        if (selectedSignal != null &&
            selectedSignal != s)
        {
            if (!CanBuildRoute(selectedSignal, s))
                textColor = DisabledSignalColor;
        }

        // Выбранный первый светофор тоже остаётся чёрным.
        // Никакого кружка или дополнительной подсветки нет.

        using var tb = new SolidBrush(textColor);

        g.DrawString(
            s.Name,
            fLabel,
            tb,
            s.Label,
            center
        );
    }

    // =================================================================
    //  ЛАМПА
    // =================================================================

    static void Lamp(
        Graphics g,
        PointF c,
        Color color,
        float r)
    {
        using var b = new SolidBrush(color);

        using var p = new Pen(
            Color.FromArgb(90, 90, 90),
            1.5f
        );

        g.FillEllipse(
            b,
            c.X - r,
            c.Y - r,
            r * 2,
            r * 2
        );

        g.DrawEllipse(
            p,
            c.X - r,
            c.Y - r,
            r * 2,
            r * 2
        );
    }

    // =================================================================
    //  ПОИСК СТРЕЛКИ
    // =================================================================

    Switch SwitchAt(PointF p)
    {
        foreach (var sw in st.Switches.Values)
        {
            if (Math.Abs(p.X - sw.Label.X) < 20 &&
                Math.Abs(p.Y - sw.Label.Y) < 12)
            {
                return sw;
            }
        }

        return null;
    }

    // =================================================================
    //  ПОИСК СВЕТОФОРА
    // =================================================================

    Signal SignalAt(PointF p)
    {
        foreach (var s in st.Signals)
        {
            // Область клика именно вокруг текста.
            if (Math.Abs(p.X - s.Label.X) < 30 &&
                Math.Abs(p.Y - s.Label.Y) < 15)
            {
                return s;
            }
        }

        return null;
    }

    // =================================================================
    //  ПРОВЕРКА: МОЖНО ЛИ ПОСТРОИТЬ МАРШРУТ
    // =================================================================

    bool CanBuildRoute(Signal from, Signal to)
    {
        // Нельзя строить маршрут самому в себя
        if (from == to)
            return false;

        // -------------------------------------------------------------
        // Сначала ищем маневровый маршрут
        // -------------------------------------------------------------

        Route route = st.Find(
            from.Name,
            to.Name,
            true
        );

        // -------------------------------------------------------------
        // Если маневрового нет — ищем поездной
        // -------------------------------------------------------------

        if (route == null)
        {
            route = st.Find(
                from.Name,
                to.Name,
                false
            );
        }

        // Такого маршрута вообще нет
        if (route == null)
            return false;

        // Уже установлен
        if (st.Active.Contains(route))
            return false;

        // -------------------------------------------------------------
        // Проверяем занятые участки
        // -------------------------------------------------------------

        foreach (var seg in route.Segs)
        {
            if (st.Occupied.Contains(seg))
                return false;
        }

        // -------------------------------------------------------------
        // Проверяем конфликты с уже установленными маршрутами
        // -------------------------------------------------------------

        foreach (var active in st.Active)
        {
            // Общий участок
            if (active.Segs.Intersect(route.Segs).Any())
                return false;

            // Один и тот же светофор отправления
            if (active.From == route.From)
                return false;
        }

        // -------------------------------------------------------------
        // Проверяем стрелки
        // -------------------------------------------------------------

        foreach (var (num, plus) in route.Sw)
        {
            var sw = st.Switches[num];

            if (sw.Locked && sw.Plus != plus)
                return false;
        }

        return true;
    }

    // =================================================================
    //  НАВЕДЕНИЕ МЫШИ
    // =================================================================

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var p = ToDesign(e.Location);

        bool overSwitch = SwitchAt(p) != null;
        bool overSignal = SignalAt(p) != null;

        Cursor =
            overSwitch || overSignal
                ? Cursors.Hand
                : Cursors.Default;
    }

    // =================================================================
    //  НАЖАТИЕ МЫШИ
    // =================================================================

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        var p = ToDesign(e.Location);

        // =============================================================
        // ЛЕВАЯ КНОПКА
        // =============================================================

        if (e.Button == MouseButtons.Left)
        {
            // ---------------------------------------------------------
            // Сначала проверяем стрелку
            // ---------------------------------------------------------

            var sw = SwitchAt(p);

            if (sw != null)
            {
                st.ToggleSwitch(sw.Num);
                return;
            }

            // ---------------------------------------------------------
            // Затем проверяем светофор
            // ---------------------------------------------------------

            var signal = SignalAt(p);

            if (signal != null)
            {
                SelectSignal(signal);
                return;
            }
        }

        // =============================================================
        // ПРАВАЯ КНОПКА
        // =============================================================

        else if (e.Button == MouseButtons.Right)
        {
            var id = st.HitSeg(p);

            if (id != null)
                st.ToggleOccupied(id);
        }
    }

    // =================================================================
    //  ВЫБОР СВЕТОФОРА / ПОСТРОЕНИЕ МАРШРУТА
    // =================================================================

    void SelectSignal(Signal signal)
    {
        // =============================================================
        // ПЕРВЫЙ КЛИК
        // =============================================================

        if (selectedSignal == null)
        {
            selectedSignal = signal;

            st.Say(
                $"Выбран {signal.Name}. Выберите конечный светофор."
            );

            Invalidate();

            return;
        }

        // =============================================================
        // НАЖАЛИ НА ТОТ ЖЕ СВЕТОФОР — ОТМЕНА
        // =============================================================

        if (selectedSignal == signal)
        {
            selectedSignal = null;

            st.Say("Выбор маршрута отменён.");

            Invalidate();

            return;
        }

        // =============================================================
        // ВТОРОЙ КЛИК
        // =============================================================

        Signal from = selectedSignal;
        Signal to = signal;

        // Сбрасываем выбор
        selectedSignal = null;

        // =============================================================
        // ИЩЕМ МАРШРУТ
        // =============================================================

        Route route = st.Find(
            from.Name,
            to.Name,
            true
        );

        // Если маневрового нет — ищем поездной
        if (route == null)
        {
            route = st.Find(
                from.Name,
                to.Name,
                false
            );
        }

        // =============================================================
        // МАРШРУТА НЕТ
        // =============================================================

        if (route == null)
        {
            st.Say(
                $"Маршрут {from.Name} → {to.Name} отсутствует."
            );

            Invalidate();

            return;
        }

        // =============================================================
        // ПЫТАЕМСЯ ПОСТРОИТЬ
        // =============================================================

        if (!CanBuildRoute(from, to))
        {
            st.Say(
                $"Маршрут {from.Name} → {to.Name} сейчас недоступен."
            );

            Invalidate();

            return;
        }

        st.TrySetRoute(route);

        Invalidate();
    }
}