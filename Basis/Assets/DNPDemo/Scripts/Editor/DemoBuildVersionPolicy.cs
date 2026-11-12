using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DNPDemo.Editor
{
    /// <summary>
    /// アプリのバージョンを固定し、どのビルドもヘッドセットに入っているアプリへ上書きインストールできるようにする。
    /// Basis はビルドのたびに Android の versionCode を 1 上げる（BasisBuildDialogAndSettings）。
    /// この値は ProjectSettings.asset に保存されるため、ファイルを元に戻したり別の clone でビルドしたりすると番号が下がり、
    /// Android がダウングレードとしてインストールを拒否する。versionCode が同じなら上書きインストールできるので、
    /// 固定値にしておけば常にインストールできる。Basis の処理の後に実行し、値を固定値に戻す。
    /// ストアなどを通して、インストール済みのアプリを置き換える必要が出たら <see cref="AndroidVersionCode"/> を上げる。
    /// </summary>
    public class DemoBuildVersionPolicy : IPreprocessBuildWithReport
    {
        // 固定する前にヘッドセットへ入ったことのある最大の versionCode（223）より大きい値。
        public const int AndroidVersionCode = 224;
        public const string BundleVersion = "2.0.1";

        // Basis のビルド前処理は 0。これより大きい値にすると、その後に実行される。
        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            Apply();
        }

        public static void Apply()
        {
            PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;
            PlayerSettings.bundleVersion = BundleVersion;
        }
    }
}
