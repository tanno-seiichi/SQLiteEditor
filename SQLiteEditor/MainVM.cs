using System;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace SQLiteEditor
{
    internal class MainVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged( string a_propertyName )
        {
            this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( a_propertyName ) );
        }

        public string appTitle { get; set; } = string.Empty;

        public string statusMessage { get; set; } = string.Empty;

        public string sqlExecuted { get; set; } = string.Empty;

        private string m_dbFilePath = string.Empty;    
        public string dbFilePath
        { 
            get
            {
                return this.m_dbFilePath;
            }
            set
            {
                this.m_dbFilePath = value;
                this.appTitle = Assembly.GetExecutingAssembly().GetName().Name + " v" + Assembly.GetExecutingAssembly().GetName().Version + " [" + ( string.IsNullOrEmpty( this.m_dbFilePath ) ? "DBファイル未指定" : this.m_dbFilePath ) + "]";
                this.RaisePropertyChanged( nameof( this.appTitle ) ); 
            } 
        }

        public string sqlStmt { get; set; } = string.Empty;

        public DataView dataList { get; private set; } = new DataView();

        public void Execute( string a_sqlStmt = "" )
        {
            // DBファイルの存在チェック
            if( !File.Exists( this.dbFilePath ) )
            {
                if( string.IsNullOrEmpty( this.dbFilePath ) )
                {
                    MessageBox.Show( "DBファイルが指定されていません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error );
                    this.statusMessage = "DBファイルが指定されていません。";
                }
                else
                {
                    MessageBox.Show( "指定されたDBファイルが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error );
                    this.statusMessage = "指定されたDBファイルが存在しません。";
                }
                return;
            }

            this.statusMessage = string.Empty;

            a_sqlStmt = string.IsNullOrEmpty( a_sqlStmt ) ? this.sqlStmt : a_sqlStmt;
            this.sqlExecuted = " \"" + Regex.Replace( a_sqlStmt, @"\r\n|\r|\n", " " ).Trim() + "\"";

            if( !string.IsNullOrEmpty( this.dbFilePath ) && !string.IsNullOrEmpty( a_sqlStmt ) )
            {
                string connectionString = new SQLiteConnectionStringBuilder()
                {
                    DataSource = this.dbFilePath,
                    Password = Properties.Settings.Default.Password,
                    SyncMode = SynchronizationModes.Off,
                    JournalMode = SQLiteJournalModeEnum.Wal,
                    BusyTimeout = 3000
                }
                .ToString();

                using( var conn = new SQLiteConnection( connectionString ) )
                {
                    try
                    {
                        conn.Open();
                        using( var cmd = new SQLiteCommand( a_sqlStmt, conn ) )
                        {
                            bool isQuery_flg = false;
                            using( var reader = cmd.ExecuteReader() )
                            {
                                isQuery_flg = reader.HasRows;
                            }

                            if( isQuery_flg )
                            {
                                /* SQLがクエリーステートメントの場合 */
                                using( var adapter = new SQLiteDataAdapter( cmd ) )
                                {
                                    DataTable table = new DataTable();
                                    int result = adapter.Fill( table );
                                    this.statusMessage = $"取得件数: {result} 件";
 
                                    this.dataList = table.DefaultView;
                                    this.RaisePropertyChanged( nameof( this.dataList ) );
                                }
                            }
                            else
                            {
                                /* SQLが非クエリーステートメントの場合 */
                                int result = cmd.ExecuteNonQuery();
                                this.statusMessage = $"影響件数: {result} 件";
                            }
                        }
                    }
                    catch( Exception ex )
                    {
                        MessageBox.Show( ex.Message, Assembly.GetExecutingAssembly().GetName().Name );
                    }
                }
            }

            this.RaisePropertyChanged( nameof( this.statusMessage ) );
            this.RaisePropertyChanged( nameof( this.sqlExecuted ) );
        }

        public void Load()
        {
            this.dbFilePath = Properties.Settings.Default.DbFilePath;
            this.sqlStmt = Properties.Settings.Default.SqlStmt;
            this.RaisePropertyChanged( nameof( this.sqlStmt ) );
        }

        public void Save()
        {
            Properties.Settings.Default.DbFilePath = this.dbFilePath;
            Properties.Settings.Default.SqlStmt = this.sqlStmt;
            Properties.Settings.Default.Save();
        }

    }
}
