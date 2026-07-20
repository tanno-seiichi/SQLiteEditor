using System.Windows;
using System.Windows.Input;

namespace SQLiteEditor
{
    /// <summary>
    /// TextInputWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TextInputWindow : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TextInputWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ウインドウ起動時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void WindowLoaded( object a_sender, RoutedEventArgs a_e )
        {
            this.Input.Focus();
            Keyboard.Focus( this.Input );
        }

        /// <summary>
        /// Inputコントロールのキーアップイベント
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void InputPreviewKeyUp( object a_sender, KeyEventArgs a_e )
        {
            if( Key.Enter == a_e.Key )
            {
                /*  OKボタンと同じ処理 */
                this.DialogResult = true;
                this.Close();
            }
            else if( Key.Escape == a_e.Key )
            {
                /*  キャンセルボタンと同じ処理 */
                this.DialogResult = false;
                this.Close();
            }
            else
            {
                // Do nothing
            }
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void OkButtonClick( object a_sender, RoutedEventArgs a_e )
        {
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// </summary>
        /// <param name="a_sender"></param>
        /// <param name="a_e"></param>
        private void CancelButtonClick( object a_sender, RoutedEventArgs a_e )
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
