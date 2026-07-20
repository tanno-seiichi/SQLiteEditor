using System;
using System.Reflection;
using System.Windows;

namespace SQLiteEditor
{
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            Assembly asm = Assembly.GetExecutingAssembly();
            this.Title = asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? asm.GetName().Name;
            this.AppTitle.Content = this.Title;
            this.AppVersion.Content = asm.GetName().Version?.ToString() ?? string.Empty;
            this.AppCopyright.Content = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        }
    }
}
