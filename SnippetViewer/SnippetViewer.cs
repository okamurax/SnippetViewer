using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SnippetViewer
{
    public class MainForm : Form
    {
        private Panel leftPanel;
        private Panel centerPanel;
        private Panel rightPanel;
        private Splitter splitter1;
        private Splitter splitter2;

        private ListBox fileListBox;
        private TextBox headingSearchBox;
        private ListBox headingListBox;
        private TextBox contentSearchBox;
        private TextBox contentTextBox;

        private List<SnippetFile> snippetFiles = new List<SnippetFile>();
        private List<Heading> allHeadings = new List<Heading>();
        private List<Heading> filteredHeadings = new List<Heading>();

        private string settingsPath;
        private const int PanelMargin = 6;
        private const int SearchBoxHeight = 28;

        // 復元用の選択状態
        private string savedSelectedFileName = "";
        private string savedSelectedHeadingTitle = "";

        public MainForm()
        {
            settingsPath = Path.Combine(Application.StartupPath, "settings.json");
            InitializeComponents();
            LoadSettings();
            LoadSnippetFiles();

            // 右のテキストボックスの選択を解除
            contentTextBox.SelectionStart = 0;
            contentTextBox.SelectionLength = 0;

            // マウスカーソル位置にウィンドウアイコンが来るようにウィンドウを配置
            PositionWindowAtCursor();

            // 起動時にフォーカスをあてる
            this.Shown += (s, e) => this.Activate();
        }

        private void PositionWindowAtCursor()
        {
            try
            {
                // マウスカーソル位置にウィンドウの左上アイコンが来るように配置
                // タイトルバーのアイコンは通常、左上隅から約15ピクセル内側にある
                Point cursorPos = Cursor.Position;
                var currentScreen = Screen.FromPoint(cursorPos);
                var area = currentScreen.WorkingArea;

                int newX = cursorPos.X - 25;
                int newY = cursorPos.Y - 25;

                // 画面外にはみ出さないように調整
                if (newX + this.Width > area.Right) newX = area.Right - this.Width;
                if (newY + this.Height > area.Bottom) newY = area.Bottom - this.Height;
                if (newX < area.Left) newX = area.Left;
                if (newY < area.Top) newY = area.Top;

                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(newX, newY);
            }
            catch
            {
                // 失敗した場合は標準位置で起動
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void InitializeComponents()
        {
            this.Text = "Snippet Viewer";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(PanelMargin);

            // フォーカスが外れたら終了
            this.Deactivate += (s, e) => Application.Exit();

            // ESCキーで終了
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Application.Exit();
                }
            };

            // 終了時に設定を保存
            this.FormClosing += (s, e) => SaveSettings();

            // 左側パネル（ファイル一覧）
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                Padding = new Padding(0, 0, PanelMargin, 0)
            };

            // 左上のラベル（高さを合わせるため）
            Label fileLabel = new Label
            {
                Dock = DockStyle.Top,
                Text = "ファイル一覧",
                Font = new Font("Yu Gothic UI", 10),
                Height = SearchBoxHeight,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 0, 0, 0)
            };

            fileListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Yu Gothic UI", 10),
                IntegralHeight = false  // 高さをピクセル単位で調整可能に
            };
            fileListBox.SelectedIndexChanged += FileListBox_SelectedIndexChanged;
            fileListBox.DoubleClick += FileListBox_DoubleClick;
            fileListBox.MouseMove += FileListBox_MouseMove;

            leftPanel.Controls.Add(fileListBox);
            leftPanel.Controls.Add(fileLabel);

            // 中央パネル（見出し一覧）
            centerPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 350,
                Padding = new Padding(0, 0, PanelMargin, 0)
            };

            headingSearchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Yu Gothic UI", 10),
                Height = SearchBoxHeight
            };
            headingSearchBox.TextChanged += HeadingSearchBox_TextChanged;

            headingListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Yu Gothic UI", 10),
                IntegralHeight = false  // 高さをピクセル単位で調整可能に
            };
            headingListBox.SelectedIndexChanged += HeadingListBox_SelectedIndexChanged;
            headingListBox.MouseMove += HeadingListBox_MouseMove;

            centerPanel.Controls.Add(headingListBox);
            centerPanel.Controls.Add(headingSearchBox);

            // 右側パネル（内容表示）
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            contentSearchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Yu Gothic UI", 10),
                Height = SearchBoxHeight
            };
            contentSearchBox.TextChanged += ContentSearchBox_TextChanged;

            contentTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Yu Gothic UI", 10),
                ReadOnly = true,
                WordWrap = false
            };
            contentTextBox.Click += ContentTextBox_Click;

            rightPanel.Controls.Add(contentTextBox);
            rightPanel.Controls.Add(contentSearchBox);

            // スプリッター追加
            splitter1 = new Splitter
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = SystemColors.ControlLight
            };
            splitter2 = new Splitter
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = SystemColors.ControlLight
            };

            this.Controls.Add(rightPanel);
            this.Controls.Add(splitter2);
            this.Controls.Add(centerPanel);
            this.Controls.Add(splitter1);
            this.Controls.Add(leftPanel);
        }

        private void LoadSettings()
        {
            if (!File.Exists(settingsPath)) return;

            try
            {
                string json = File.ReadAllText(settingsPath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    this.Size = new Size(settings.WindowWidth, settings.WindowHeight);

                    leftPanel.Width = settings.LeftPanelWidth;
                    centerPanel.Width = settings.CenterPanelWidth;

                    // 選択状態を一時保存（LoadSnippetFilesで復元）
                    savedSelectedFileName = settings.SelectedFileName ?? "";
                    savedSelectedHeadingTitle = settings.SelectedHeadingTitle ?? "";
                }
            }
            catch
            {
                // 設定ファイル読み込みに失敗しても続行
            }
        }

        private void SaveSettings()
        {
            try
            {
                // 選択中のファイル名と見出しタイトルを取得
                string selectedFileName = "";
                string selectedHeadingTitle = "";

                if (fileListBox.SelectedIndex >= 0 && fileListBox.SelectedIndex < snippetFiles.Count)
                {
                    selectedFileName = snippetFiles[fileListBox.SelectedIndex].FileName;
                }
                if (headingListBox.SelectedIndex >= 0 && headingListBox.SelectedIndex < filteredHeadings.Count)
                {
                    selectedHeadingTitle = filteredHeadings[headingListBox.SelectedIndex].Title;
                }

                var settings = new AppSettings
                {
                    WindowWidth = this.Size.Width,
                    WindowHeight = this.Size.Height,
                    LeftPanelWidth = leftPanel.Width,
                    CenterPanelWidth = centerPanel.Width,
                    SelectedFileName = selectedFileName,
                    SelectedHeadingTitle = selectedHeadingTitle
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(settingsPath, json, Encoding.UTF8);
            }
            catch
            {
                // 設定ファイル保存に失敗しても続行
            }
        }

        private void LoadSnippetFiles()
        {
            string exePath = Application.StartupPath;
            string snippetFolderPath = Path.Combine(exePath, "snippets");

            if (!Directory.Exists(snippetFolderPath))
            {
                Directory.CreateDirectory(snippetFolderPath);
                return;
            }

            var files = Directory.GetFiles(snippetFolderPath, "*.md");
            foreach (var file in files)
            {
                var snippetFile = new SnippetFile
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Content = File.ReadAllText(file, Encoding.UTF8)
                };
                snippetFile.Headings = ParseHeadings(snippetFile.Content);
                snippetFiles.Add(snippetFile);
            }

            fileListBox.Items.Clear();
            foreach (var snippetFile in snippetFiles)
            {
                fileListBox.Items.Add(snippetFile.FileName);
            }

            if (fileListBox.Items.Count > 0)
            {
                // 保存されたファイル名に一致するものを探す
                int fileIndex = 0;
                if (!string.IsNullOrEmpty(savedSelectedFileName))
                {
                    for (int i = 0; i < snippetFiles.Count; i++)
                    {
                        if (snippetFiles[i].FileName == savedSelectedFileName)
                        {
                            fileIndex = i;
                            break;
                        }
                    }
                }
                fileListBox.SelectedIndex = fileIndex;

                // 保存された見出しタイトルに一致するものを探す
                if (!string.IsNullOrEmpty(savedSelectedHeadingTitle))
                {
                    for (int i = 0; i < filteredHeadings.Count; i++)
                    {
                        if (filteredHeadings[i].Title == savedSelectedHeadingTitle)
                        {
                            headingListBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private List<Heading> ParseHeadings(string content)
        {
            var headings = new List<Heading>();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var regex = new Regex(@"^(#{1,6})\s+(.+)$");

            for (int i = 0; i < lines.Length; i++)
            {
                var match = regex.Match(lines[i]);
                if (match.Success)
                {
                    int level = match.Groups[1].Value.Length;
                    string title = match.Groups[2].Value;

                    // 見出しの内容を取得（次の見出しまで）
                    int startLine = i + 1;
                    int endLine = startLine;
                    for (int j = startLine; j < lines.Length; j++)
                    {
                        if (regex.IsMatch(lines[j]))
                        {
                            break;
                        }
                        endLine = j + 1;
                    }

                    string headingContent = string.Join(Environment.NewLine,
                        lines.Skip(startLine).Take(endLine - startLine));

                    headings.Add(new Heading
                    {
                        Level = level,
                        Title = title,
                        Content = headingContent.Trim(),
                        LineNumber = i
                    });
                }
            }

            return headings;
        }

        private string GetIndent(int level)
        {
            // 見出し1はインデントなし、見出し2は全角スペース1つ、見出し3は全角スペース2つ...
            return new string('　', level - 1);
        }

        private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileListBox.SelectedIndex < 0) return;

            var selectedFile = snippetFiles[fileListBox.SelectedIndex];
            allHeadings = selectedFile.Headings;
            UpdateHeadingList();
            contentTextBox.Clear();
            headingSearchBox.Clear();
            contentSearchBox.Clear();
        }

        private void FileListBox_DoubleClick(object sender, EventArgs e)
        {
            if (fileListBox.SelectedIndex < 0) return;

            var selectedFile = snippetFiles[fileListBox.SelectedIndex];
            try
            {
                // Ctrlキーが押されている場合はフォルダを開く
                if (Control.ModifierKeys == Keys.Control)
                {
                    string folderPath = Path.GetDirectoryName(selectedFile.FilePath);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{selectedFile.FilePath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // 通常のダブルクリックはファイルを開く
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = selectedFile.FilePath,
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // ファイルを開けない場合は無視
            }
        }

        private void FileListBox_MouseMove(object sender, MouseEventArgs e)
        {
            int index = fileListBox.IndexFromPoint(e.Location);
            if (index >= 0 && index != fileListBox.SelectedIndex)
            {
                fileListBox.SelectedIndex = index;
            }
        }

        private void HeadingListBox_MouseMove(object sender, MouseEventArgs e)
        {
            int index = headingListBox.IndexFromPoint(e.Location);
            if (index >= 0 && index != headingListBox.SelectedIndex)
            {
                headingListBox.SelectedIndex = index;
            }
        }

        private void UpdateHeadingList()
        {
            string filter = headingSearchBox.Text.ToLower();
            filteredHeadings = string.IsNullOrEmpty(filter)
                ? allHeadings.ToList()
                : allHeadings.Where(h => h.Title.ToLower().Contains(filter)).ToList();

            headingListBox.Items.Clear();
            foreach (var heading in filteredHeadings)
            {
                headingListBox.Items.Add(GetIndent(heading.Level) + heading.Title);
            }
        }

        private void HeadingSearchBox_TextChanged(object sender, EventArgs e)
        {
            UpdateHeadingList();
        }

        private void HeadingListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (headingListBox.SelectedIndex < 0) return;

            var selectedHeading = filteredHeadings[headingListBox.SelectedIndex];
            contentTextBox.Text = selectedHeading.Content;
            contentSearchBox.Clear();
        }

        private void ContentSearchBox_TextChanged(object sender, EventArgs e)
        {
            string filter = contentSearchBox.Text.ToLower();
            if (string.IsNullOrEmpty(filter))
            {
                // フィルタが空なら元の内容を表示
                if (headingListBox.SelectedIndex >= 0 && headingListBox.SelectedIndex < filteredHeadings.Count)
                {
                    contentTextBox.Text = filteredHeadings[headingListBox.SelectedIndex].Content;
                }
                return;
            }

            // 内容内で検索してハイライト（簡易版：該当行のみ表示）
            if (headingListBox.SelectedIndex >= 0 && headingListBox.SelectedIndex < filteredHeadings.Count)
            {
                var content = filteredHeadings[headingListBox.SelectedIndex].Content;
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                var matchedLines = lines.Where(l => l.ToLower().Contains(filter));
                contentTextBox.Text = string.Join(Environment.NewLine, matchedLines);
            }
        }

        private void ContentTextBox_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(contentTextBox.Text)) return;

            try
            {
                // カーソル位置から行番号を取得
                int charIndex = contentTextBox.SelectionStart;
                int lineIndex = contentTextBox.GetLineFromCharIndex(charIndex);

                // 行の内容を取得
                var lines = contentTextBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                if (lineIndex >= 0 && lineIndex < lines.Length)
                {
                    string line = lines[lineIndex];
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // クリックした行から空白行が出るまでをまとめてコピー
                        var linesToCopy = new List<string>();
                        linesToCopy.Add(line);

                        // 下の行も空白行が出るまで追加
                        for (int i = lineIndex + 1; i < lines.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i]))
                            {
                                break;
                            }
                            linesToCopy.Add(lines[i]);
                        }

                        string textToCopy = string.Join(Environment.NewLine, linesToCopy);
                        Clipboard.SetText(textToCopy);
                    }
                }
            }
            catch
            {
                // クリップボードアクセス失敗時は無視
            }
        }
    }

    public class AppSettings
    {
        public int WindowWidth { get; set; } = 1200;
        public int WindowHeight { get; set; } = 700;
        public int LeftPanelWidth { get; set; } = 200;
        public int CenterPanelWidth { get; set; } = 350;
        public string SelectedFileName { get; set; } = "";
        public string SelectedHeadingTitle { get; set; } = "";
    }

    public class SnippetFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
        public List<Heading> Headings { get; set; } = new List<Heading>();
    }

    public class Heading
    {
        public int Level { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int LineNumber { get; set; }
    }

    static class Program
    {
        private static Mutex mutex;

        [STAThread]
        static void Main()
        {
            // 複数起動を防止
            bool createdNew;
            mutex = new Mutex(true, "SnippetViewer_SingleInstance", out createdNew);
            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
