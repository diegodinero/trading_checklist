// TradingChecklist.cs
// Interactive pre-trade checklist indicator for Quantower.
// Displays a panel with configurable checklist items, interactive checkboxes,
// a title bar, and a Reset All button. Supports UI scaling, font customization,
// transparent background, and chart placement settings.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using TradingPlatform.BusinessLayer.Native;

#nullable disable
public class TradingChecklist : Indicator
{
    // ── CHECKLIST ITEMS ──────────────────────────────────────────────────────────
    [InputParameter("Item 1", 0)]
    public string Item1 { get; set; } = "Trend confirmed on higher timeframe";

    [InputParameter("Item 2", 1)]
    public string Item2 { get; set; } = "Key support/resistance identified";

    [InputParameter("Item 3", 2)]
    public string Item3 { get; set; } = "Entry signal confirmed";

    [InputParameter("Item 4", 3)]
    public string Item4 { get; set; } = "Stop loss placed";

    [InputParameter("Item 5", 4)]
    public string Item5 { get; set; } = "Risk/Reward acceptable";

    [InputParameter("Item 6", 5)]
    public string Item6 { get; set; } = "Position size calculated";

    [InputParameter("Item 7", 6)]
    public string Item7 { get; set; } = "No major news events";

    [InputParameter("Item 8", 7)]
    public string Item8 { get; set; } = "Trading session check";

    [InputParameter("Item 9", 8)]
    public string Item9 { get; set; } = "Emotion check";

    [InputParameter("Item 10", 9)]
    public string Item10 { get; set; } = "Daily loss limit check";

    [InputParameter("Item 11", 10)]
    public string Item11 { get; set; } = "";

    [InputParameter("Item 12", 11)]
    public string Item12 { get; set; } = "";

    [InputParameter("Item 13", 12)]
    public string Item13 { get; set; } = "";

    [InputParameter("Item 14", 13)]
    public string Item14 { get; set; } = "";

    [InputParameter("Item 15", 14)]
    public string Item15 { get; set; } = "";

    // ── PERSISTED CHECKED STATES ─────────────────────────────────────────────────
    [InputParameter("Checked State 1", 50)]
    public bool Checked1 { get; set; } = false;
    [InputParameter("Checked State 2", 51)]
    public bool Checked2 { get; set; } = false;
    [InputParameter("Checked State 3", 52)]
    public bool Checked3 { get; set; } = false;
    [InputParameter("Checked State 4", 53)]
    public bool Checked4 { get; set; } = false;
    [InputParameter("Checked State 5", 54)]
    public bool Checked5 { get; set; } = false;
    [InputParameter("Checked State 6", 55)]
    public bool Checked6 { get; set; } = false;
    [InputParameter("Checked State 7", 56)]
    public bool Checked7 { get; set; } = false;
    [InputParameter("Checked State 8", 57)]
    public bool Checked8 { get; set; } = false;
    [InputParameter("Checked State 9", 58)]
    public bool Checked9 { get; set; } = false;
    [InputParameter("Checked State 10", 59)]
    public bool Checked10 { get; set; } = false;
    [InputParameter("Checked State 11", 60)]
    public bool Checked11 { get; set; } = false;
    [InputParameter("Checked State 12", 61)]
    public bool Checked12 { get; set; } = false;
    [InputParameter("Checked State 13", 62)]
    public bool Checked13 { get; set; } = false;
    [InputParameter("Checked State 14", 63)]
    public bool Checked14 { get; set; } = false;
    [InputParameter("Checked State 15", 64)]
    public bool Checked15 { get; set; } = false;

    // ── POSITION SETTINGS ────────────────────────────────────────────────────────
    [InputParameter("X Offset", 15)]
    public int XShift { get; set; } = 20;

    [InputParameter("Y Offset", 16)]
    public int YShift { get; set; } = 20;

    // ── UI SETTINGS ──────────────────────────────────────────────────────────────
    [InputParameter("UI Scale", 17, 0.5, 3.0, 0.1)]
    public double UIScale { get; set; } = 1.0;

    [InputParameter("Panel Width", 18, 150, 600, 10)]
    public int PanelWidth { get; set; } = 300;

    [InputParameter("Transparent Background", 19)]
    public bool TransparentBackground { get; set; } = false;

