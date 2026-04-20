// TradingChecklist.cs
// Interactive pre-trade checklist indicator for Quantower.
// Displays a panel with configurable checklist items, interactive checkboxes,
// a title bar, and a Reset All button. Supports UI scaling, font customization,
// transparent background, and chart placement settings.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
    private readonly Font _countFont = new Font("Segoe UI", 9,  FontStyle.Regular);
    private readonly Font _resetFont = new Font("Segoe UI", 10, FontStyle.Regular);

    // ── CACHED BRUSHES & PENS (rebuilt in BuildBrushesAndPens) ───────────────────
    private SolidBrush _panelBrush;
    private SolidBrush _headerBrush;
    private SolidBrush _titleBrush;
    private SolidBrush _countBrush;
    private SolidBrush _rowHighlightBrush;
    private SolidBrush _cbUncheckedBrush;
    private SolidBrush _cbCheckedBrush;
    private SolidBrush _resetButtonBrush;
    private SolidBrush _resetTextBrush;
    private SolidBrush _textUncheckedBrush;
    private SolidBrush _textCheckedBrush;
    private Pen        _borderPen;
    private Pen        _headerDividerPen;
    private Pen        _rowDividerPen;
    private Pen        _cbUncheckedPen;
    private Pen        _cbCheckedPen;
    private Pen        _checkmarkPen;

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
        // Initialize all GDI brush/pen resources before the first OnPaintChart call.
        InitBrushesAndPens();
        LayoutUI();
        CurrentChart.MouseClick += OnChartMouseClick;
    }

    protected override void OnSettingsUpdated()
    {
        InitBrushesAndPens();
        LayoutUI();
    }

    private void InitBrushesAndPens()
    {
        // Dispose previous cached resources before rebuilding
        DisposeBrushesAndPens();

        _panelBrush        = new SolidBrush(Color.FromArgb(20, 30, 40));
        _headerBrush       = new SolidBrush(Color.FromArgb(41, 50, 60));
        _titleBrush        = new SolidBrush(TitleColor);
        _countBrush        = new SolidBrush(Color.FromArgb(184, 205, 228));
        _rowHighlightBrush = new SolidBrush(Color.FromArgb(30, CheckedColor.R, CheckedColor.G, CheckedColor.B));
        _cbUncheckedBrush  = new SolidBrush(Color.FromArgb(41, 50, 60));
        _cbCheckedBrush    = new SolidBrush(CheckedColor);
        _resetButtonBrush  = new SolidBrush(Color.FromArgb(41, 50, 60));
        _resetTextBrush    = new SolidBrush(Color.FromArgb(184, 205, 228));
        _textUncheckedBrush = new SolidBrush(ItemTextColor);
        _textCheckedBrush  = new SolidBrush(Color.FromArgb(140, ItemTextColor.R, ItemTextColor.G, ItemTextColor.B));

        _borderPen        = new Pen(Color.Gray);
        _headerDividerPen = new Pen(Color.FromArgb(80, 150, 150, 150));
        _rowDividerPen    = new Pen(Color.FromArgb(40, 150, 150, 150));
        _cbUncheckedPen   = new Pen(Color.Gray);
        _cbCheckedPen     = new Pen(CheckedColor);
        _checkmarkPen     = new Pen(Color.White, 2f) { LineJoin = LineJoin.Round };

        // Load checked state. Symbol may be null the very first time this is
        // called from OnInit(), in which case GetStateFilePath() falls back to
        // "default". On subsequent calls (e.g. when a color setting changes)
        // Symbol will be set and the correct per-symbol file will be loaded.
        LoadState();
    }

    private void DisposeBrushesAndPens()
    {
        _panelBrush?.Dispose();        _panelBrush        = null;
        _headerBrush?.Dispose();       _headerBrush       = null;
        _titleBrush?.Dispose();        _titleBrush        = null;
        _countBrush?.Dispose();        _countBrush        = null;
        _rowHighlightBrush?.Dispose(); _rowHighlightBrush = null;
        _cbUncheckedBrush?.Dispose();  _cbUncheckedBrush  = null;
        _cbCheckedBrush?.Dispose();    _cbCheckedBrush    = null;
        _resetButtonBrush?.Dispose();  _resetButtonBrush  = null;
        _resetTextBrush?.Dispose();    _resetTextBrush    = null;
        _textUncheckedBrush?.Dispose(); _textUncheckedBrush = null;
        _textCheckedBrush?.Dispose();  _textCheckedBrush  = null;
        _borderPen?.Dispose();        _borderPen        = null;
        _headerDividerPen?.Dispose(); _headerDividerPen = null;
        _rowDividerPen?.Dispose();    _rowDividerPen    = null;
        _cbUncheckedPen?.Dispose();   _cbUncheckedPen   = null;
        _cbCheckedPen?.Dispose();     _cbCheckedPen     = null;
        _checkmarkPen?.Dispose();     _checkmarkPen     = null;
    }

    public override void Dispose()
    {
        CurrentChart.MouseClick -= OnChartMouseClick;

        // Dispose fonts and string formats
        _titleFont.Dispose();
        _countFont.Dispose();
        _resetFont.Dispose();
        ItemFont?.Dispose();
        CenterFormat.Dispose();
        LeftFormat.Dispose();
        RightFormat.Dispose();

        DisposeBrushesAndPens();

        base.Dispose();
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────────
    private string[] GetItems() => new[]
    {
        Item1, Item2, Item3, Item4, Item5,
        Item6, Item7, Item8, Item9, Item10,
        Item11, Item12, Item13, Item14, Item15
    };

    /// <summary>
    /// Recalculates all layout rectangles based on current settings.
    /// Must be called after any setting that affects size or position changes.
    /// </summary>
    private void LayoutUI()
    {
        _panelW = Math.Max(150, Math.Min(PanelWidth, 600));

        var items = GetItems();
        _visibleItemCount = items.Count(s => !string.IsNullOrWhiteSpace(s));

        int X = XShift;
        int Y = YShift;

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

    // ── STATE PERSISTENCE ────────────────────────────────────────────────────────
    private string GetStateFilePath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TradingChecklist");
        Directory.CreateDirectory(dir);
        string safeSymbol = (Symbol?.Name ?? "default").Replace("/", "_").Replace("\\", "_");
        return Path.Combine(dir, $"state_{safeSymbol}.txt");
    }

    private void SaveState()
    {
        try
        {
            File.WriteAllText(GetStateFilePath(),
                new string(_checked.Select(c => c ? '1' : '0').ToArray()));
        }
        catch { }
    }

    private void LoadState()
    {
        try
        {
            string path = GetStateFilePath();
            if (!File.Exists(path)) return;
            string content = File.ReadAllText(path).Trim();
            if (content.Length == 15)
                for (int i = 0; i < 15; i++)
                    _checked[i] = content[i] == '1';
        }
        catch { }
    }

    // ── MOUSE CLICK HANDLER ──────────────────────────────────────────────────────
    private void OnChartMouseClick(object sender, ChartMouseNativeEventArgs e)
    {
        var ne = (NativeMouseEventArgs)e;

        // Convert raw chart coordinates to logical (un-scaled) coordinates.
        int x = (int)(ne.X / UIScale);
        int y = (int)(ne.Y / UIScale);

        // Reset All button
        if (_resetBtnRect.Contains(x, y))
        {
            for (int i = 0; i < 15; i++)
                _checked[i] = false;
            SaveState();
            CurrentChart.RedrawBuffer();
            return;
        }

        // Toggle items: clicking anywhere on the row (including the checkbox) toggles it.
        for (int i = 0; i < 15; i++)
        {
            if (_itemRects[i] != Rectangle.Empty && _itemRects[i].Contains(x, y))
            {
                _checked[i] = !_checked[i];
                SaveState();
                CurrentChart.RedrawBuffer();
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
                g.FillPath(_panelBrush, path);
            g.DrawPath(_borderPen, path);
        }

        // ── Header bar ───────────────────────────────────────────────────────────
        var hdrRect = new Rectangle(X, Y, _panelW, HeaderH);
        if (!TransparentBackground)
            g.FillRectangle(_headerBrush, hdrRect);

        // Divider line under header
        g.DrawLine(_headerDividerPen, X, Y + HeaderH, X + _panelW, Y + HeaderH);

        // Title text
        g.DrawString("Trading Checklist", _titleFont, _titleBrush,
            X + _panelW / 2f, Y + HeaderH / 2f, CenterFormat);

        // Checked count (e.g. "3/10") in top-right of header
        int checkedCount = 0;
        for (int i = 0; i < 15; i++)
            if (_checked[i] && !string.IsNullOrWhiteSpace(items[i]))
                checkedCount++;

        g.DrawString($"{checkedCount}/{_visibleItemCount}", _countFont, _countBrush,
            X + _panelW - Gutter, Y + HeaderH / 2f, RightFormat);

        // ── Checklist rows ───────────────────────────────────────────────────────
        int idx = 0;
        for (int i = 0; i < 15; i++)
        {
            if (string.IsNullOrWhiteSpace(items[i]))
                continue;

            int rowY = Y + HeaderH + idx * ItemH;
            bool isChecked = _checked[i];

            // Row highlight when checked
            if (isChecked)
                g.FillRectangle(_rowHighlightBrush, X, rowY, _panelW, ItemH);

            // Row divider
            g.DrawLine(_rowDividerPen, X + Gutter, rowY, X + _panelW - Gutter, rowY);

            // Checkbox background
            var cbRect = new Rectangle(X + Gutter, rowY + (ItemH - CheckSize) / 2, CheckSize, CheckSize);
            g.FillRectangle(isChecked ? _cbCheckedBrush : _cbUncheckedBrush, cbRect);
            g.DrawRectangle(isChecked ? _cbCheckedPen : _cbUncheckedPen, cbRect);

            // Checkmark (✓) when checked
            if (isChecked)
            {
                g.DrawLine(_checkmarkPen,
                    cbRect.X + 2,               cbRect.Y + cbRect.Height / 2,
                    cbRect.X + cbRect.Width / 2, cbRect.Y + cbRect.Height - 3);
                g.DrawLine(_checkmarkPen,
                    cbRect.X + cbRect.Width / 2, cbRect.Y + cbRect.Height - 3,
                    cbRect.X + cbRect.Width - 2, cbRect.Y + 3);
            }

            // Item text (dimmed when checked)
            g.DrawString(items[i], ItemFont, isChecked ? _textCheckedBrush : _textUncheckedBrush,
                X + Gutter + CheckSize + Gutter, rowY + ItemH / 2f, LeftFormat);

            idx++;
        }

        // ── Reset All button ─────────────────────────────────────────────────────
        var resetRectF = new RectangleF(_resetBtnRect.X, _resetBtnRect.Y,
                                         _resetBtnRect.Width, _resetBtnRect.Height);
        using (var path = RoundedRect(resetRectF, BtnRadius))
        {
            g.FillPath(_resetButtonBrush, path);
            g.DrawPath(_borderPen, path);
        }
        g.DrawString("Reset All", _resetFont, _resetTextBrush,
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
