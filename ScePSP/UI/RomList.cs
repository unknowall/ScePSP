using ScePSP.Hle.Formats;
using ScePSP.Hle.Vfs.Iso;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ScePSX.UI
{
    public class RomList : ListBox
    {
        public class GameEntry
        {
            public string IsoFile;
            public long IsoSize;
            public string DiscId0;
            public string APP_VER;
            public bool BOOTABLE;
            public string CATEGORY;
            public string DISC_ID;
            public int DISC_NUMBER;
            public int DISC_TOTAL;
            public string DISC_VERSION;
            public string DRIVER_PATH;
            public string GAMEDATA_ID;
            public int HRKGMP_VER;
            public int PARENTAL_LEVEL;
            public string PSP_SYSTEM_VER;
            public int REGION;
            public string REGION_STR;
            public string TITLE;
            public bool USE_USB;
            public byte[] Icon0Png;
            public Image CachedBitmap;
            public string Hash;
            public bool PatchedWithPrometheus;
        }

        public List<GameEntry> Entries = new List<GameEntry>();
        public event Action<string, int, int> Progress;
        public event Action<GameEntry, bool> EntryAdded;
        static XmlSerializer Serializer = new XmlSerializer(typeof(GameEntry));

        private int _hoverIndex = -1;
        private readonly Image DefaultIcon;

        private Rectangle scrollBarBounds; // 滚动条区域
        private Rectangle thumbBounds;    // 滑块区域
        private bool isDraggingThumb = false; // 是否正在拖动滑块
        private int thumbPosition = 0;    // 滑块当前位置
        private int thumbSize;            // 滑块大小
        private bool _isScrollBarVisible;
        private const int ScrollBarWidth = 8;     // 滚动条总宽度
        private const int ScrollBarMargin = 2;     // 滚动条与边缘间距
        private const int ThumbMinSize = 20;       // 滑块最小高度
        private readonly Color TrackColor = Color.FromArgb(60, 60, 60);    // 轨道颜色
        private readonly Color ThumbColor = Color.FromArgb(100, 100, 100); // 滑块颜色
        private readonly Color ThumbHoverColor = Color.FromArgb(120, 120, 120); // 滑块悬停颜色

        private Color TextColor = Color.White;
        private Color InfoBackColor = Color.FromArgb(50, 50, 50);
        private Color MainBackColor = Color.FromArgb(45, 45, 45);
        private Color MenuBackColor = Color.FromArgb(45, 45, 45);
        private Color ItemBackColor2 = Color.FromArgb(43, 43, 43);
        private Color ItemBackColor1 = Color.FromArgb(50, 50, 50);
        private Color HoverColor = Color.FromArgb(70, 70, 70); // 悬停时的高亮颜色
        private Color SelectionColor = Color.Orange; // 选中时的高亮颜色
        private Color BorderColor = Color.FromArgb(100, 100, 100); // 边框颜色
        private Color ShadowColor = Color.FromArgb(50, 0, 0, 0); // 半透明阴影
        private Color MainBoardColor = Color.FromArgb(60, 60, 60); // 主框背景颜色
        private Color InfoBorderColor = Color.FromArgb(100, 100, 100);

        private static Color MenuSelectColor = Color.FromArgb(70, 70, 70); // 菜单选中颜色
        private static Color MenuHoverColor = Color.FromArgb(80, 80, 80); // 菜单悬停颜色
        private static Color MenuUnSelectColor = Color.FromArgb(45, 45, 45);
        private static Color SepColor = Color.FromArgb(100, 100, 100);

        public RomList()
        {
            InitializeComponent();

            scrollBarBounds = new Rectangle(
                ClientRectangle.Width - ScrollBarWidth - ScrollBarMargin,
                ScrollBarMargin,
                ScrollBarWidth,
                Math.Max(ThumbMinSize * 2, ClientRectangle.Height - 2 * ScrollBarMargin)
            );

            // 初始化滑块尺寸和位置
            thumbSize = scrollBarBounds.Height;
            thumbPosition = 0;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();

            DrawMode = DrawMode.OwnerDrawVariable;
            BackColor = MainBackColor;
            ForeColor = TextColor;
            ItemHeight = 85;

            DefaultIcon = GetDefaultExeIcon();

            MouseMove += RomList_MouseMove;
            MouseLeave += RomList_MouseLeave;
        }

        public void ScanPath(string Folder, string CacheFolder, int MaxCount = int.MaxValue)
        {
            Entries.Clear();

            try
            {
                var CheckList = new List<string>();

                foreach (var File in Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories))
                {
                    switch (Path.GetExtension(File).ToLowerInvariant())
                    {
                        case ".iso":
                        case ".cso":
                        case ".dax":
                        case ".pbp":
                            {
                                CheckList.Add(File);
                            }
                            break;
                    }
                    if (CheckList.Count > MaxCount) break;
                }

                int Current = 0;
                int Total = CheckList.Count;

                Parallel.ForEach(CheckList, (IsoFile) =>
                {
                    try
                    {
                        if (Progress != null) Progress(IsoFile, Current, Total);

                        var Hash = GetHash(IsoFile);
                        try { Directory.CreateDirectory(CacheFolder + "/romlist"); } catch { }
                        var CacheFile = CacheFolder + "/romlist/" + Hash + ".xml";

                        GameEntry Entry;
                        bool Cached = false;

                        if (File.Exists(CacheFile))
                        {
                            Entry = (GameEntry)Serializer.Deserialize(File.OpenRead(CacheFile));
                            Cached = true;
                        }
                        else
                        {
                            Entry = HandleIso(IsoFile);
                            if (Entry == null) return;

                            using (var CacheFileStream = File.OpenWrite(CacheFile))
                            {
                                Serializer.Serialize(CacheFileStream, Entry);
                            }
                        }

                        lock (Entries)
                        {
                            Entries.Add(Entry);
                            Items.Add(Entry);

                            if (EntryAdded != null && Entry != null)
                            {
                                EntryAdded(Entry, Cached);
                            }
                        }
                    }
                    catch (Exception Exception)
                    {
                        Console.Error.WriteLine(Exception);
                    }
                    Current++;
                });
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }
            if (Progress != null) Progress("Done!", 1, 1);
        }

        public String GetHash(string IsoFile)
        {
            var IsoFileInfo = new FileInfo(IsoFile);
            return BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(IsoFileInfo.FullName + "_" + IsoFileInfo.Length))).Replace("-", "");
        }

        public GameEntry HandleIso(string IsoFile)
        {
            var IsoFileInfo = new FileInfo(IsoFile);
            Psf ParamSfo;
            var Entry = new GameEntry();
            byte[] Icon0Png;
            string UmdData = string.Empty;

            using (var IsoStream = File.OpenRead(IsoFile))
            {
                switch (new FormatDetector().DetectSubType(IsoStream))
                {
                    case FormatDetector.SubType.Pbp:
                        var PBP = new Pbp().Load(File.OpenRead(IsoFile));
                        ParamSfo = new Psf(PBP[Pbp.Types.ParamSfo]);

                        Icon0Png = PBP.ContainsKey(Pbp.Types.Icon0Png) ? PBP[Pbp.Types.Icon0Png].ReadAll() : new byte[0];
                        UmdData = "---";

                        break;
                    case FormatDetector.SubType.Iso:
                    case FormatDetector.SubType.Cso:
                    case FormatDetector.SubType.Dax:
                        using (var Iso = IsoLoader.GetIso(IsoFile))
                        {
                            var FileSystem = new HleIoDriverIso(Iso);

                            if (!FileSystem.FileExists("/PSP_GAME/PARAM.SFO"))
                            {
                                throw (new Exception(String.Format("Not a PSP ISO '{0}'", IsoFile)));
                            }

                            ParamSfo = new Psf(new MemoryStream(FileSystem.OpenRead("/PSP_GAME/PARAM.SFO").ReadAll()));

                            if (FileSystem.FileExists("/UMD_DATA.BIN")) UmdData = FileSystem.OpenRead("/UMD_DATA.BIN").ReadAllContentsAsString();
                            Icon0Png = FileSystem.FileExists("/PSP_GAME/ICON0.PNG") ? FileSystem.OpenRead("/PSP_GAME/ICON0.PNG").ReadAll() : new byte[0];
                            Entry.PatchedWithPrometheus = FileSystem.FileExists("/PSP_GAME/SYSDIR/prometheus.prx") || FileSystem.FileExists("/PSP_GAME/SYSDIR/EBOOT.OLD");
                        }
                        break;
                    default: return null;
                }
            }

            FillGameEntryFromSfo(Entry, ParamSfo);
            Entry.IsoSize = IsoFileInfo.Length;
            Entry.Hash = GetHash(IsoFile);
            Entry.IsoFile = IsoFile;
            Entry.DiscId0 = UmdData.Split('|')[0];
            Entry.Icon0Png = Icon0Png;
            return Entry;
        }

        private void FillGameEntryFromSfo(GameEntry Entry, Psf ParamSfo)
        {
            var Entries = ParamSfo.EntryDictionary;
            Entry.APP_VER = (string)Entries.GetOrDefault("APP_VER", "01.00");
            Entry.BOOTABLE = (int)Entries.GetOrDefault("BOOTABLE", 1) != 0;
            Entry.CATEGORY = (string)Entries.GetOrDefault("CATEGORY", "UG");
            Entry.DISC_ID = (string)Entries.GetOrDefault("DISC_ID", "XXXX99999");
            if (string.IsNullOrWhiteSpace(Entry.DiscId0)) Entry.DiscId0 = Entry.DISC_ID.Substring(0, 4) + "-" + Entry.DISC_ID.Substring(4);
            Entry.DISC_NUMBER = (int)Entries.GetOrDefault("DISC_NUMBER", 1);
            Entry.DISC_TOTAL = (int)Entries.GetOrDefault("DISC_TOTAL", 1);
            Entry.DISC_VERSION = (string)Entries.GetOrDefault("DISC_VERSION", "1.00");
            Entry.DRIVER_PATH = (string)Entries.GetOrDefault("DRIVER_PATH", "");
            Entry.GAMEDATA_ID = (string)Entries.GetOrDefault("GAMEDATA_ID", "XXXX99999");
            Entry.HRKGMP_VER = (int)Entries.GetOrDefault("HRKGMP_VER", 19);
            Entry.PARENTAL_LEVEL = (int)Entries.GetOrDefault("PARENTAL_LEVEL", 5);
            Entry.PSP_SYSTEM_VER = (string)Entries.GetOrDefault("PSP_SYSTEM_VER", "1.00");
            Entry.REGION = (int)Entries.GetOrDefault("REGION", 32768);
            Entry.TITLE = (string)Entries.GetOrDefault("TITLE", "Unknown Title");
            Entry.USE_USB = ((int)Entries.GetOrDefault("USE_USB", 0)) != 0;
            Entry.REGION_STR = RStr(Entry.DISC_ID);
        }

        public string RStr(string DISC_ID)
        {
            switch (DISC_ID[2])
            {
                case 'P':
                case 'J': return " JPN";
                case 'E': return " EUR";
                case 'K': return " KOR";
                case 'U': return " USA";
                case 'A': return "Asia";
                default: return "unknow";
            }
        }

        public string TStr(string DISC_ID)
        {
            switch (DISC_ID[0])
            {
                case 'S': return "CD/DVD";
                case 'U': return "UMD";
                case 'B': return "BluRay";
                case 'N': return "PSN";
                default: return "Unknown";
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            FormattingEnabled = true;
            TabIndex = 0;
            Name = "RomList";
            Size = new System.Drawing.Size(510, 316);
            ResumeLayout(false);
        }

        private void RomList_MouseMove(object sender, MouseEventArgs e)
        {
            int index = myIndexFromPoint(e.Location);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }

            if (isDraggingThumb && scrollBarBounds.Contains(e.Location))
            {
                // 拖动滑块
                thumbPosition = Math.Max(0, Math.Min(e.Y - thumbSize / 2, scrollBarBounds.Height - thumbSize));
                UpdateScrollPosition();
                Invalidate();
            }
        }

        private void RomList_MouseLeave(object sender, EventArgs e)
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = myIndexFromPoint(e.Location);

                if (index != ListBox.NoMatches && index >= 0 && index < Items.Count)
                {
                    SelectedIndex = index;
                }
                else
                {
                    SelectedIndex = -1;
                }

                if (SelectedIndex != -1 && Items.Count > 0)
                {

                }
                else
                {
                    return;
                }
            }

            if (scrollBarBounds.Contains(e.Location))
            {

                if (thumbBounds.Contains(e.Location))
                {
                    isDraggingThumb = true;
                }
                else
                {
                    thumbPosition = Math.Max(0, Math.Min(e.Y - thumbSize / 2, scrollBarBounds.Height - thumbSize));
                    UpdateScrollPosition();
                }
                Invalidate();
                return; // 阻止基类处理
            }


            Point adjustedPoint = new Point(
                Math.Min(e.X, ClientSize.Width - scrollBarBounds.Width - 1),
                e.Y
            );
            base.OnMouseDown(new MouseEventArgs(e.Button, e.Clicks, adjustedPoint.X, adjustedPoint.Y, e.Delta));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            isDraggingThumb = false;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
                return;

            using (var doubleBuffer = new Bitmap(e.Bounds.Width, e.Bounds.Height))
            using (var g = Graphics.FromImage(doubleBuffer))
            {
                var localArgs = new DrawItemEventArgs(
                    g,
                    e.Font,
                    new Rectangle(0, 0, e.Bounds.Width, e.Bounds.Height),
                    e.Index,
                    e.State
                );

                DrawItems(localArgs);

                e.Graphics.DrawImage(doubleBuffer, e.Bounds.Location);
            }
        }

        private void DrawItems(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= this.Items.Count)
                return;

            if (_isScrollBarVisible)
            {
                int contentWidth = ClientSize.Width - scrollBarBounds.Width;
                e = new DrawItemEventArgs(
                    e.Graphics,
                    e.Font,
                    new Rectangle(e.Bounds.X, e.Bounds.Y, contentWidth, e.Bounds.Height), // 修正宽度
                    e.Index,
                    e.State
                );
            }

            Rectangle bounds = e.Bounds;

            bool isHovered = e.Index == _hoverIndex;

            Color rowBackColor = (e.Index % 2 == 0)
                ? ItemBackColor2 // 偶数行稍浅
                : ItemBackColor1; // 奇数行稍深

            if (isHovered)
                rowBackColor = HoverColor;

            using (var backBrush = new SolidBrush(rowBackColor))
            {
                e.Graphics.FillRectangle(backBrush, bounds);
            }

            var game = this.Items[e.Index] as GameEntry;

            if (game.Icon0Png.Length < 100)
                game.CachedBitmap = null;
            else
            {
                game.CachedBitmap = Image.FromStream(new MemoryStream(game.Icon0Png));
            }

            int padding = 5;

            DrawMainBox(e.Graphics, bounds);

            DrawIcon(e.Graphics, game.CachedBitmap ?? DefaultIcon, bounds, 144, 70, padding);

            DrawName(e.Graphics, game.TITLE, bounds, 65, padding);

            DrawInfoBoxes(e.Graphics, game, bounds, 150, padding);

            //if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            //{
            //    DrawSelectionEffect(e.Graphics, bounds);
            //}
        }

        private void DrawMainBox(Graphics g, Rectangle bounds)
        {
            using (var borderPen = new Pen(BorderColor, 2)) // 边框颜色
            using (var shadowBrush = new SolidBrush(ShadowColor)) // 半透明阴影
            //using (var mainBrush = new SolidBrush(MainBoardColor)) // 主框背景颜色
            {
                // 阴影
                g.FillRectangle(shadowBrush, bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
                // 主框
                //g.FillRectangle(mainBrush, bounds.X, bounds.Y, bounds.Width - 2, bounds.Height - 2);
                g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 2, bounds.Height - 2);
            }
        }

        private void DrawIcon(Graphics g, Image icon, Rectangle bounds, int width, int height, int padding)
        {
            int icony = bounds.Top + (bounds.Height - height) / 2;
            if (icon != null)
            {
                g.DrawImage(icon, bounds.Left + padding, icony, width, height);
            }
        }

        private void DrawName(Graphics g, string name, Rectangle bounds, int iconSize, int padding)
        {
            using (var nameFont = new Font("Arial", 13, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                int icony = bounds.Top + (bounds.Height - iconSize) / 2;
                SizeF nameSize = g.MeasureString(name, nameFont);
                g.DrawString(name, nameFont, brush, bounds.Left + 150 + padding * 2, icony + 3);
            }
        }

        private void DrawInfoBoxes(Graphics g, GameEntry game, Rectangle bounds, int iconSize, int padding)
        {
            int startX = bounds.Left + iconSize + 15;
            int startY = bounds.Top + 32;

            DrawInfoBox(g, $"{game.DiscId0}", startX, startY + 13, 10);

            startX = bounds.Right - 340;
            startY = bounds.Bottom - 32;
            if (game.DISC_VERSION != "" && this.Width > 550)
                DrawInfoBox(g, $"Version: {game.DISC_VERSION}", startX - 29, startY, 9);
            DrawInfoBox(g, $"Region: {game.REGION_STR}", startX + 65, startY, 9);
            DrawInfoBox(g, $"Fireware: {game.PSP_SYSTEM_VER}", startX + 160, startY, 9);
            DrawInfoBox(g, $"{TStr(game.DISC_ID)}", startX + 260, startY, 9);
        }

        private void DrawSelectionEffect(Graphics g, Rectangle bounds)
        {
            using (var focusPen = new Pen(SelectionColor, 2))
            {
                g.DrawRectangle(focusPen, bounds.X + 1, bounds.Y + 1, bounds.Width - 3, bounds.Height - 3);
            }
        }

        private void DrawInfoBox(Graphics g, string label, int x, int y, int fontSize = 9)
        {
            using (var boxBrush = new SolidBrush(InfoBackColor)) // 框背景颜色
            using (var borderPen = new Pen(InfoBorderColor)) // 边框颜色
            using (var textBrush = new SolidBrush(Color.White)) // 文字颜色
            using (var font = new Font("Arial", fontSize))
            {
                int padding = 4;

                SizeF labelSize = g.MeasureString(label, font);

                g.FillRectangle(boxBrush, x, y, labelSize.Width + padding * 2, labelSize.Height + padding * 2);
                g.DrawRectangle(borderPen, x, y, labelSize.Width + padding * 2, labelSize.Height + padding * 2);

                g.DrawString(label, font, textBrush, x + padding, y + padding);
            }
        }

        private void DrawInfoBoxValue(Graphics g, string label, string value, int x, int y, int width, int height, int fontsize = 9)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(50, 50, 50))) // 框背景颜色
            using (var borderPen = new Pen(Color.FromArgb(100, 100, 100))) // 边框颜色
            using (var textBrush = new SolidBrush(Color.White)) // 文字颜色
            using (var font = new Font("Arial", fontsize))
            {
                SizeF labelSize = g.MeasureString(label, font);
                SizeF valueSize = g.MeasureString(value, font);

                g.FillRectangle(boxBrush, x, y, labelSize.Width + 2, labelSize.Height + 2);
                g.DrawRectangle(borderPen, x, y, labelSize.Width + 2, labelSize.Height + 2);

                g.DrawString(label, font, textBrush, x + 2, y + (height - labelSize.Height) / 2);

                g.DrawString(value, font, textBrush, x + width - valueSize.Width - 2, y + (height - valueSize.Height) / 2);
            }
        }

        private Image GetDefaultExeIcon()
        {
            try
            {
                Icon defaultIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return defaultIcon.ToBitmap();
            }
            catch
            {
                return new Bitmap(48, 48);
            }
        }

        #region MENU
        public class CustomToolStripRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    using (var brush = new SolidBrush(RomList.MenuSelectColor))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(RomList.MenuUnSelectColor))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Color.White;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                using (var pen = new Pen(RomList.SepColor))
                {
                    e.Graphics.DrawLine(pen, e.Item.ContentRectangle.Left, e.Item.ContentRectangle.Height / 2, e.Item.ContentRectangle.Right, e.Item.ContentRectangle.Height / 2);
                }
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                Rectangle imageMarginBounds = new Rectangle(
                    e.AffectedBounds.Left,
                    e.AffectedBounds.Top,
                    e.AffectedBounds.Width,
                    e.AffectedBounds.Height
                );

                using (var brush = new SolidBrush(RomList.MenuUnSelectColor))
                {
                    e.Graphics.FillRectangle(brush, imageMarginBounds);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (var pen = new Pen(RomList.SepColor))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
            }
        }
        #endregion

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            e.ItemHeight = ItemHeight;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            scrollBarBounds = new Rectangle(
                ClientRectangle.Width - ScrollBarWidth - ScrollBarMargin,
                ScrollBarMargin,
                ScrollBarWidth,
                Math.Max(ThumbMinSize * 2, ClientRectangle.Height - 2 * ScrollBarMargin)
            );

            UpdateScrollBar();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int delta = e.Delta * SystemInformation.MouseWheelScrollLines / 120 * ItemHeight;
            thumbPosition -= delta;

            thumbPosition = Math.Max(0, Math.Min(thumbPosition, scrollBarBounds.Height - thumbSize));

            UpdateScrollPosition();
            Invalidate();
        }

        public new Rectangle GetItemRectangle(int index)
        {
            Rectangle baseRect = base.GetItemRectangle(index);
            // 动态计算内容宽度
            int contentWidth = _isScrollBarVisible ?
                ClientSize.Width - ScrollBarWidth - ScrollBarMargin * 2 :
                ClientSize.Width;
            baseRect.Width = contentWidth;
            return baseRect;
        }

        public int myIndexFromPoint(Point p)
        {
            if (p.X >= ClientSize.Width - scrollBarBounds.Width)
                return -1; // 点击在滚动条区域

            Point adjustedPoint = new Point(
                Math.Min(p.X, ClientSize.Width - scrollBarBounds.Width - 1),
                p.Y
            );

            return base.IndexFromPoint(adjustedPoint);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            int firstVisibleIndex = TopIndex;
            int itemsPerPage = ClientSize.Height / ItemHeight;
            int lastVisibleIndex = Math.Min(Items.Count - 1, firstVisibleIndex + itemsPerPage + 1);

            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                Rectangle itemRect = GetItemRectangle(i);
                if (itemRect.Bottom < 0 || itemRect.Top > ClientSize.Height)
                    continue;

                DrawItemState state = DrawItemState.Default;
                if (i == SelectedIndex)
                    state |= DrawItemState.Selected;

                DrawItemEventArgs args = new DrawItemEventArgs(
                    e.Graphics,
                    Font,
                    itemRect,
                    i,
                    state
                );
                OnDrawItem(args);
            }

            DrawScrollBar(g);
        }

        private void DrawScrollBar(Graphics g)
        {
            if (!_isScrollBarVisible)
                return;

            // 检查滚动条轨道有效性
            if (scrollBarBounds.Width <= 0 || scrollBarBounds.Height <= 0)
                return;

            // 绘制轨道背景
            using (var trackBrush = new SolidBrush(TrackColor))
            using (var borderPen = new Pen(Color.FromArgb(80, 80, 80)))
            {
                g.FillRectangle(trackBrush, scrollBarBounds);
                g.DrawRectangle(borderPen, scrollBarBounds);
            }

            // 初始化滑块区域
            thumbBounds = new Rectangle(
                scrollBarBounds.X + 2,
                scrollBarBounds.Y + thumbPosition,
                scrollBarBounds.Width - 4,
                Math.Max(1, thumbSize) // 确保高度至少为1像素
            );

            // 绘制滑块
            bool isHovered = thumbBounds.Contains(PointToClient(Cursor.Position));
            using (var thumbBrush = new LinearGradientBrush(
                thumbBounds,
                isHovered ? ThumbHoverColor : ThumbColor,
                Color.FromArgb(isHovered ? 80 : 60, 80, 80),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(thumbBrush, thumbBounds);
                using (var highlightPen = new Pen(Color.FromArgb(150, 150, 150)))
                {
                    g.DrawLine(highlightPen, thumbBounds.Left + 1, thumbBounds.Top + 1,
                        thumbBounds.Right - 2, thumbBounds.Top + 1);
                }
            }

            // 绘制滑块边框
            using (var thumbBorderPen = new Pen(Color.FromArgb(180, 180, 180)))
            {
                g.DrawRectangle(thumbBorderPen, thumbBounds);
            }
        }

        public void UpdateScrollBar()
        {

            _isScrollBarVisible = Items.Count * ItemHeight > ClientSize.Height;

            if (!_isScrollBarVisible)
            {
                thumbSize = 0;
                thumbPosition = 0;
                return;
            }

            if (scrollBarBounds.Height <= 0)
            {
                scrollBarBounds.Height = Math.Max(ThumbMinSize * 2, ClientSize.Height);
            }

            int visibleItems = ClientSize.Height / ItemHeight;
            int totalItems = Items.Count;

            thumbSize = Math.Max(
                ThumbMinSize,
                (int)((visibleItems / (float)totalItems) * scrollBarBounds.Height)
            );
            thumbSize = Math.Min(thumbSize, scrollBarBounds.Height);

            int maxTopIndex = Math.Max(0, totalItems - visibleItems);
            if (maxTopIndex == 0)
            {
                thumbPosition = 0;
            }
            else
            {
                thumbPosition = (int)((TopIndex / (float)maxTopIndex) * (scrollBarBounds.Height - thumbSize));
            }

            thumbPosition = Math.Max(0, Math.Min(thumbPosition, scrollBarBounds.Height - thumbSize));
        }

        private void UpdateScrollPosition()
        {
            if (Items.Count == 0)
                return;

            int visibleItems = ClientSize.Height / ItemHeight;
            int totalItems = Items.Count;
            int maxTopIndex = Math.Max(0, totalItems - visibleItems);

            if (scrollBarBounds.Height - thumbSize != 0)
            {
                float ratio = thumbPosition / (float)(scrollBarBounds.Height - thumbSize);
                TopIndex = (int)(ratio * maxTopIndex);
            }

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DefaultIcon.Dispose();
                foreach (var item in Items)
                {
                    if ((item as GameEntry).CachedBitmap != null)
                        (item as GameEntry).CachedBitmap.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {

            base.WndProc(ref m);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.Style &= ~0x00100000; // WS_HSCROLL (水平滚动条)
                createParams.Style &= ~0x00200000; // WS_VSCROLL (垂直滚动条)
                return (createParams);
            }
        }
    }

}
