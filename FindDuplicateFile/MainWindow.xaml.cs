using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FindDuplicateFile
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {
        public class FileMD5InfoUI : INotifyPropertyChanged,IComparable
        {
            private bool _bIsChecked;
            private int _id;
            private int _rowIndex;
            private string _name;
            private string _path;
            private string _md5;
            private Brush _gridBackground;
            private Brush _duplicateMD5Foreground;
            private Brush _md5Background;

            public FileMD5InfoUI()
            {
                this.MD5Foreground = new SolidColorBrush(Colors.Black);
                this.MD5 = "N/A";
            }

            public bool Checked
            {
                get
                {
                    return this._bIsChecked;
                }
                set
                {
                    this._bIsChecked = value;
                    
                    this.GridBackground = (value? new SolidColorBrush(Colors.LightBlue) : new SolidColorBrush(Colors.White));

                    this.OnPropertyChanged("Checked");
                }
            }

            public Brush GridBackground
            {
                get
                {
                    return this._gridBackground;
                }
                set
                {
                    this._gridBackground = value;

                    this.OnPropertyChanged("GridBackground");
                } 
            }

            public Brush MD5Foreground
            {
                get
                {
                    return this._duplicateMD5Foreground;
                }
                set
                {
                    this._duplicateMD5Foreground = value;

                    this.OnPropertyChanged("MD5Foreground");
                }
            }

            public Brush MD5Background
            {
                get
                {
                    return this._md5Background;
                }
                set
                {
                    this._md5Background = value;

                    this.OnPropertyChanged("MD5Background");
                }
            }

            /// <summary>
            /// 数据库中主键值
            /// </summary>
            public int ID
            {
                get
                {
                    return this._id;
                }
                set
                {
                    this._id = value;

                    this.OnPropertyChanged("ID");
                }
            }

            public int RowIndex
            {
                get
                {
                    return this._rowIndex;
                }
                set
                {
                    this._rowIndex = value;

                    this.OnPropertyChanged("RowIndex");
                }
            }

            public string Name
            {
                get
                {
                    return this._name;
                }
                set
                {
                    this._name = value;

                    this.OnPropertyChanged("Name");
                }
            }

            public string Path
            {
                get
                {
                    return this._path;
                }
                set
                {
                    this._path = value;

                    this.OnPropertyChanged("Path");
                }
            }

            public string MD5
            {
                get
                {
                    return this._md5;
                }
                set
                {
                    this._md5 = value;

                    this.OnPropertyChanged("MD5");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = this.PropertyChanged;
                if (null != handler)
                {
                    handler(this, new PropertyChangedEventArgs(name));                    
                }
            }

            public int CompareTo(object obj)
            {
                return this.Path.CompareTo(((FileMD5InfoUI)obj).Path);
            }
        }

        #region Private Fields

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private static readonly String SQLITEDBNAME = "findduplicatefile.db";
        private static readonly String FILEMD5INFOSTABLENAME = "filemd5infos";
        private static readonly int TIMERALARM = 1000;

        private SQLiteConnection _sqlConnection = null;
        private String _pathFind = null;
        private Thread _threadFind = null;
        private SynchronizationContext _syn = null;
        private double _glColumnOneWidth;
        private double _glColumnTwoWidth;
        private double _glColumnThreeWidth;

        private int _countFind = 0;
        private bool _bIsFinding = false;
        private string _errorInfos = string.Empty;

        private ObservableCollection<FileMD5InfoUI> _ocAllFindFileInfos = new ObservableCollection<FileMD5InfoUI>();

        private string _noticeinfo = string.Empty;

        private System.Threading.Timer _timerNotice = null;

        private Dictionary<string, List<FileMD5InfoUI>> _dicDuplicateMD5 = new Dictionary<string, List<FileMD5InfoUI>>();

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            this._syn = SynchronizationContext.Current;

            this.btBrowser.Click += btBrowser_Click;
            this.btStart.Click += btStart_Click;
            this.btGetAllInfos.Click += btGetAllInfos_Click;
            this.lbAllFileInfos.SizeChanged += lbAllFileInfos_SizeChanged;
            this.btSelectDeuplicate.Click += btSelectDeuplicate_Click;
            this.btCancelSelectDeuplicate.Click += btCancelSelectDeuplicate_Click;
            this.btMoveSelectDeuplicate.Click += btMoveSelectDeuplicate_Click;
            this.btClearDBData.Click += btClearDBData_Click;
            this.btGetDuplicateInfos.Click += btGetDuplicateInfos_Click;

            this._timerNotice = new System.Threading.Timer(this.Timer_NoticeInfo, null, Timeout.Infinite, TIMERALARM);

            this.InitDB();

            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.btGetAllInfos_Click(this, new RoutedEventArgs());
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this._sqlConnection.Close();
        }

        #endregion

        #region Public Property

        public ObservableCollection<FileMD5InfoUI> OCAllFindFileInfos
        {
            get
            {
                return this._ocAllFindFileInfos;
            }
        }

        public double GLColumnOneWidth
        {
            get
            {
                return this._glColumnOneWidth;
            }
            set
            {
                this._glColumnOneWidth = value;

                this.OnPropertyChanged("GLColumnOneWidth");
            }
        }

        public double GLColumnTwoWidth
        {
            get
            {
                return this._glColumnTwoWidth;
            }
            set
            {
                this._glColumnTwoWidth = value;

                this.OnPropertyChanged("GLColumnTwoWidth");
            }
        }

        public double GLColumnThreeWidth
        {
            get
            {
                return this._glColumnThreeWidth;
            }
            set
            {
                this._glColumnThreeWidth = value;

                this.OnPropertyChanged("GLColumnThreeWidth");
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        #endregion

        #region Private Methods

        #region Click Event

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            this._pathFind = this.tbPath.Text.Trim();

            if (String.IsNullOrEmpty(this._pathFind))
            {
                return;                
            }

            if (this._bIsFinding)
            {
                this._bIsFinding = false;
                this.btStart.IsEnabled = false;
            }
            else
            {
                if (null != this._threadFind)
                {
                    return;
                }

                this.tbInfos.Visibility = System.Windows.Visibility.Visible;
                this.tbInfos.Text = "正在查找，请等待...";

                this._timerNotice.Change(TIMERALARM, TIMERALARM);
                this._threadFind = new Thread(new ThreadStart(this.FindDuplicateFile_DoWork));
                this._bIsFinding = true;
                this._threadFind.Start();
                
                this.tbPath.IsEnabled = false;
                this.btBrowser.IsEnabled = false;
                this.spBtn.IsEnabled = false;

                this.btStart.Content = "停止";
            }
        }

        private void btGetAllInfos_Click(object sender, RoutedEventArgs e)
        {
            this.FillAllInfosUI();
        }

        private void btGetDuplicateInfos_Click(object sender, RoutedEventArgs e)
        {
            this.FillAllInfosUI(true);
        }

        private void btBrowser_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    this.tbPath.Text = dialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// 相同MD5的文件只移动一个到指定文件夹，并在文件名上添加上MD5，其它的文件则删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btMoveSelectDeuplicate_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    String basePath = dialog.SelectedPath;

                    List<FileMD5InfoUI> listDuplicate = new List<FileMD5InfoUI>();

                    lock (this.OCAllFindFileInfos)
                    {
                        foreach (var item in this.OCAllFindFileInfos)
                        {
                            if (item.Checked)
                            {
                                listDuplicate.Add(item);
                            }
                        }
                    }

                    using (SQLiteTransaction sqlTransaction = this._sqlConnection.BeginTransaction())
                    {
                        string lastMD5 = null;
                        foreach (var item in listDuplicate)
                        {
                            try
                            {
                                if (lastMD5 != item.MD5)
                                {
                                    //本次MD5和上次不同，则移动此文件到指定文件夹，并加上MD5重命名                                
                                    String destFileName = System.IO.Path.GetFileNameWithoutExtension(item.Name) + "[" + item.MD5 + "]" + System.IO.Path.GetExtension(item.Name);

                                    File.Move(item.Path, System.IO.Path.Combine(basePath, destFileName));
                                    item.Name = destFileName;
                                    item.Path = System.IO.Path.Combine(basePath, destFileName);

                                    SQLiteCommand sqlCmd = new SQLiteCommand(this._sqlConnection);
                                    sqlCmd.Transaction = sqlTransaction;
                                    sqlCmd.CommandText = "UPDATE " + FILEMD5INFOSTABLENAME + " SET name = '" + item.Name + "', path = '" + item.Path + "' WHERE id = " + item.ID;
                                    sqlCmd.ExecuteNonQuery();
                                }
                                else
                                {
                                    //本次MD5和上次相同，则说明有多个重复文件，则删除

                                    File.Delete(item.Path);
                                    this.OCAllFindFileInfos.Remove(item);

                                    SQLiteCommand sqlCmd = new SQLiteCommand(this._sqlConnection);
                                    sqlCmd.Transaction = sqlTransaction;
                                    sqlCmd.CommandText = "DELETE FROM " + FILEMD5INFOSTABLENAME + " WHERE id = " + item.ID;
                                    sqlCmd.ExecuteNonQuery();
                                }

                                lastMD5 = item.MD5;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Move - source: " + item.Path + "; Exception: " + ex.Message);
                            }
                        }

                        sqlTransaction.Commit();
                    }
                }
            }
        }

        private void btCancelSelectDeuplicate_Click(object sender, RoutedEventArgs e)
        {
            lock (this.OCAllFindFileInfos)
            {
                foreach (var item in this.OCAllFindFileInfos)
                {
                    item.Checked = false;
                    item.MD5Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void btSelectDeuplicate_Click(object sender, RoutedEventArgs e)
        {
            foreach (var md5 in this._dicDuplicateMD5.Keys)
            {
                if (this._dicDuplicateMD5[md5].Count > 1)
                {
                    for (int i = 0; i < this._dicDuplicateMD5[md5].Count; i++)
                    {
                        if (0 == i)
                        {
                            this._dicDuplicateMD5[md5][i].MD5Foreground = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            this._dicDuplicateMD5[md5][i].Checked = true;
                        }
                    }
                }
            }
        }

        private void btClearDBData_Click(object sender, RoutedEventArgs e)
        {
            string sql = "DELETE FROM " + FILEMD5INFOSTABLENAME;
            SQLiteCommand cmdQ = new SQLiteCommand(sql, this._sqlConnection);

            cmdQ.ExecuteNonQuery();

            this.OCAllFindFileInfos.Clear();
        }

        #endregion

        /// <summary>
        /// 定时更新当前查找的文件夹
        /// </summary>
        /// <param name="obj"></param>
        private void Timer_NoticeInfo(object obj)
        {
            this._syn.Post(new SendOrPostCallback(o => 
            {
                this.tbInfos.Text = this._noticeinfo; 
            }), null);
        }

        /// <summary>
        /// 建立数据库连接，创建表
        /// </summary>
        private void InitDB()
        {
            try
            {
                string dbPath = "Data Source =" + System.IO.Path.Combine(Environment.CurrentDirectory,SQLITEDBNAME);
                this._sqlConnection = new SQLiteConnection(dbPath);//创建数据库实例，指定文件位置  
                this._sqlConnection.Open();//打开数据库，若文件不存在会自动创建  

                string sql = "CREATE TABLE IF NOT EXISTS " + FILEMD5INFOSTABLENAME + " (id INTEGER PRIMARY KEY AUTOINCREMENT, name text, path text UNIQUE, md5 text);";//建表语句  
                SQLiteCommand cmdCreateTable = new SQLiteCommand(sql, this._sqlConnection);
                cmdCreateTable.ExecuteNonQuery();//如果表不存在，创建数据表  
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitDB: " + ex.Message);
            }
        }

        /// <summary>
        /// 查找数据插入到数据库
        /// </summary>
        /// <param name="infos"></param>
        private void InsertDB(FileMD5InfoUI[] infos)
        {
            try
            {
                using (SQLiteTransaction sqlTransaction = this._sqlConnection.BeginTransaction())//实例化一个事务  
                {
                    foreach (var info in infos)
                    {
                        SQLiteCommand sqlCmd = new SQLiteCommand(this._sqlConnection);
                        sqlCmd.Transaction = sqlTransaction;
                        sqlCmd.CommandText = "insert into " + FILEMD5INFOSTABLENAME + " (name,path,md5) values (@name,@path,@md5)";
                        sqlCmd.Parameters.AddRange(new SQLiteParameter[] {
                            new SQLiteParameter("@name",info.Name),
                            new SQLiteParameter("@path",info.Path),
                            new SQLiteParameter("@md5",info.MD5)
                        });

                        sqlCmd.ExecuteNonQuery();
                    }

                    sqlTransaction.Commit();
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertDB: " + ex.Message);
            }
        }

        private void GetAllInfos()
        {
            try
            {
                this._dicDuplicateMD5.Clear();

                string sql = "select * from " + FILEMD5INFOSTABLENAME + " order by md5, path asc";
                SQLiteCommand cmdQ = new SQLiteCommand(sql, this._sqlConnection);

                SQLiteDataReader reader = cmdQ.ExecuteReader();

                while (reader.Read())
                {
                    FileMD5InfoUI tempUI = new FileMD5InfoUI() { ID = reader.GetInt32(0), Name = reader.GetString(1), Path = reader.GetString(2), MD5 = reader.GetString(3) };
                    if (!this._dicDuplicateMD5.ContainsKey(tempUI.MD5))
                    {
                        this._dicDuplicateMD5[tempUI.MD5] = new List<FileMD5InfoUI>();
                    }

                    this._dicDuplicateMD5[tempUI.MD5].Add(tempUI);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btGetAllInfos_Click: " + ex.Message);
            }
        }

        private void FillAllInfosUI(bool isOnlyDuplicate = false)
        {
            try
            {
                this.GetAllInfos();

                this.OCAllFindFileInfos.Clear();

                int count = 1;
                bool bIsLightGray = false;
                foreach (var md5 in this._dicDuplicateMD5.Keys)
                {
                    if (isOnlyDuplicate)
                    {
                        if (1 == this._dicDuplicateMD5[md5].Count)
                        {
                            continue;
                        }                        
                    }

                    this._dicDuplicateMD5[md5].Sort();

                    foreach (var info in this._dicDuplicateMD5[md5])
                    {
                        info.RowIndex = count++;
                        info.MD5Background = bIsLightGray ? new SolidColorBrush(Colors.LightGray) : new SolidColorBrush(Colors.White);
                        this.OCAllFindFileInfos.Add(info);
                    }

                    bIsLightGray = !bIsLightGray;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btGetAllInfos_Click: " + ex.Message);
            }
        }

        /// <summary>
        /// 开启线程，执行查找
        /// </summary>
        private void FindDuplicateFile_DoWork()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            this._countFind = 0;
            this._errorInfos = string.Empty;

            try
            {
                this.LoopFolderAndGetMD5(new DirectoryInfo(this._pathFind), new string[] { "*.jpg", "*.jpeg","*.png","*.mp4"});
            }
            catch (Exception ex)
            {
                Console.WriteLine("FindDuplicateFile_DoWork: " + ex.Message);
            }
            finally
            {
                watch.Stop();

                this._threadFind = null;
                this._bIsFinding = false;

                this._syn.Post(new SendOrPostCallback(o =>
                {
                    this._timerNotice.Change(Timeout.Infinite, Timeout.Infinite);

                    this._noticeinfo = "找到 " + this._countFind.ToString() + " 个文件，用时 " + watch.Elapsed.TotalMinutes.ToString("F2") + " 分钟\r\n" + this._errorInfos;
                    System.Windows.MessageBox.Show(this._noticeinfo, "提示信息");

                    this.tbInfos.Text = this._noticeinfo;
                    
                    this.spBtn.IsEnabled = true;
                    this.tbPath.IsEnabled = true;
                    this.btBrowser.IsEnabled = true;

                    this.btStart.Content = "查找";
                    this.btStart.IsEnabled = true;
                    this.tbInfos.Visibility = System.Windows.Visibility.Collapsed;

                    this.btGetAllInfos_Click(this, new RoutedEventArgs());
                }), null);
            }
        }

        /// <summary>
        /// 递归遍历文件夹查找
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="filterExts"></param>
        private void LoopFolderAndGetMD5(DirectoryInfo dirInfo, String[] filterExts)
        {
            try
            {
                if (!this._bIsFinding)
                {
                    return;
                }

                this._noticeinfo = dirInfo.FullName;

                List<FileMD5InfoUI> listMD5Infos = new List<FileMD5InfoUI>();

                foreach (var filter in filterExts)
                {
                    if (!this._bIsFinding)
                    {
                        return;
                    }

                    try
                    {
                        FileInfo[] fileInfos = dirInfo.GetFiles(filter);
                        if (null != fileInfos)
                        {
                            foreach (var info in fileInfos)
                            {
                                FileMD5InfoUI md5Info = new FileMD5InfoUI();
                                md5Info.MD5 = this.GetMD5HashFromFile(info.FullName);
                                md5Info.Name = System.IO.Path.GetFileName(info.FullName);
                                md5Info.Path = info.FullName;

                                if (string.IsNullOrEmpty(md5Info.MD5))
                                {
                                    this._errorInfos += info.FullName + "： 获取MD5值失败\r\n";
                                    Console.WriteLine(info.FullName + "： 获取MD5值失败");
                                    continue;
                                }

                                this._countFind++;

                                listMD5Infos.Add(md5Info);
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (listMD5Infos.Count > 0)
                {
                    this.InsertDB(listMD5Infos.ToArray());                    
                }

                DirectoryInfo[] dirInfos = dirInfo.GetDirectories();

                if (null != dirInfos)
                {
                    foreach (var nestDirInfo in dirInfos)
                    {
                        if (!this._bIsFinding)
                        {
                            return;
                        }

                        this.LoopFolderAndGetMD5(nestDirInfo, filterExts);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoopFolderAndGetMD5: " + ex.Message);
            }
        }

        /// <summary>
        /// 针对MP4文件可以过大，导致MD5计算太耗时。
        /// 文件大于200M，读取前面2048字节做MD5计算。
        /// 只是用来判断是否唯一，没必要计算完整的MD5，忽略前2048字节相同，其他部分不同的情况，这种情况太特殊。
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetMD5HashFromFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return String.Empty;
            }

            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);                
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = null;
                if (file.Length > 200 * 1024 *1024)
                {
                    byte[] buffer = new byte[2048];
                    file.Read(buffer, 0, buffer.Length);
                    retVal = md5.ComputeHash(buffer);
                }
                else
                {
                    retVal = md5.ComputeHash(file);
                }

                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("X2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetMD5HashFromFile: FileName = " + fileName + "; Exception: " + ex.Message);
            }

            return String.Empty;
        }

        private void lbAllFileInfos_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (null == this.lbAllFileInfos)
            {
                return;
            }

            double width = this.lbAllFileInfos.ActualWidth;
            if (width > 0)
            {
                width = width - 50 - 80;

                this.GLColumnOneWidth = (width / 4);
                this.GLColumnTwoWidth = (width / 2);
                this.GLColumnThreeWidth = (width / 4) - 20; //滚动条
            }
        }

        #endregion

        #endregion

    }
}
