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

        private IntPtr _copiedHIcon = IntPtr.Zero;
        private IntPtr _hwnd = IntPtr.Zero;

        /// <summary>
        /// 表示しているウインドウ数の数
        /// </summary>
        public static int WindowCount = 0;

        /// <summary>
        /// SQLエディタ入力時のタブサイズ指定
        /// </summary>
        public int TabSize { get; set; } = 4;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            /* ウインドウ数カウントアップ */
            MainWindow.WindowCount++;

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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            /* ウインドウの角を丸くして影を付ける */
            var hwnd = new WindowInteropHelper(this).Handle;
            int corner = (int)DwmWindowCornerPreference.Round;
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
                    _copiedHIcon = CopyIcon( hIcon );

                    // コピーに成功したらコピーしたハンドルを使う（破棄してもコピーは生きる）
                    IntPtr useHIcon = _copiedHIcon != IntPtr.Zero ? _copiedHIcon : hIcon;

                    // WPF の Window.Icon に設定（タイトルバーの表示等に使われる）
                    this.Icon = Imaging.CreateBitmapSourceFromHIcon( useHIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions() );

                    // ネイティブアイコンを設定（タスクバー/Alt+Tab）
                    SendMessage( _hwnd, WM_SETICON, ICON_BIG, useHIcon );
                    SendMessage( _hwnd, WM_SETICON, ICON_SMALL, useHIcon );
                    // icon は using で Dispose されるが、コピーしたハンドルは引き続き有効
                }
            }

            _icon.Visibility = Visibility.Collapsed;

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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if( _copiedHIcon != IntPtr.Zero )
            {
                // ウィンドウから解除してから破棄
                SendMessage( _hwnd, WM_SETICON, ICON_BIG, IntPtr.Zero );
                SendMessage( _hwnd, WM_SETICON, ICON_SMALL, IntPtr.Zero );

                DestroyIcon( _copiedHIcon );
                _copiedHIcon = IntPtr.Zero;
            }

            if( 0 >= --MainWindow.WindowCount )
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaximizeClick( object sender, RoutedEventArgs e )
        {
            if( WindowState == WindowState.Maximized )
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// 最大化ボタンクリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseClick( object sender, RoutedEventArgs e )
        {
            this.Close();
        }

        /// <summary>
        /// 閉じるボタンクリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinimizeClick( object sender, RoutedEventArgs e )
        {
            WindowState = WindowState.Minimized;
        }
        private void CloseWindow_Click( object sender, RoutedEventArgs e )
        {
            this.Close();
        }

        /// <summary>
        /// 終了メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shutdown_Click( object sender, RoutedEventArgs e )
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// DBファイル選択メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectFilePath_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                // ファイル選択ダイアログの初期ディレクトリを設定する
                var openFileDialog = new OpenFileDialog()
                {
                    InitialDirectory = this.GetExistPath( vm.DbFilePath )
                };            

                // ファイル選択ダイアログを開く
                if( openFileDialog.ShowDialog().Value )
                {
                    vm.DbFilePath = openFileDialog.FileName;
                }
            }
        }

        /// <summary>
        /// 指定されたファイルパスまたは存在する親ディレクトリのパスを取得する
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>指定されたファイルパスまたは存在する親ディレクトリのパス</returns>
        private string GetExistPath( string filePath )
        {
            if( File.Exists( filePath ) )
            {
                // ファイルが存在する場合はそのまま返す
                return filePath;
            }
            else if( string.IsNullOrEmpty( filePath ) )
            {
                // ファイルパスが空の場合はデスクトップを返す
                return Environment.GetFolderPath( Environment.SpecialFolder.Desktop ); ;
            }
            else
            {
                // ファイルが存在しない場合は存在する親ディレクトリを返す
                string parentDir = Path.GetDirectoryName( filePath );
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearFilePath_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.DbFilePath = string.Empty;
            }
        }

        /// <summary>
        /// DBパスワード設定メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Password_Click( object sender, RoutedEventArgs e )
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewWindow_Click( object sender, RoutedEventArgs e )
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAboutWindow_Click( object sender, RoutedEventArgs e )
        {
            //AboutWindowを表示する
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// テーブル一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowTableList_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name from sqlite_master where type = 'table' and tbl_name not like 'sqlite_%' order by tbl_name;" );
            }
        }

        /// <summary>
        /// テーブル定義一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowTableDefinitions_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name, sql from sqlite_master where type in ( 'table', 'index' ) and tbl_name not like 'sqlite_%' order by tbl_name;" );
            }
        }

        /// <summary>
        /// トリガー一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowTriggerList_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name, name from sqlite_master where type = 'trigger' and tbl_name not like 'sqlite_%' order by tbl_name, name;" );
            }
        }

        /// <summary>
        /// トリガー定義一覧表示メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowTriggerDefinitions_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute( "select tbl_name, name, sql from sqlite_master where type = 'trigger' and tbl_name not like 'sqlite_%' order by tbl_name, name;" );
            }
        }

        /// <summary>
        /// 透過表示メニュークリック時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Transparency_Click( object sender, RoutedEventArgs e )
        {
            if( sender is MenuItem menuItem )
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Execute_Click( object sender, RoutedEventArgs e )
        {
            if( this.DataContext is MainVM vm )
            {
                vm.Execute();
            }
        }

        /// <summary>
        /// SQLエディタの特定キー押下時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SqlStmt_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            if( Key.Enter == e.Key && Keyboard.Modifiers.HasFlag( ModifierKeys.Control ) )
            {
                /* Ctrl + Enter */
                if( this.DataContext is MainVM vm )
                {
                    vm.Execute();
                }
                e.Handled = true;
            }
            else if( Key.Tab == e.Key )
            {
                /* Tab */
                var textBox = (TextBox)sender;
                int caret = textBox.CaretIndex;

                int lineIndex = textBox.GetLineIndexFromCharacterIndex(caret);
                int lineStart = textBox.GetCharacterIndexFromLineIndex(lineIndex);
                int column = caret - lineStart;
                int spaces = TabSize - (column % TabSize);
                if( spaces == 0 ) { spaces = TabSize; }

                textBox.Text = textBox.Text.Insert( caret, new string( ' ', spaces ) );
                textBox.CaretIndex = caret + spaces;

                e.Handled = true;
            }
            else
            {
                // Do nothing
            }
        }

        /// <summary>
        /// ウインドウの透過表示を設定する
        /// </summary>
        /// <param name="transparent">透過表示にする場合はtrue、通常表示にする場合はfalse</param>
        private void SetTransparency( bool transparent )
        {
            Properties.Settings.Default.Transparent = transparent;
            if( transparent )
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

        private enum DwmSystemBackdropType
        {
            Auto = 0,
            None = 1,
            Mica = 2,
            Acrylic = 3,
            MicaAlt = 4
        }

        private enum DwmWindowCornerPreference
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3
        }

    }
}