    // ── COLOR SETTINGS ───────────────────────────────────────────────────────────
    [InputParameter("Title Color", 20)]
    public Color TitleColor { get; set; } = Color.White;

    [InputParameter("Item Text Color", 21)]
    public Color ItemTextColor { get; set; } = Color.White;

    [InputParameter("Checked Color", 22)]
    public Color CheckedColor { get; set; } = Color.FromArgb(47, 164, 102);

    // ── FONT SETTING (via Settings override) ─────────────────────────────────────
    public Font ItemFont { get; private set; } = new Font("Segoe UI", 11, FontStyle.Regular);

    public override IList<SettingItem> Settings
    {
        get
        {
            var settings = base.Settings;
            var defaultSeparator = settings.FirstOrDefault()?.SeparatorGroup;
            settings.Add(new SettingItemFont("Item Font", ItemFont, 100) { SeparatorGroup = defaultSeparator });
            return settings;
        }
        set
        {
            base.Settings = value;
            if (value.TryGetValue("Item Font", out Font fontItem))
                ItemFont = fontItem;
        }
    }

    // ── LAYOUT CONSTANTS ─────────────────────────────────────────────────────────
    private const int HeaderH = 36;
    private const int ItemH = 28;
    private const int CheckSize = 14;
    private const int Gutter = 8;
    private const int BtnRadius = 10;
    private const int ResetBtnH = 28;

    // ── FONTS ────────────────────────────────────────────────────────────────────
    private readonly Font _titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
    private readonly Font _countFont = new Font("Segoe UI", 9, FontStyle.Regular);
    private readonly Font _resetFont = new Font("Segoe UI", 10, FontStyle.Regular);

