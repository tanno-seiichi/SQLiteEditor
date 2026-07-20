// ※ コメントは必ず日本語で記述すること

using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace SQLiteEditor
{
    public partial class MainWindow : Window
    {
        [DllImport( "user32.dll", CharSet = CharSet.Auto )]
        private static extern IntPtr SendMessage( IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam );

        [DllImport( "user32.dll", CharSet = CharSet.Auto )]
        private static extern IntPtr CopyIcon( IntPtr hIcon );

        [DllImport( "user32.dll", CharSet = CharSet.Auto )]
        [return: MarshalAs( UnmanagedType.Bool )]
        private static extern bool DestroyIcon( IntPtr hIcon );

        private const uint WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = IntPtr.Zero; // 0
        private static readonly IntPtr ICON_BIG = new IntPtr( 1 ); // 1

        private IntPtr m_copiedHIcon = IntPtr.Zero;
        private IntPtr m_hwnd = IntPtr.Zero;

        /// <summary>
        /// 表示しているウインドウ数の数
        /// </summary>
        public static int m_windowCount = 0;

        /// <summary>
        /// SQLエディタ入力時のタブサイズ指定
        /// </summary>
        public int tabSize { get; set; } = 4;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            /* ウインドウ数カウントアップ */
            MainWindow.m_windowCount++;

            /* 表示初期化 */
            InitializeComponent();
#if false
            this.Icon = Imaging.CreateBitmapSourceFromHIcon(
                SystemIcons.Application.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()) ;
#elif false
            this.Icon = new BitmapImage( new Uri( "pack://application:,,,/image/SQLiteEditor.ico", UriKind.Absolute ) );
#else

#endif

            MouseLeftButtonDown += ( _, __ ) => { DragMove(); };

            /* 設定読み込み */
            var vm = new MainVM();
            vm.Load();
            this.DataContext = vm;
        }

        /// <summary>
        /// ウインドウ起動時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void WindowLoaded( object a_sender, RoutedEventArgs a_e )
        {
            /* ウインドウの角を丸くして影を付ける */
            var hwnd = new WindowInteropHelper(this).Handle;
            int corner = (int)DwmWindowCornerPreferenceEn.Round;
            DwmSetWindowAttribute(
                hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref corner,
                sizeof( int ) );

            Uri uri = new Uri( "pack://application:,,,/Resources/SQLiteEditor.ico", UriKind.Absolute );
            StreamResourceInfo sri = Application.GetResourceStream( uri );
            if( sri != null )
            {
                using( var stream = sri.Stream )
                using( var icon = new Icon( stream ) )
                {
                    IntPtr hIcon = icon.Handle;

                    // HICON をコピーしてプロセス側で管理する（CopyIcon を使う）
                    m_copiedHIcon = CopyIcon( hIcon );

                    // コピーに成功したらコピーしたハンドルを使う（破棄してもコピーは生きる）
                    IntPtr useHIcon = m_copiedHIcon != IntPtr.Zero ? m_copiedHIcon : hIcon;

                    // WPF の Window.Icon に設定（タイトルバーの表示等に使われる）
                    this.Icon = Imaging.CreateBitmapSourceFromHIcon( useHIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions() );

                    // ネイティブアイコンを設定（タスクバー/Alt+Tab）
                    SendMessage( m_hwnd, WM_SETICON, ICON_BIG, useHIcon );
                    SendMessage( m_hwnd, WM_SETICON, ICON_SMALL, useHIcon );
                    // icon は using で Dispose されるが、コピーしたハンドルは引き続き有効
                }
            }

            /* 透過表示設定 */
            this.SetTransparency( Properties.Settings.Default.Transparent );

            /* パスワードメニューのチェック状態設定 */
            this.PasswordMenu.IsChecked = !string.IsNullOrEmpty( Properties.Settings.Default.Password );

            /* SQLエディタにフォーカス設定 */
            this.SqlStmt.Focus();
            Keyboard.Focus( this.SqlStmt );
        }

        /// <summary>
        /// ウインドウ終了時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void WindowClosing( object a_sender, System.ComponentModel.CancelEventArgs a_e )
        {
            if( m_copiedHIcon != IntPtr.Zero )
            {
                // ウィンドウから解除してから破棄
                SendMessage( m_hwnd, WM_SETICON, ICON_BIG, IntPtr.Zero );
                SendMessage( m_hwnd, WM_SETICON, ICON_SMALL, IntPtr.Zero );

                DestroyIcon( m_copiedHIcon );
                m_copiedHIcon = IntPtr.Zero;
            }

            if( 0 >= --MainWindow.m_windowCount )
            {
                /* 最後のウインドウが閉じられた時 */

                /* 設定保存 */
                if( this.DataContext is MainVM vm )
                {
                    vm.Save();
                }

                /* アプリケーション終了 */
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 最小化ボタンクリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void MaximizeClick( object a_sender, RoutedEventArgs a_e )
        {
            if( WindowState == WindowState.Maximized )
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// 最大化ボタンクリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void CloseClick( object a_sender, RoutedEventArgs a_e )
        {
            this.Close();
        }

        /// <summary>
        /// 閉じるボタンクリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void MinimizeClick( object a_sender, RoutedEventArgs a_e )
        {
            WindowState = WindowState.Minimized;
        }
        private void CloseWindowClick( object a_sender, RoutedEventArgs a_e )
        {
            this.Close();
        }

        /// <summary>
        /// 終了メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ShutdownClick( object a_sender, RoutedEventArgs a_e )
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// DBファイル選択メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void SelectFilePathClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                // ファイル選択ダイアログの初期ディレクトリを設定する
                var openFileDialog = new OpenFileDialog()
                {
                    InitialDirectory = this.GetExistPath( vm.dbFilePath )
                };            

                // ファイル選択ダイアログを開く
                if( openFileDialog.ShowDialog().Value )
                {
                    vm.dbFilePath = openFileDialog.FileName;
                }
            }
        }

        /// <summary>
        /// 指定されたファイルパスまたは存在する親ディレクトリのパスを取得する
        /// </summary>
        /// <param name="a_filePath">ファイルパス</param>
        /// <returns>指定されたファイルパスまたは存在する親ディレクトリのパス</returns>
        private string GetExistPath( string a_filePath )
        {
            if( File.Exists( a_filePath ) )
            {
                // ファイルが存在する場合はそのまま返す
                return a_filePath;
            }
            else if( string.IsNullOrEmpty( a_filePath ) )
            {
                // ファイルパスが空の場合はデスクトップを返す
                return Environment.GetFolderPath( Environment.SpecialFolder.Desktop ); ;
            }
            else
            {
                // ファイルが存在しない場合は存在する親ディレクトリを返す
                string parentDir = Path.GetDirectoryName( a_filePath );
                while( !Directory.Exists( parentDir ) )
                {
                    parentDir = Directory.GetParent( parentDir ).FullName;

                    // ルートディレクトリまで到達した場合はデスクトップを返す
                    if( string.IsNullOrEmpty( parentDir ) )
                    {
                        parentDir = Environment.GetFolderPath( Environment.SpecialFolder.Desktop ); ;
                        break;
                    }
                }
                return parentDir;
            }
        }

        /// <summary>
        /// DBファイルパスクリアメニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ClearFilePathClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.dbFilePath = string.Empty;
            }
        }

        /// <summary>
        /// DBパスワード設定メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void PasswordClick( object a_sender, RoutedEventArgs a_e )
        {
            var inputWindow = new TextInputWindow();
            inputWindow.Owner = this;
            inputWindow.Input.Password = Properties.Settings.Default.Password;
            if( inputWindow.ShowDialog().Value )
            {
                Properties.Settings.Default.Password = inputWindow.Input.Password;
                Properties.Settings.Default.Save();

                this.PasswordMenu.IsChecked = !string.IsNullOrEmpty( Properties.Settings.Default.Password );
            }
        }

        /// <summary>
        /// 新規ウィンドウメニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void NewWindowClick( object a_sender, RoutedEventArgs a_e )
        {
            // 現在のウィンドウの設定を保存
            if( this.DataContext is MainVM vm )
            {
                // 設定保存
                vm.Save();
            }

            // 新しいウィンドウを開く
            var newWindow = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = this.Left + 30,
                Top  = this.Top  + 30
            };

            newWindow.Show();

            // 新しいウィンドウのSQLエディタを空にする
            newWindow.SqlStmt.Clear();
        }

        /// <summary>
        /// Aboutウインドウ表示メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ShowAboutWindowClick( object a_sender, RoutedEventArgs a_e )
        {
            //AboutWindowを表示する
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// テーブル一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ShowTableListClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name from sqlite_master where type = 'table' and tbl_name not like 'sqlite_%' order by tbl_name;" );
            }
        }

        /// <summary>
        /// テーブル定義一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ShowTableDefinitionsClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name, sql from sqlite_master where type in ( 'table', 'index' ) and tbl_name not like 'sqlite_%' order by tbl_name;" );
            }
        }

        /// <summary>
        /// トリガー一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ShowTriggerListClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name, name from sqlite_master where type = 'trigger' and tbl_name not like 'sqlite_%' order by tbl_name, name;" );
            }
        }

        /// <summary>
        /// トリガー定義一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ShowTriggerDefinitionsClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name, name, sql from sqlite_master where type = 'trigger' and tbl_name not like 'sqlite_%' order by tbl_name, name;" );
            }
        }

        /// <summary>
        /// 透過表示メニュークリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void TransparencyClick( object a_sender, RoutedEventArgs a_e )
        {
            if( a_sender is MenuItem menuItem )
            {
                if( menuItem.IsChecked )
                {
                    this.SetTransparency( false );
                }
                else
                {
                    this.SetTransparency( true );
                }
            }
        }

        /// <summary>
        /// SQL実行ボタンクリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void ExecuteClick( object a_sender, RoutedEventArgs a_e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute();
            }
        }

        /// <summary>
        /// SQLエディタの特定キー押下時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void SqlStmtPreviewKeyDown( object a_sender, KeyEventArgs a_e )
        {
            if( Key.Enter == a_e.Key && Keyboard.Modifiers.HasFlag( ModifierKeys.Control ) )
            {
                /* Ctrl + Enter */
                if( this.DataContext is MainVM vm )
                {
                    vm.Execute();
                }
                a_e.Handled = true;
            }
            else if( Key.Tab == a_e.Key )
            {
                /* Tab */
                var textBox = (TextBox)a_sender;
                int caret = textBox.CaretIndex;

                int lineIndex = textBox.GetLineIndexFromCharacterIndex(caret);
                int lineStart = textBox.GetCharacterIndexFromLineIndex(lineIndex);
                int column = caret - lineStart;
                int spaces = tabSize - (column % tabSize);
                if( spaces == 0 ) { spaces = tabSize; }

                textBox.Text = textBox.Text.Insert( caret, new string( ' ', spaces ) );
                textBox.CaretIndex = caret + spaces;

                a_e.Handled = true;
            }
            else
            {
                // Do nothing
            }
        }

        /// <summary>
        /// ウインドウの透過表示を設定する
        /// </summary>
        /// <param name="a_transparent_flg">透過表示にする場合はtrue、通常表示にする場合はfalse</param>
        private void SetTransparency( bool a_transparent_flg )
        {
            Properties.Settings.Default.Transparent = a_transparent_flg;
            if( a_transparent_flg )
            {
                this.Background = new SolidColorBrush( (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString( "#CFFFFFFF" ) );
                this.MinimizeButton.Foreground = System.Windows.Media.Brushes.Gray;
                this.MaximizeButton.Foreground = System.Windows.Media.Brushes.Gray;
                this.CloseButton.Foreground = System.Windows.Media.Brushes.Gray;
                this.TransparencyMenu.IsChecked = true;
            }
            else
            {
                this.Background = new SolidColorBrush( (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString( "White" ) );
                this.MinimizeButton.Foreground = System.Windows.Media.Brushes.Black;
                this.MaximizeButton.Foreground = System.Windows.Media.Brushes.Black;
                this.CloseButton.Foreground = System.Windows.Media.Brushes.Black;
                this.TransparencyMenu.IsChecked = false;
            }
        }


        [DllImport( "dwmapi.dll" )]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE attribute,
            ref int pvAttribute,
            int cbAttribute );

        private enum DWMWINDOWATTRIBUTE
        {
            DWMWA_SYSTEMBACKDROP_TYPE = 38,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        private enum DwmSystemBackdropTypeEn
        {
            Auto = 0,
            None = 1,
            Mica = 2,
            Acrylic = 3,
            MicaAlt = 4
        }

        private enum DwmWindowCornerPreferenceEn
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3
        }

    }
}
