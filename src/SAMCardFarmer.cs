using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SAMCardFarmer
{
    public class BadgeGame
    {
        public int AppId { get; set; }
        public string Title { get; set; }
        public int DropsRemaining { get; set; }
        public string Status { get; set; }

        public BadgeGame(int appId, string title, int drops)
        {
            AppId = appId;
            Title = title;
            DropsRemaining = drops;
            Status = "Готова к запуску";
        }
    }

    public class ListViewItemComparer : IComparer
    {
        private int col;
        private bool ascending;

        public ListViewItemComparer(int column, bool asc)
        {
            col = column;
            ascending = asc;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemA = (ListViewItem)x;
            ListViewItem itemB = (ListViewItem)y;

            string textA = itemA.SubItems.Count > col ? itemA.SubItems[col].Text : "";
            string textB = itemB.SubItems.Count > col ? itemB.SubItems[col].Text : "";

            int result = 0;
            if (col == 1) // AppID
            {
                int numA, numB;
                if (int.TryParse(textA, out numA) && int.TryParse(textB, out numB))
                    result = numA.CompareTo(numB);
                else
                    result = string.Compare(textA, textB);
            }
            else if (col == 2) // Drops remaining
            {
                Match mA = Regex.Match(textA, @"\d+");
                Match mB = Regex.Match(textB, @"\d+");
                int valA = mA.Success ? int.Parse(mA.Value) : 0;
                int valB = mB.Success ? int.Parse(mB.Value) : 0;
                result = valA.CompareTo(valB);
            }
            else // Title or Status
            {
                result = string.Compare(textA, textB, StringComparison.CurrentCultureIgnoreCase);
            }

            return ascending ? result : -result;
        }
    }

    public class InputDialog : Form
    {
        private TextBox txtInput;
        public string InputText { get { return txtInput.Text; } }

        public InputDialog(string prompt, string title, string defaultValue = "")
        {
            this.Text = title;
            this.Size = new Size(420, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(27, 40, 56);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.25f);

            Label lbl = new Label() { Text = prompt, Location = new Point(16, 16), Size = new Size(370, 38), ForeColor = Color.FromArgb(170, 185, 200) };
            txtInput = new TextBox() { Text = defaultValue, Location = new Point(18, 58), Size = new Size(368, 25), BackColor = Color.FromArgb(17, 20, 25), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Button btnOk = new Button() { Text = "ОК", DialogResult = DialogResult.OK, Location = new Point(220, 98), Size = new Size(80, 28), BackColor = Color.FromArgb(46, 139, 87), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            Button btnCancel = new Button() { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(306, 98), Size = new Size(80, 28), BackColor = Color.FromArgb(50, 60, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            this.Controls.Add(lbl);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        public static string Show(IWin32Window owner, string prompt, string title, string defaultValue = "")
        {
            using (InputDialog dlg = new InputDialog(prompt, title, defaultValue))
            {
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.InputText : null;
            }
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int SW_HIDE = 0;

        private Button btnOpenFile;
        private Button btnOpenBrowserBadges;
        private Button btnHelp;
        private TextBox txtSearch;
        private ListView lvGames;
        private Button btnSelectAll;
        private Button btnDeselectAll;
        private Button btnAddManual;
        private Button btnLaunchAll;
        private Button btnStopAll;
        private Label lblRestartInterval;
        private NumericUpDown numRestartInterval;
        private Label lblRestartHint;
        private Label lblCountInfo;
        private RichTextBox rtbLog;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel samStatusLabel;
        private ContextMenuStrip cmsGames;

        private string samPath = "";
        private List<BadgeGame> allLoadedGames = new List<BadgeGame>();
        private Dictionary<int, Process> runningProcesses = new Dictionary<int, Process>();
        private System.Windows.Forms.Timer restartTimer;
        private System.Windows.Forms.Timer windowHideTimer;
        private int sortColumn = 2; // Default sort by drops remaining
        private bool sortAscending = false;

        private FileSystemWatcher fileWatcher;
        private string watchedFilePath = "";
        private System.Threading.Timer reloadDebounceTimer;

        public MainForm()
        {
            try { SetProcessDPIAware(); } catch { }
            InitializeComponent();
            FindSamExecutable();
        }

        private void InitializeComponent()
        {
            this.Text = "SAM Card Farmer — Автозапуск игр Steam с карточками";
            this.Size = new Size(1020, 780);
            this.MinimumSize = new Size(880, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(27, 40, 56);
            this.ForeColor = Color.FromArgb(237, 242, 244);
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            this.AllowDrop = true;

            // Top Panel
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 118;
            topPanel.Padding = new Padding(12);
            topPanel.BackColor = Color.FromArgb(23, 26, 33);
            topPanel.AllowDrop = true;

            btnOpenFile = new Button();
            btnOpenFile.Text = "📂 ОТКРЫТЬ HTML-ФАЙЛ ЗНАЧКОВ";
            btnOpenFile.Location = new Point(12, 10);
            btnOpenFile.Size = new Size(370, 46);
            btnOpenFile.BackColor = Color.FromArgb(46, 139, 87);
            btnOpenFile.ForeColor = Color.White;
            btnOpenFile.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnOpenFile.FlatStyle = FlatStyle.Flat;
            btnOpenFile.Cursor = Cursors.Hand;
            btnOpenFile.Click += new EventHandler(BtnOpenFile_Click);

            btnOpenBrowserBadges = new Button();
            btnOpenBrowserBadges.Text = "🌐 Открыть страницу значков в Steam";
            btnOpenBrowserBadges.Location = new Point(392, 10);
            btnOpenBrowserBadges.Size = new Size(330, 46);
            btnOpenBrowserBadges.BackColor = Color.FromArgb(41, 98, 144);
            btnOpenBrowserBadges.ForeColor = Color.White;
            btnOpenBrowserBadges.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnOpenBrowserBadges.FlatStyle = FlatStyle.Flat;
            btnOpenBrowserBadges.Cursor = Cursors.Hand;
            btnOpenBrowserBadges.Click += new EventHandler((s, e) => {
                try { Process.Start("https://steamcommunity.com/my/badges"); } catch { }
            });

            btnHelp = new Button();
            btnHelp.Text = "❓ ИНСТРУКЦИЯ (РУС / ENG)";
            btnHelp.Location = new Point(732, 10);
            btnHelp.Size = new Size(255, 46);
            btnHelp.BackColor = Color.FromArgb(142, 68, 173);
            btnHelp.ForeColor = Color.White;
            btnHelp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnHelp.FlatStyle = FlatStyle.Flat;
            btnHelp.Cursor = Cursors.Hand;
            btnHelp.Click += new EventHandler(BtnHelp_Click);

            Label lblHint = new Label();
            lblHint.Text = "💡 КАК ПОЛЬЗОВАТЬСЯ: В браузере на странице значков нажмите Ctrl + S и просто перетащите файл мышкой в это окно!";
            lblHint.Location = new Point(12, 63);
            lblHint.AutoSize = true;
            lblHint.ForeColor = Color.FromArgb(102, 192, 244);
            lblHint.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);

            Label lblHint2 = new Label();
            lblHint2.Text = "⚡ АВТО-СЛЕЖЕНИЕ: Программа автоматически следит за файлом — при повторном нажатии Ctrl + S в браузере список обновится сам!";
            lblHint2.Location = new Point(12, 86);
            lblHint2.AutoSize = true;
            lblHint2.ForeColor = Color.FromArgb(170, 185, 200);
            lblHint2.Font = new Font("Segoe UI", 8.75f);

            topPanel.Controls.Add(btnOpenFile);
            topPanel.Controls.Add(btnOpenBrowserBadges);
            topPanel.Controls.Add(btnHelp);
            topPanel.Controls.Add(lblHint);
            topPanel.Controls.Add(lblHint2);

            // Filter bar
            Panel filterPanel = new Panel();
            filterPanel.Dock = DockStyle.Top;
            filterPanel.Height = 36;
            filterPanel.Padding = new Padding(12, 4, 12, 4);
            filterPanel.BackColor = Color.FromArgb(20, 23, 29);

            Label lblFilter = new Label();
            lblFilter.Text = "Поиск:";
            lblFilter.Location = new Point(12, 8);
            lblFilter.AutoSize = true;
            lblFilter.ForeColor = Color.FromArgb(170, 185, 200);

            txtSearch = new TextBox();
            txtSearch.Location = new Point(65, 5);
            txtSearch.Size = new Size(240, 24);
            txtSearch.BackColor = Color.FromArgb(30, 40, 55);
            txtSearch.ForeColor = Color.White;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.TextChanged += new EventHandler((s, e) => { ApplyFilter(); });

            lblCountInfo = new Label();
            lblCountInfo.Text = "Игр: 0 | Выбрано: 0 | Карточек: 0 шт.";
            lblCountInfo.Location = new Point(320, 8);
            lblCountInfo.AutoSize = true;
            lblCountInfo.ForeColor = Color.FromArgb(102, 192, 244);
            lblCountInfo.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);

            filterPanel.Controls.Add(lblFilter);
            filterPanel.Controls.Add(txtSearch);
            filterPanel.Controls.Add(lblCountInfo);

            // Context Menu for Games
            cmsGames = new ContextMenuStrip();
            cmsGames.Items.Add("▶ Запустить эту игру", null, (s, e) => { LaunchSelectedGame(); });
            cmsGames.Items.Add("⏹ Остановить эту игру", null, (s, e) => { StopSelectedGame(); });
            cmsGames.Items.Add(new ToolStripSeparator());
            cmsGames.Items.Add("📋 Скопировать AppID", null, (s, e) => { CopySelectedAppId(); });
            cmsGames.Items.Add("🃏 Открыть значок в браузере", null, (s, e) => { OpenSelectedGameCards(); });
            cmsGames.Items.Add("🌐 Открыть страницу в магазине Steam", null, (s, e) => { OpenSelectedStorePage(); });
            cmsGames.Items.Add(new ToolStripSeparator());
            cmsGames.Items.Add("❌ Исключить из списка", null, (s, e) => { RemoveSelectedGame(); });

            // Games ListView
            lvGames = new ListView();
            lvGames.Dock = DockStyle.Fill;
            lvGames.View = View.Details;
            lvGames.CheckBoxes = true;
            lvGames.FullRowSelect = true;
            lvGames.GridLines = true;
            lvGames.BackColor = Color.FromArgb(17, 20, 25);
            lvGames.ForeColor = Color.White;
            lvGames.Font = new Font("Segoe UI", 9.5f);
            lvGames.AllowDrop = true;
            lvGames.ContextMenuStrip = cmsGames;
            lvGames.ItemChecked += new ItemCheckedEventHandler(LvGames_ItemChecked);
            lvGames.ColumnClick += new ColumnClickEventHandler(LvGames_ColumnClick);
            lvGames.DoubleClick += new EventHandler((s, e) => { ToggleSelectedGame(); });

            lvGames.Columns.Add("Игра", 420);
            lvGames.Columns.Add("AppID", 95);
            lvGames.Columns.Add("Осталось карточек", 150);
            lvGames.Columns.Add("Статус", 280);

            // Action Panel
            Panel actionPanel = new Panel();
            actionPanel.Dock = DockStyle.Bottom;
            actionPanel.Height = 94;
            actionPanel.Padding = new Padding(10);
            actionPanel.BackColor = Color.FromArgb(23, 26, 33);

            btnSelectAll = new Button();
            btnSelectAll.Text = "☑ Выбрать все";
            btnSelectAll.Location = new Point(12, 10);
            btnSelectAll.Size = new Size(115, 28);
            btnSelectAll.BackColor = Color.FromArgb(50, 60, 75);
            btnSelectAll.ForeColor = Color.White;
            btnSelectAll.FlatStyle = FlatStyle.Flat;
            btnSelectAll.Click += new EventHandler((s, e) => { SetAllChecked(true); });

            btnDeselectAll = new Button();
            btnDeselectAll.Text = "☐ Снять выбор";
            btnDeselectAll.Location = new Point(135, 10);
            btnDeselectAll.Size = new Size(115, 28);
            btnDeselectAll.BackColor = Color.FromArgb(50, 60, 75);
            btnDeselectAll.ForeColor = Color.White;
            btnDeselectAll.FlatStyle = FlatStyle.Flat;
            btnDeselectAll.Click += new EventHandler((s, e) => { SetAllChecked(false); });

            btnAddManual = new Button();
            btnAddManual.Text = "➕ Добавить AppID";
            btnAddManual.Location = new Point(258, 10);
            btnAddManual.Size = new Size(135, 28);
            btnAddManual.BackColor = Color.FromArgb(50, 60, 75);
            btnAddManual.ForeColor = Color.White;
            btnAddManual.FlatStyle = FlatStyle.Flat;
            btnAddManual.Click += new EventHandler(BtnAddManual_Click);

            lblRestartInterval = new Label();
            lblRestartInterval.Text = "Авто-перезапуск каждые (мин):";
            lblRestartInterval.Location = new Point(415, 15);
            lblRestartInterval.AutoSize = true;
            lblRestartInterval.ForeColor = Color.FromArgb(170, 185, 200);

            numRestartInterval = new NumericUpDown();
            numRestartInterval.Location = new Point(630, 12);
            numRestartInterval.Size = new Size(55, 24);
            numRestartInterval.Minimum = 0;
            numRestartInterval.Maximum = 360;
            numRestartInterval.Value = 20;
            numRestartInterval.BackColor = Color.FromArgb(38, 54, 76);
            numRestartInterval.ForeColor = Color.White;

            lblRestartHint = new Label();
            lblRestartHint.Text = "(0 = выключен)";
            lblRestartHint.Location = new Point(695, 15);
            lblRestartHint.AutoSize = true;
            lblRestartHint.ForeColor = Color.FromArgb(130, 145, 165);

            btnLaunchAll = new Button();
            btnLaunchAll.Text = "🚀 ЗАПУСТИТЬ ФАРМ (В ФОНЕ)";
            btnLaunchAll.Location = new Point(12, 48);
            btnLaunchAll.Size = new Size(380, 36);
            btnLaunchAll.BackColor = Color.FromArgb(46, 139, 87);
            btnLaunchAll.ForeColor = Color.White;
            btnLaunchAll.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnLaunchAll.FlatStyle = FlatStyle.Flat;
            btnLaunchAll.Cursor = Cursors.Hand;
            btnLaunchAll.Click += new EventHandler(BtnLaunchAll_Click);

            btnStopAll = new Button();
            btnStopAll.Text = "⏹ ОСТАНОВИТЬ ВСЕ";
            btnStopAll.Location = new Point(402, 48);
            btnStopAll.Size = new Size(220, 36);
            btnStopAll.BackColor = Color.FromArgb(178, 34, 34);
            btnStopAll.ForeColor = Color.White;
            btnStopAll.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnStopAll.FlatStyle = FlatStyle.Flat;
            btnStopAll.Cursor = Cursors.Hand;
            btnStopAll.Click += new EventHandler(BtnStopAll_Click);

            actionPanel.Controls.Add(btnSelectAll);
            actionPanel.Controls.Add(btnDeselectAll);
            actionPanel.Controls.Add(btnAddManual);
            actionPanel.Controls.Add(lblRestartInterval);
            actionPanel.Controls.Add(numRestartInterval);
            actionPanel.Controls.Add(lblRestartHint);
            actionPanel.Controls.Add(btnLaunchAll);
            actionPanel.Controls.Add(btnStopAll);

            // Log Area Panel
            Panel logPanel = new Panel();
            logPanel.Dock = DockStyle.Bottom;
            logPanel.Height = 125;
            logPanel.Padding = new Padding(10, 0, 10, 5);
            logPanel.BackColor = Color.FromArgb(23, 26, 33);

            rtbLog = new RichTextBox();
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.ReadOnly = true;
            rtbLog.BackColor = Color.FromArgb(15, 17, 22);
            rtbLog.ForeColor = Color.FromArgb(160, 200, 230);
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Font = new Font("Consolas", 9f);
            logPanel.Controls.Add(rtbLog);

            // Status Strip
            statusStrip = new StatusStrip();
            statusStrip.BackColor = Color.FromArgb(15, 17, 22);
            statusLabel = new ToolStripStatusLabel("Готов к работе. Перетащите сохранённый HTML-файл значков в окно.");
            statusLabel.ForeColor = Color.FromArgb(200, 210, 225);

            samStatusLabel = new ToolStripStatusLabel("SAM: не найден");
            samStatusLabel.ForeColor = Color.FromArgb(243, 156, 18);
            samStatusLabel.IsLink = true;
            samStatusLabel.Click += new EventHandler((s, e) => { EnsureSamExecutable(true); });

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(new ToolStripStatusLabel("  |  ") { ForeColor = Color.FromArgb(70, 80, 95) });
            statusStrip.Items.Add(samStatusLabel);

            // Assemble Form
            this.Controls.Add(lvGames);
            this.Controls.Add(filterPanel);
            this.Controls.Add(actionPanel);
            this.Controls.Add(logPanel);
            this.Controls.Add(statusStrip);
            this.Controls.Add(topPanel);

            // Drag and drop handlers
            this.DragEnter += new DragEventHandler(MainForm_DragEnter);
            this.DragDrop += new DragEventHandler(MainForm_DragDrop);
            lvGames.DragEnter += new DragEventHandler(MainForm_DragEnter);
            lvGames.DragDrop += new DragEventHandler(MainForm_DragDrop);

            // Restart Timer
            restartTimer = new System.Windows.Forms.Timer();
            restartTimer.Tick += new EventHandler(RestartTimer_Tick);

            // Window Hide Timer (ensures SAM windows are completely hidden in background)
            windowHideTimer = new System.Windows.Forms.Timer();
            windowHideTimer.Interval = 100;
            windowHideTimer.Tick += new EventHandler(WindowHideTimer_Tick);
            windowHideTimer.Start();

            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
        }

        private void Log(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(Log), message);
                return;
            }
            string time = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.AppendText(string.Format("[{0}] {1}\n", time, message));
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
            statusLabel.Text = message;
        }

        private void FindSamExecutable()
        {
            // 1. Saved custom config
            string cfgFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sam_path.cfg");
            if (File.Exists(cfgFile))
            {
                try
                {
                    string saved = File.ReadAllText(cfgFile).Trim();
                    if (File.Exists(saved))
                    {
                        samPath = saved;
                        UpdateSamStatus(true);
                        Log("Используется сохранённый SAM.Game.exe: " + samPath);
                        return;
                    }
                }
                catch { }
            }

            // 2. Current directory
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string test1 = Path.Combine(currentDir, "SAM.Game.exe");
            if (File.Exists(test1))
            {
                samPath = test1;
                UpdateSamStatus(true);
                Log("Найден SAM.Game.exe рядом с программой.");
                return;
            }

            // 3. Parent directory
            try
            {
                string parentDir = Path.GetFullPath(Path.Combine(currentDir, ".."));
                string test2 = Path.Combine(parentDir, "SAM.Game.exe");
                if (File.Exists(test2))
                {
                    samPath = test2;
                    UpdateSamStatus(true);
                    Log("Найден SAM.Game.exe в родительской папке: " + samPath);
                    return;
                }
            }
            catch { }

            // 4. Subdirectories (e.g. SteamAchievementManager)
            try
            {
                string[] dirs = Directory.GetDirectories(currentDir, "*SteamAchievementManager*", SearchOption.TopDirectoryOnly);
                foreach (string d in dirs)
                {
                    string test3 = Path.Combine(d, "SAM.Game.exe");
                    if (File.Exists(test3))
                    {
                        samPath = test3;
                        UpdateSamStatus(true);
                        Log("Найден SAM.Game.exe в подпапке: " + samPath);
                        return;
                    }
                }
            }
            catch { }

            UpdateSamStatus(false);
            Log("ВНИМАНИЕ: SAM.Game.exe не найден. Поместите программу в папку с SAM или кликните по статусу внизу для выбора.");
        }

        private void UpdateSamStatus(bool found)
        {
            if (found)
            {
                samStatusLabel.Text = "SAM: Готов (" + Path.GetFileName(samPath) + ")";
                samStatusLabel.ForeColor = Color.FromArgb(46, 204, 113);
            }
            else
            {
                samStatusLabel.Text = "SAM: Не найден (кликните чтобы указать)";
                samStatusLabel.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        private bool EnsureSamExecutable(bool forceChoose = false)
        {
            if (!forceChoose && !string.IsNullOrEmpty(samPath) && File.Exists(samPath))
            {
                return true;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Укажите файл SAM.Game.exe";
            ofd.Filter = "SAM.Game.exe|SAM.Game.exe|Все файлы (*.*)|*.*";
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                samPath = ofd.FileName;
                UpdateSamStatus(true);
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sam_path.cfg"), samPath);
                }
                catch { }
                Log("Установлен путь к SAM: " + samPath);
                return true;
            }

            return false;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                LoadHtmlFromFile(files[0]);
            }
        }

        private void BtnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выберите сохранённую HTML-страницу значков Steam";
            ofd.Filter = "HTML страницы (*.html;*.htm)|*.html;*.htm|Все файлы (*.*)|*.*";

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                LoadHtmlFromFile(ofd.FileName);
            }
        }

        private void LoadHtmlFromFile(string filePath, bool isAutoReload = false)
        {
            try
            {
                if (!isAutoReload) Log("Чтение файла: " + Path.GetFileName(filePath));

                string html = null;
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            html = sr.ReadToEnd();
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(150);
                    }
                }

                if (string.IsNullOrEmpty(html))
                {
                    Log("Файл пуст или заблокирован другим процессом: " + Path.GetFileName(filePath));
                    return;
                }

                if (html.Contains("\uFFFD") && !html.Contains("badge"))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        html = Encoding.Default.GetString(bytes);
                    }
                    catch { }
                }

                int count = ProcessHtmlBadges(html, isAutoReload ? "АВТО-СЛЕЖЕНИЕ" : "ФАЙЛ " + Path.GetFileName(filePath));

                if (!isAutoReload)
                {
                    SetupFileWatcher(filePath);
                }
            }
            catch (Exception ex)
            {
                if (!isAutoReload) MessageBox.Show("Ошибка при чтении файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("Ошибка: " + ex.Message);
            }
        }

        private void SetupFileWatcher(string filePath)
        {
            try
            {
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = false;
                    fileWatcher.Dispose();
                    fileWatcher = null;
                }

                watchedFilePath = filePath;
                string dir = Path.GetDirectoryName(filePath);
                string name = Path.GetFileName(filePath);

                fileWatcher = new FileSystemWatcher(dir, name);
                fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
                fileWatcher.Changed += (s, e) => DebounceReloadFile();
                fileWatcher.Created += (s, e) => DebounceReloadFile();
                fileWatcher.EnableRaisingEvents = true;

                Log("Включено авто-слежение: файл " + name + " отслеживается. При повторном сохранении (Ctrl+S) в браузере список обновится сам!");
            }
            catch (Exception ex)
            {
                Log("Не удалось включить авто-слежение: " + ex.Message);
            }
        }

        private void DebounceReloadFile()
        {
            if (reloadDebounceTimer != null)
            {
                try { reloadDebounceTimer.Dispose(); } catch { }
            }

            reloadDebounceTimer = new System.Threading.Timer(state => {
                try
                {
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke((Action)(() => {
                            if (!string.IsNullOrEmpty(watchedFilePath) && File.Exists(watchedFilePath))
                            {
                                LoadHtmlFromFile(watchedFilePath, isAutoReload: true);
                            }
                        }));
                    }
                }
                catch { }
            }, null, 500, Timeout.Infinite);
        }

        private int ProcessHtmlBadges(string html, string sourceName)
        {
            try
            {
                List<BadgeGame> games = ParseBadgesFromHtml(html);
                List<BadgeGame> withDrops = new List<BadgeGame>();

                foreach (BadgeGame g in games)
                {
                    if (g.DropsRemaining > 0)
                    {
                        withDrops.Add(g);
                    }
                }

                if (withDrops.Count > 0)
                {
                    allLoadedGames = withDrops;
                    ApplyFilter();
                    Log(string.Format("✓ [{0}] УСПЕХ: Найдено {1} игр с доступными карточками!", sourceName, withDrops.Count));
                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                    return withDrops.Count;
                }
                else
                {
                    if (games.Count > 0)
                    {
                        Log(string.Format("[{0}] Найдено {1} значков, но во всех карточки уже получены (0 осталось).", sourceName, games.Count));
                    }
                    else
                    {
                        Log(string.Format("[{0}] Не найдено строк со значками. Проверьте страницу https://steamcommunity.com/my/badges", sourceName));
                    }
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Log("Ошибка обработки HTML: " + ex.Message);
                return 0;
            }
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            try
            {
                string helpFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ИНСТРУКЦИЯ.html");
                if (File.Exists(helpFile))
                {
                    Process.Start(helpFile);
                    return;
                }

                string localFile = Path.Combine(Directory.GetCurrentDirectory(), "ИНСТРУКЦИЯ.html");
                if (File.Exists(localFile))
                {
                    Process.Start(localFile);
                    return;
                }

                MessageBox.Show(
                    "ИНСТРУКЦИЯ / GUIDE:\n\n" +
                    "1. В браузере откройте: https://steamcommunity.com/my/badges\n" +
                    "2. Нажмите Ctrl + S и сохраните страницу как HTML-файл.\n" +
                    "3. Перетащите сохранённый файл мышкой прямо в это окно программы.\n" +
                    "4. Нажмите большую кнопку «🚀 ЗАПУСТИТЬ ФАРМ (В ФОНЕ)».\n\n" +
                    "Игры запустятся скрытно в фоне БЕЗ скачивания, и карточки будут автоматически выпадать в инвентарь Steam!\n\n" +
                    "⚡ АВТО-ОБНОВЛЕНИЕ: Когда карточки выпадут, просто сохраните страницу значков в браузере снова (Ctrl+S) — список обновится сам!",
                    "Инструкция / Guide",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть инструкцию: " + ex.Message, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static List<BadgeGame> ParseBadgesFromHtml(string html)
        {
            List<BadgeGame> list = new List<BadgeGame>();
            Dictionary<int, BadgeGame> uniqueGames = new Dictionary<int, BadgeGame>();

            // Match full badge rows (excluding inner row divs)
            MatchCollection rowMatches = Regex.Matches(html, @"<div[^>]*class=""[^""]*\bbadge_row\b(?!_inner)[^""]*""[\s\S]*?(?=(?:<div[^>]*class=""[^""]*\bbadge_row\b(?!_inner)|<div class=""footer_spacer""|\Z))");
            foreach (Match m in rowMatches)
            {
                string row = m.Value;
                Match gcMatch = Regex.Match(row, @"/gamecards/(\d+)");
                if (!gcMatch.Success) continue;

                int appId = int.Parse(gcMatch.Groups[1].Value);

                Match titleMatch = Regex.Match(row, @"<div class=""badge_title"">\s*([^<]+?)\s*(?:&nbsp;|<span|<)");
                string title = titleMatch.Success ? WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim()) : string.Format("AppID {0}", appId);

                // Russian: "Ещё выпадет карточек: 4", "Выпадет ещё 1 карточка.", English: "4 card drops remaining"
                int drops = 0;
                Match dropMatch = Regex.Match(row, @"(?:Ещё выпадет карточек:\s*(\d+)|Выпадет ещё\s*(\d+)\s*карточ|(\d+)\s*(?:card drops? remaining|карточ\w*\s*осталось|осталось выпадений карточек|drops? remaining))", RegexOptions.IgnoreCase);
                if (dropMatch.Success)
                {
                    string val = dropMatch.Groups[1].Success ? dropMatch.Groups[1].Value : (dropMatch.Groups[2].Success ? dropMatch.Groups[2].Value : dropMatch.Groups[3].Value);
                    drops = int.Parse(val);
                }
                else
                {
                    Match boldMatch = Regex.Match(row, @"class=""progress_info_bold"">(\d+)</span>");
                    if (boldMatch.Success && (row.ToLower().Contains("card") || row.ToLower().Contains("карточ") || row.ToLower().Contains("выпадет") || row.ToLower().Contains("drop")))
                    {
                        drops = int.Parse(boldMatch.Groups[1].Value);
                    }
                }

                if (!uniqueGames.ContainsKey(appId))
                {
                    BadgeGame bg = new BadgeGame(appId, title, drops);
                    uniqueGames[appId] = bg;
                    list.Add(bg);
                }
            }

            return list;
        }

        private void SetAllChecked(bool isChecked)
        {
            lvGames.BeginUpdate();
            foreach (ListViewItem item in lvGames.Items)
            {
                item.Checked = isChecked;
            }
            lvGames.EndUpdate();
            UpdateStats();
        }

        private void LvGames_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateStats();
        }

        private void LvGames_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == sortColumn)
            {
                sortAscending = !sortAscending;
            }
            else
            {
                sortColumn = e.Column;
                sortAscending = true;
            }

            lvGames.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortAscending);
            lvGames.Sort();
        }

        private void UpdateStats()
        {
            int checkedCount = 0;
            int totalDrops = 0;

            foreach (ListViewItem item in lvGames.Items)
            {
                if (item.Checked && item.Tag is BadgeGame)
                {
                    BadgeGame g = (BadgeGame)item.Tag;
                    checkedCount++;
                    totalDrops += g.DropsRemaining;
                }
            }

            lblCountInfo.Text = string.Format("Игр: {0} | Выбрано: {1} | Карточек к выпадению: {2} шт. | Запущено: {3}",
                allLoadedGames.Count, checkedCount, totalDrops, runningProcesses.Count);
        }

        private void ApplyFilter()
        {
            string q = txtSearch.Text.Trim().ToLower();
            lvGames.BeginUpdate();
            lvGames.Items.Clear();

            foreach (BadgeGame g in allLoadedGames)
            {
                if (string.IsNullOrEmpty(q) || g.Title.ToLower().Contains(q) || g.AppId.ToString().Contains(q))
                {
                    ListViewItem item = new ListViewItem(g.Title);
                    item.SubItems.Add(g.AppId.ToString());
                    item.SubItems.Add(string.Format("Осталось: {0}", g.DropsRemaining));
                    item.SubItems.Add(g.Status);
                    item.Tag = g;
                    item.Checked = true;

                    if (runningProcesses.ContainsKey(g.AppId))
                    {
                        item.ForeColor = Color.FromArgb(46, 204, 113);
                    }

                    lvGames.Items.Add(item);
                }
            }

            lvGames.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortAscending);
            lvGames.Sort();
            lvGames.EndUpdate();
            UpdateStats();
        }

        private void BtnAddManual_Click(object sender, EventArgs e)
        {
            string input = InputDialog.Show(this, "Введите AppID игры Steam (число):", "Добавить игру вручную");
            if (string.IsNullOrEmpty(input)) return;

            int appId;
            if (!int.TryParse(input.Trim(), out appId))
            {
                MessageBox.Show("Некорректный AppID. Введите только цифры.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (BadgeGame ex in allLoadedGames)
            {
                if (ex.AppId == appId)
                {
                    MessageBox.Show("Эта игра уже есть в списке: " + ex.Title, "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            string title = string.Format("Игра (AppID {0})", appId);
            BadgeGame bg = new BadgeGame(appId, title, 1);
            allLoadedGames.Add(bg);
            ApplyFilter();
            Log(string.Format("Вручную добавлена игра: {0} (AppID: {1})", title, appId));
        }

        private void LaunchSelectedGame()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g == null) return;

            if (runningProcesses.ContainsKey(g.AppId))
            {
                MessageBox.Show(string.Format("Игра {0} уже запущена!", g.Title), "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureSamExecutable()) return;

            StartSamProcess(g);
        }

        private void StopSelectedGame()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g == null) return;

            if (runningProcesses.ContainsKey(g.AppId))
            {
                StopSamProcess(g.AppId);
            }
            else
            {
                MessageBox.Show(string.Format("Игра {0} не запущена.", g.Title), "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ToggleSelectedGame()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g == null) return;

            if (runningProcesses.ContainsKey(g.AppId))
            {
                StopSamProcess(g.AppId);
            }
            else
            {
                if (!EnsureSamExecutable()) return;
                StartSamProcess(g);
            }
        }

        private void CopySelectedAppId()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g != null)
            {
                Clipboard.SetText(g.AppId.ToString());
                Log(string.Format("Скопирован AppID: {0} ({1})", g.AppId, g.Title));
            }
        }

        private void OpenSelectedGameCards()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g != null)
            {
                string url = string.Format("https://steamcommunity.com/my/gamecards/{0}/", g.AppId);
                try { Process.Start(url); } catch { }
            }
        }

        private void OpenSelectedStorePage()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g != null)
            {
                string url = string.Format("https://store.steampowered.com/app/{0}/", g.AppId);
                try { Process.Start(url); } catch { }
            }
        }

        private void RemoveSelectedGame()
        {
            if (lvGames.SelectedItems.Count == 0) return;
            ListViewItem item = lvGames.SelectedItems[0];
            BadgeGame g = item.Tag as BadgeGame;
            if (g != null)
            {
                if (runningProcesses.ContainsKey(g.AppId))
                {
                    StopSamProcess(g.AppId);
                }
                allLoadedGames.Remove(g);
                ApplyFilter();
                Log(string.Format("Удалена из списка: {0} ({1})", g.Title, g.AppId));
            }
        }

        private void WindowHideTimer_Tick(object sender, EventArgs e)
        {
            if (runningProcesses.Count == 0) return;

            HashSet<uint> pids = new HashSet<uint>();
            foreach (KeyValuePair<int, Process> kvp in runningProcesses)
            {
                try
                {
                    if (!kvp.Value.HasExited)
                    {
                        pids.Add((uint)kvp.Value.Id);
                    }
                }
                catch { }
            }

            if (pids.Count == 0) return;

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (pids.Contains(pid))
                    {
                        ShowWindowAsync(hWnd, SW_HIDE);
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private bool StartSamProcess(BadgeGame g)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = samPath;
                psi.Arguments = g.AppId.ToString();
                psi.WorkingDirectory = Path.GetDirectoryName(samPath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                Process proc = Process.Start(psi);
                if (proc != null)
                {
                    runningProcesses[g.AppId] = proc;
                    g.Status = string.Format("В ФОНЕ (PID {0})", proc.Id);
                    UpdateGameStatus(g.AppId, g.Status, Color.FromArgb(46, 204, 113));
                    Log(string.Format("▶ Запущена игра: {0} ({1}) [PID {2}] (окно скрыто)", g.Title, g.AppId, proc.Id));
                    UpdateStats();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log(string.Format("Ошибка запуска игры {0} ({1}): {2}", g.Title, g.AppId, ex.Message));
            }
            return false;
        }

        private void StopSamProcess(int appId)
        {
            if (runningProcesses.ContainsKey(appId))
            {
                Process p = runningProcesses[appId];
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                        p.WaitForExit(1000);
                    }
                }
                catch { }

                runningProcesses.Remove(appId);
                UpdateGameStatus(appId, "Остановлена", Color.FromArgb(237, 242, 244));
                Log(string.Format("⏹ Остановлена игра AppID: {0}", appId));
                UpdateStats();
            }
        }

        private void UpdateGameStatus(int appId, string status, Color? color = null)
        {
            foreach (ListViewItem item in lvGames.Items)
            {
                BadgeGame g = item.Tag as BadgeGame;
                if (g != null && g.AppId == appId)
                {
                    g.Status = status;
                    if (item.SubItems.Count > 3)
                    {
                        item.SubItems[3].Text = status;
                    }
                    if (color.HasValue)
                    {
                        item.ForeColor = color.Value;
                    }
                    break;
                }
            }
        }

        private void BtnLaunchAll_Click(object sender, EventArgs e)
        {
            List<BadgeGame> toLaunch = new List<BadgeGame>();
            foreach (ListViewItem item in lvGames.Items)
            {
                if (item.Checked && item.Tag is BadgeGame)
                {
                    BadgeGame g = (BadgeGame)item.Tag;
                    if (!runningProcesses.ContainsKey(g.AppId))
                    {
                        toLaunch.Add(g);
                    }
                }
            }

            if (toLaunch.Count == 0)
            {
                MessageBox.Show("Не выбрано ни одной игры для запуска (или все уже запущены).", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureSamExecutable()) return;

            windowHideTimer.Start();

            Log(string.Format("Старт одновременного запуска {0} игр в скрытом фоне...", toLaunch.Count));
            int started = 0;
            foreach (BadgeGame g in toLaunch)
            {
                if (StartSamProcess(g))
                {
                    started++;
                }
                Thread.Sleep(60);
            }

            Log(string.Format("Успешно запущено {0} игр. Окна скрыты — компьютер не нагружается.", started));

            int restartMins = (int)numRestartInterval.Value;
            if (restartMins > 0)
            {
                restartTimer.Interval = restartMins * 60 * 1000;
                restartTimer.Stop();
                restartTimer.Start();
                Log(string.Format("Включён авто-перезапуск каждые {0} мин. для сброса таймера Steam.", restartMins));
            }
            else
            {
                restartTimer.Stop();
            }
        }

        private void RestartTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Log("⏰ [Авто-перезапуск] Мягкий перезапуск запущенных игр для сброса проверки дропа в Steam...");

                List<BadgeGame> toRestart = new List<BadgeGame>();
                foreach (ListViewItem item in lvGames.Items)
                {
                    if (item.Checked && item.Tag is BadgeGame)
                    {
                        toRestart.Add((BadgeGame)item.Tag);
                    }
                }

                if (toRestart.Count == 0)
                {
                    Log("[Авто-перезапуск] Нет выбранных игр для перезапуска.");
                    restartTimer.Stop();
                    return;
                }

                ThreadPool.QueueUserWorkItem(state =>
                {
                    List<Process> procsToKill = new List<Process>();
                    if (this.IsHandleCreated)
                    {
                        this.Invoke((Action)(() =>
                        {
                            foreach (KeyValuePair<int, Process> kvp in runningProcesses)
                            {
                                procsToKill.Add(kvp.Value);
                                UpdateGameStatus(kvp.Key, "Перезапуск...", Color.FromArgb(241, 196, 15));
                            }
                            runningProcesses.Clear();
                            UpdateStats();
                        }));
                    }

                    foreach (Process p in procsToKill)
                    {
                        try
                        {
                            if (!p.HasExited) p.Kill();
                        }
                        catch { }
                    }

                    Thread.Sleep(1200);

                    int restarted = 0;
                    foreach (BadgeGame g in toRestart)
                    {
                        if (this.IsHandleCreated)
                        {
                            this.Invoke((Action)(() =>
                            {
                                if (StartSamProcess(g))
                                {
                                    restarted++;
                                }
                            }));
                        }
                        Thread.Sleep(60);
                    }

                    if (this.IsHandleCreated)
                    {
                        this.Invoke((Action)(() =>
                        {
                            Log(string.Format("✓ [Авто-перезапуск] Успешно перезапущено {0} игр в скрытом фоне.", restarted));
                            UpdateStats();
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                Log("Ошибка авто-перезапуска: " + ex.Message);
            }
        }

        private void BtnStopAll_Click(object sender, EventArgs e)
        {
            restartTimer.Stop();

            int count = runningProcesses.Count;
            foreach (KeyValuePair<int, Process> kvp in runningProcesses)
            {
                try
                {
                    if (!kvp.Value.HasExited)
                    {
                        kvp.Value.Kill();
                    }
                }
                catch { }

                UpdateGameStatus(kvp.Key, "Остановлена");
            }

            runningProcesses.Clear();
            Log(string.Format("Остановлено {0} процессов SAM.", count));
            UpdateStats();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (fileWatcher != null)
            {
                try { fileWatcher.EnableRaisingEvents = false; fileWatcher.Dispose(); } catch { }
                fileWatcher = null;
            }
            if (reloadDebounceTimer != null)
            {
                try { reloadDebounceTimer.Dispose(); } catch { }
                reloadDebounceTimer = null;
            }

            if (runningProcesses.Count > 0)
            {
                DialogResult dr = MessageBox.Show(
                    "Остановить все запущенные игры SAM при выходе?",
                    "Выход",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (dr == DialogResult.Yes)
                {
                    BtnStopAll_Click(null, null);
                }
            }
        }

        public void LoadHtmlFileOnStartup(string path)
        {
            this.Shown += new EventHandler((s, e) =>
            {
                if (File.Exists(path))
                {
                    LoadHtmlFromFile(path);
                }
            });
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm form = new MainForm();
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                form.LoadHtmlFileOnStartup(args[0]);
            }
            Application.Run(form);
        }
    }
}