    // ── STRING FORMATS ───────────────────────────────────────────────────────────
    private readonly StringFormat CenterFormat = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
    private readonly StringFormat LeftFormat = new StringFormat
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center
    };
    private readonly StringFormat RightFormat = new StringFormat
    {
        Alignment = StringAlignment.Far,
        LineAlignment = StringAlignment.Center
    };

    // ── RUNTIME STATE ────────────────────────────────────────────────────────────
    private readonly bool[] _checked = new bool[15];

    // ── MOVE STATE ───────────────────────────────────────────────────────────────
    // Repositioning uses two clicks: click header to "grab", click anywhere to "place".
    private bool _isMoving;
    private int _movePickupOffsetX;   // cursor offset from panel left edge when grabbed
    private int _movePickupOffsetY;   // cursor offset from panel top  edge when grabbed

    // ── CACHED LAYOUT RECTS ──────────────────────────────────────────────────────
    private int _panelW;
    private int _visibleItemCount;
    private readonly Rectangle[] _itemRects = new Rectangle[15];
    private readonly Rectangle[] _checkRects = new Rectangle[15];
    private Rectangle _resetBtnRect;
    private RectangleF _panelRect;

    // ── CONSTRUCTOR ──────────────────────────────────────────────────────────────
    public TradingChecklist()
    {
        Name = "Trading Checklist";
        Description = "Interactive pre-trade checklist with up to 15 configurable items.";
        SeparateWindow = false;
    }

    // ── LIFECYCLE ────────────────────────────────────────────────────────────────
    protected override void OnInit()
    {
        base.OnInit();
        LoadCheckedStates(); // Re-enable persistence
        LayoutUI();
        if (CurrentChart != null)
            CurrentChart.MouseClick += OnChartMouseClick;
    }

    protected override void OnSettingsUpdated()
    {
        base.OnSettingsUpdated();
        LoadCheckedStates(); // Re-enable persistence
        LayoutUI();
        CurrentChart?.RedrawBuffer();
    }

    public override void Dispose()
    {
        if (CurrentChart != null)
            CurrentChart.MouseClick -= OnChartMouseClick;

        // Don't dispose fonts - let GC handle them to avoid refresh issues
        // The fonts are readonly fields and disposing them breaks refresh

        base.Dispose();
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────────
    private string[] GetItems()
    {
        // Return actual InputParameter values without forced fallbacks
        // This allows dynamic show/hide to work properly
        return new string[]
        {
            Item1 ?? "",
            Item2 ?? "",
            Item3 ?? "",
            Item4 ?? "",
            Item5 ?? "",
            Item6 ?? "",
            Item7 ?? "",
            Item8 ?? "",
            Item9 ?? "",
            Item10 ?? "",
            Item11 ?? "",
            Item12 ?? "",
            Item13 ?? "",
            Item14 ?? "",
            Item15 ?? ""
        };
    }

    /// <summary>
    /// Load checked states from InputParameters to maintain state across chart refreshes
    /// </summary>
    private void LoadCheckedStates()
    {
        _checked[0] = Checked1;
        _checked[1] = Checked2;
        _checked[2] = Checked3;
        _checked[3] = Checked4;
        _checked[4] = Checked5;
        _checked[5] = Checked6;
        _checked[6] = Checked7;
        _checked[7] = Checked8;
        _checked[8] = Checked9;
        _checked[9] = Checked10;
        _checked[10] = Checked11;
        _checked[11] = Checked12;
        _checked[12] = Checked13;
        _checked[13] = Checked14;
        _checked[14] = Checked15;
    }

    /// <summary>
    /// Save checked states to InputParameters for persistence across chart refreshes
    /// </summary>
    private void SaveCheckedStates()
    {
        Checked1 = _checked[0];
        Checked2 = _checked[1];
        Checked3 = _checked[2];
        Checked4 = _checked[3];
        Checked5 = _checked[4];
        Checked6 = _checked[5];
        Checked7 = _checked[6];
        Checked8 = _checked[7];
        Checked9 = _checked[8];
        Checked10 = _checked[9];
        Checked11 = _checked[10];
        Checked12 = _checked[11];
        Checked13 = _checked[12];
        Checked14 = _checked[13];
        Checked15 = _checked[14];
    }

    /// <summary>
    /// Recalculates all layout rectangles based on current settings.
    /// Must be called after any setting that affects size or position changes.
    /// </summary>
    private void LayoutUI()
    {
        _panelW = Math.Max(150, Math.Min(PanelWidth, 600));

        int X = XShift;
        int Y = YShift;

        var items = GetItems();

        // Count visible items (non-empty)
        int visibleCount = 0;
        for (int i = 0; i < 15; i++)
        {
            if (!string.IsNullOrWhiteSpace(items[i]))
                visibleCount++;
        }
        _visibleItemCount = visibleCount;

        // Create layout only for items that have content
        int idx = 0;
        for (int i = 0; i < 15; i++)
        {
            if (!string.IsNullOrWhiteSpace(items[i]))
            {
                int rowY = Y + HeaderH + idx * ItemH;
                _itemRects[i] = new Rectangle(X, rowY, _panelW, ItemH);
                _checkRects[i] = new Rectangle(
                    X + Gutter,
                    rowY + (ItemH - CheckSize) / 2,
                    CheckSize,
                    CheckSize
                );
                idx++;
            }
            else
            {
                _itemRects[i] = Rectangle.Empty;
                _checkRects[i] = Rectangle.Empty;
            }
        }

        int resetY = Y + HeaderH + _visibleItemCount * ItemH + Gutter;
        _resetBtnRect = new Rectangle(X + Gutter, resetY, _panelW - Gutter * 2, ResetBtnH);

        int totalH = HeaderH + _visibleItemCount * ItemH + Gutter + ResetBtnH + Gutter;
        _panelRect = new RectangleF(X - 4, Y - 4, _panelW + 8, totalH + 8);
    }

    // ── MOUSE CLICK HANDLER ──────────────────────────────────────────────────────
    private void OnChartMouseClick(object sender, ChartMouseNativeEventArgs e)
    {
        var ne = (NativeMouseEventArgs)e;

        // Convert raw chart coordinates to logical (un-scaled) coordinates.
        int x = (int)(ne.X / UIScale);
        int y = (int)(ne.Y / UIScale);

        var headerRect = new Rectangle(XShift, YShift, _panelW, HeaderH);

        // ── Move-mode active: the next click anywhere places the panel ────────────
        if (_isMoving)
        {
            // If the user clicks the header again, cancel the move instead of placing
            if (headerRect.Contains(x, y))
            {
                _isMoving = false;
                CurrentChart?.RedrawBuffer();
                return;
            }
            // Place panel so the grabbed offset is preserved under the cursor
            XShift = Math.Max(0, x - _movePickupOffsetX);
            YShift = Math.Max(0, y - _movePickupOffsetY);
            _isMoving = false;
            LayoutUI();
            CurrentChart?.RedrawBuffer();
            return;
        }

        // ── Header clicked: enter move mode ──────────────────────────────────────
        if (headerRect.Contains(x, y))
        {
            _isMoving          = true;
            _movePickupOffsetX = x - XShift;
            _movePickupOffsetY = y - YShift;
            CurrentChart?.RedrawBuffer();
            return;
        }

        // ── Reset All button ──────────────────────────────────────────────────────
        if (_resetBtnRect.Contains(x, y))
        {
            for (int i = 0; i < 15; i++)
                _checked[i] = false;
            SaveCheckedStates();
            CurrentChart?.RedrawBuffer();
            return;
        }

        // ── Toggle checklist items ────────────────────────────────────────────────
        for (int i = 0; i < 15; i++)
        {
            if (_itemRects[i] != Rectangle.Empty && _itemRects[i].Contains(x, y))
            {
                _checked[i] = !_checked[i];
                SaveCheckedStates();
                CurrentChart?.RedrawBuffer();
                return;
            }
        }
    }

    // ── PAINT ────────────────────────────────────────────────────────────────────
    public override void OnPaintChart(PaintChartEventArgs args)
    {
        base.OnPaintChart(args);

        var g = args.Graphics;
        float s = (float)UIScale;
        var savedState = g.Save();
        g.ScaleTransform(s, s);

        int X = XShift;
        int Y = YShift;
        var items = GetItems();

        // ── Panel background & rounded border ────────────────────────────────────
        using (var path = RoundedRect(_panelRect, BtnRadius))
        {
            if (!TransparentBackground)
            {
                using (var br = new SolidBrush(Color.FromArgb(20, 30, 40)))
                    g.FillPath(br, path);
            }
            using (var pen = new Pen(Color.Gray))
                g.DrawPath(pen, path);
        }

        // ── Header bar ───────────────────────────────────────────────────────────
        var hdrRect = new Rectangle(X, Y, _panelW, HeaderH);
        if (!TransparentBackground || _isMoving)
        {
            // Highlight header when in move mode so the user gets clear visual feedback
            Color hdrColor = _isMoving
                ? Color.FromArgb(35, 90, 120)   // blue tint = "grabbed"
                : Color.FromArgb(41, 50, 60);
            using (var br = new SolidBrush(hdrColor))
                g.FillRectangle(br, hdrRect);
        }

        // Divider line under header
        using (var divPen = new Pen(Color.FromArgb(80, 150, 150, 150)))
            g.DrawLine(divPen, X, Y + HeaderH, X + _panelW, Y + HeaderH);

        // Move handle indicator (3 rows × 2 columns of small dots) at the left of the header
        Color dotColor = _isMoving
            ? Color.FromArgb(200, 100, 200, 255)  // bright blue when grabbed
            : Color.FromArgb(120, 180, 180, 180);
        using (var dotBrush = new SolidBrush(dotColor))
        {
            const int dotSize = 2;
            const int dotGap  = 3;
            int handleX = X + Gutter;
            int handleY = Y + (HeaderH - (3 * dotSize + 2 * dotGap)) / 2;
            for (int col = 0; col < 2; col++)
                for (int row = 0; row < 3; row++)
                    g.FillEllipse(dotBrush,
                        handleX + col * (dotSize + dotGap),
                        handleY + row * (dotSize + dotGap),
                        dotSize, dotSize);
        }

        // Title text — append a hint when in move mode
        string titleText = _isMoving ? "Click to place…" : "Trading Checklist";
        using (var titleBrush = new SolidBrush(TitleColor))
            g.DrawString(titleText, _titleFont, titleBrush,
                X + _panelW / 2f, Y + HeaderH / 2f, CenterFormat);

        // Checked count (e.g. "3/10") in top-right of header
        int checkedCount = 0;
        for (int i = 0; i < 15; i++)
            if (_checked[i] && !string.IsNullOrWhiteSpace(items[i]))
                checkedCount++;

        string countStr = $"{checkedCount}/{_visibleItemCount}";
        using (var countBrush = new SolidBrush(Color.FromArgb(184, 205, 228)))
            g.DrawString(countStr, _countFont, countBrush,
                X + _panelW - Gutter, Y + HeaderH / 2f, RightFormat);

        // ── Checklist rows ───────────────────────────────────────────────────────
        int idx = 0;
        for (int i = 0; i < 15; i++) // Restore to 15 items with filtering
        {
            // Skip empty items to match layout
            if (string.IsNullOrWhiteSpace(items[i]))
                continue;

            int rowY = Y + HeaderH + idx * ItemH; // Use idx for correct positioning
            bool isChecked = _checked[i];

            // Row highlight when checked
            if (isChecked)
            {
                using (var rowBr = new SolidBrush(Color.FromArgb(30, CheckedColor.R, CheckedColor.G, CheckedColor.B)))
                    g.FillRectangle(rowBr, X, rowY, _panelW, ItemH);
            }

            // Row divider
            using (var divPen = new Pen(Color.FromArgb(40, 150, 150, 150)))
                g.DrawLine(divPen, X + Gutter, rowY, X + _panelW - Gutter, rowY);

            // Checkbox background
            var cbRect = new Rectangle(X + Gutter, rowY + (ItemH - CheckSize) / 2, CheckSize, CheckSize);
            using (var cbFill = new SolidBrush(isChecked ? CheckedColor : Color.FromArgb(41, 50, 60)))
                g.FillRectangle(cbFill, cbRect);
            using (var cbPen = new Pen(isChecked ? CheckedColor : Color.Gray))
                g.DrawRectangle(cbPen, cbRect);

            // Checkmark (✓) when checked
            if (isChecked)
            {
                using (var chkPen = new Pen(Color.White, 2f) { LineJoin = LineJoin.Round })
                {
                    g.DrawLine(chkPen,
                        cbRect.X + 2,               cbRect.Y + cbRect.Height / 2,
                        cbRect.X + cbRect.Width / 2, cbRect.Y + cbRect.Height - 3);
                    g.DrawLine(chkPen,
                        cbRect.X + cbRect.Width / 2, cbRect.Y + cbRect.Height - 3,
                        cbRect.X + cbRect.Width - 2, cbRect.Y + 3);
                }
            }

            // Item text (dimmed when checked) - Add extra safety check
            string itemText = items[i] ?? $"Item {i + 1}"; // Fallback text if still null
            Color textColor = isChecked
                ? Color.FromArgb(140, ItemTextColor.R, ItemTextColor.G, ItemTextColor.B)
                : ItemTextColor;

            using (var textBrush = new SolidBrush(textColor))
            {
                Font fontToUse = ItemFont ?? new Font("Arial", 10);
                g.DrawString(itemText, fontToUse, textBrush,
                    X + Gutter + CheckSize + Gutter,
                    rowY + ItemH / 2f,
                    LeftFormat);
            }

            idx++; // Increment index for next visible row
        }

        // ── Reset All button ─────────────────────────────────────────────────────
        var resetRectF = new RectangleF(_resetBtnRect.X, _resetBtnRect.Y,
                                         _resetBtnRect.Width, _resetBtnRect.Height);
        using (var path = RoundedRect(resetRectF, BtnRadius))
        {
            using (var br = new SolidBrush(Color.FromArgb(41, 50, 60)))
                g.FillPath(br, path);
            using (var pen = new Pen(Color.Gray))
                g.DrawPath(pen, path);
        }
        using (var resetBrush = new SolidBrush(Color.FromArgb(184, 205, 228)))
            g.DrawString("Reset All", _resetFont, resetBrush,
                _resetBtnRect.X + _resetBtnRect.Width / 2f,
                _resetBtnRect.Y + _resetBtnRect.Height / 2f,
                CenterFormat);

        g.Restore(savedState);
    }

    // ── UTILITY ──────────────────────────────────────────────────────────────────
    /// <summary>Returns a GraphicsPath for a rectangle with rounded corners.</summary>
    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X,           r.Y,            d, d, 180, 90);
        path.AddArc(r.Right - d,   r.Y,            d, d, 270, 90);
        path.AddArc(r.Right - d,   r.Bottom - d,   d, d,   0, 90);
        path.AddArc(r.X,           r.Bottom - d,   d, d,  90, 90);
        path.CloseAllFigures();
        return path;
    }
}
