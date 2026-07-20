using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace SQLiteEditor
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Handles application startup initialization, including settings upgrade and enabling JIT profile
        /// optimization.
        /// </summary>
        /// <remarks>This method upgrades user settings if they have not been upgraded yet and configures
        /// the JIT profile optimization to use a profile stored in the user's Documents folder. It should be called as
        /// part of the application's startup sequence.</remarks>
        /// <param name="a_e">An object that contains the event data for the startup event.</param>
        protected override void OnStartup( StartupEventArgs a_e )
        {
            // まだUpgradeしていない場合のみ実行
            if( !SQLiteEditor.Properties.Settings.Default.IsUpgraded )
            {
                SQLiteEditor.Properties.Settings.Default.Upgrade();
                SQLiteEditor.Properties.Settings.Default.IsUpgraded = true;
                SQLiteEditor.Properties.Settings.Default.Save();
            }

            // JIT Profile Optimization を有効にするためのコード
            // JIT Profile を保存するディレクトリをユーザーのドキュメントフォルダに設定
            string appName = Assembly.GetExecutingAssembly().GetName().Name;
            string appRoot = System.IO.Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), appName);
            if( !System.IO.Directory.Exists( appRoot ) )
            {
                System.IO.Directory.CreateDirectory( appRoot );
            }
            System.Runtime.ProfileOptimization.SetProfileRoot( appRoot );
            System.Runtime.ProfileOptimization.StartProfile( appName + ".jitprofile" );
            base.OnStartup( a_e );
        }

        // Properties.Settings.Defaultの古いバージョンのデータを移行する

    }
}
