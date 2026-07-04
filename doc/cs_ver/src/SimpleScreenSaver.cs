using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;


public class SimpleScreenSaver : Form
{
    /**
    <summary>
    スクリーンセーバーの挙動を理解するために作成しました。
    拡張子をEXEからSCRに変更すればスクリーンセーバーとして登録できるようになります。
    Windowsに登録するにはアプリを右クリックして表示されるコンテキストメニューから
    インストールを選びます。
    また、インストールされたスクリーンセーバーは設定メニューからプレビューなどの
    項目を選択できるようになります。
    この時、スクリーンセーバーはWindowsから引数付きで起動されます。
    引数は実際の動作を見て実装しています。

    デバッグ用のConsole.WriteLineを表示させるにはコンソールアプリとしてコンパイル
    し実行はコマンドプロンプトから行います。
    </summary>
    */


    private Random random = new Random();
    private Timer timer;

    private static readonly DateTime StartDT = DateTime.Now;
    private static Point MousePoint = new Point();


    private void timer_Tick(object sender, EventArgs e)
    {
        // OnPaint()の呼び出しもここで行われています。
        this.Invalidate();
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        /**
        <summary>
        一度に複数の図形をランダムな位置に絵画します。
        C#は様々な図形を書くメソッドが用意されています。
        https://learn.microsoft.com/ja-jp/dotnet/api/system.drawing.graphics?view=dotnet-plat-ext-8.0

        背景の透過設定について
        Windowsにインストールして実行されるデスクトップ画面は消されていました。
        プレビューでは消されないようです。
        個人的には透過できたほうが好みなんですが残念です。
        探せば透過する方法もありそうですが、そこまで調べていません。
        </summary>
        */

        // 背景を透過設定します
        this.Opacity = 0.8; // 0.0 - 1.0

        // 背景設定します
        Color backgroundColor = Color.FromArgb(0, 0, 0);
        this.BackColor = backgroundColor;

        // 図形を絵画します
        int numberShapes = 20;
        int sizeMin = this.Width / 30;
        int sizeMax = this.Width / 5;
        int colorMin = 0; // 0 - 256
        int colorMax = 128; // 0 - 256
        for (int i = 0; i < numberShapes; i++)
        {
            int x = random.Next(this.Width);
            int y = random.Next(this.Height);
            int size = random.Next(sizeMin, sizeMax);
            Color color = Color.FromArgb(
                random.Next(colorMin, colorMax),
                random.Next(colorMin, colorMax),
                random.Next(colorMin, colorMax)
            );
            e.Graphics.FillEllipse(new SolidBrush(color), x, y, size, size);
        }
    }


    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        Console.WriteLine("Key Down     : {0}", e.KeyCode);
        Application.Exit();
    }


    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        /**
        <summary>
        通常、OnKeyDownイベントがあれば十分ですがOnKeyUpイベントでも
        解除できるようにします。
        なお、コマンドプロンプトからアプリを実行した時はエンターキー
        を押した瞬間にプログラムは起動します。
        そのままエンターキーを放した時にOnKeyUpイベントが発生します。
        起動直後は判定を回避する処理を入れて対応しています。
        回避に必要な秒数は1秒としていますがもっと短くても良いかもしれません。
        多くのスクリーンセーバーはここに対応していないと思われます。
        個人的にはOnKeyUpもいれるべきだと思います。
        </summary>
        */

        // 起動直後対策 : 起動後、指定ミリ秒判定しません
        int skipMSec = 1000;
        TimeSpan ts = DateTime.Now - StartDT;
        if (ts.TotalMilliseconds < skipMSec) { return; }

        Console.WriteLine("Key Up       : {0}", e.KeyCode);
        Application.Exit();
    }


    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        /**
        <summary>
        TODO : マウスカーソルの位置情報がおかしい。。。
        本イベントをテスト中、マウスカーソルの位置情報を取得したときに
        プログラム開始から最大4回、位置情報に±1の誤差を確認しました。
        結果、プログラム開始直後はマウスを動かしていなくても
        動かした(位置を変更した)と誤検知してしまいます。
        起動直後は位置判定しないことで対策をしました。
        誤検知の回避だけなら100ミリ秒で十分でしたがダブルクリックで
        起動したときに反応しないように大きめに数値と取っています。

        C#でマウスカーソルの位置を取得する方法はいくつかありますが、
        テストした方法はいずれも同様の問題が発生しました。
        Win32APIのGetCursorPos関数でも同様の問題が発生したため根本的な
        解決策は見つかっていません。
        </summary>
        */

        // 起動直後対策 : 起動後、指定ミリ秒判定しません
        int skipMSec = 1000;
        TimeSpan ts = DateTime.Now - StartDT;
        if (ts.TotalMilliseconds < skipMSec) { return; }

        // 開始時のカーソル位置を保存します
        if (MousePoint.IsEmpty) { MousePoint = e.Location; }

        // マウスが動いたか座標をチェックします
        if (MousePoint.X != e.X || MousePoint.Y != e.Y)
        {
            Console.WriteLine(
                "Mouse Move   : X {0} -> {1} / Y {2} -> {3}",
                MousePoint.X, e.X,
                MousePoint.Y, e.Y
            );
            Application.Exit();
        }
    }


    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        Console.WriteLine("Mouse Button : {0}", e.Button);
        Application.Exit();
    }


    private void OnMouseWheel(object sender, MouseEventArgs e)
    {
        Console.WriteLine("Mouse Wheel  : {0}", e.Delta);
        Application.Exit();
    }


    public SimpleScreenSaver()
    {
        // フォームを設定します
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.DoubleBuffered = true;

        // マウスカーソルを非表示にします
        Cursor.Hide();

        // キーボード、マウスイベントを登録します
        this.KeyDown += new KeyEventHandler(OnKeyDown);
        this.KeyUp += new KeyEventHandler(OnKeyUp);
        this.MouseMove += new MouseEventHandler(OnMouseMove);
        this.MouseDown += new MouseEventHandler(OnMouseDown);
        this.MouseWheel += new MouseEventHandler(OnMouseWheel);

        // タイマーイベントを登録します
        int TIMSec = 300;
        timer = new Timer();
        timer.Interval = TIMSec;
        timer.Tick += new EventHandler(timer_Tick);
        timer.Start();
    }


    [STAThread]
    static void Main()
    {
        // 二重起動を抑止します
        string MutexObjectName = "Simple Screen Saver";
        bool createdNew;
        using (Mutex mutex = new Mutex(true, MutexObjectName, out createdNew))
        {
            if (createdNew)
            {
                // コマンドライン引数をチェックします
                string[] args = Environment.GetCommandLineArgs();
                switch (args.Length)
                {
                    case 1: // 引数がない場合:スクリーンセーバーを表示します
                        Application.Run(new SimpleScreenSaver());
                        break;
                    case 2: // 引数がある場合:引数をチェックします
                        // オプション"/C"が指定されるとき後ろに"/C"以外の文字列が
                        // 付与されているため先頭2文字でトリムします
                        // 文字列の意味は調べていません
                        switch (args[1].ToLower().Trim().Substring(0, 2))
                        {
                            case "/s": // show:スクリーンセーバーを表示します
                                Application.Run(new SimpleScreenSaver());
                                break;
                            case "/c": // configure:オプションの表示します
                                MessageBox.Show(
                                    "\n\n\nCopyright (c) 2024 Aromatibus\n",
                                    "シンプル・スクリーンセーバー",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Asterisk
                                );
                                break;
                            case "/p": // preview:プレビューを表示します
                                // プレビューボタンを押すと実際には/Sが指定されてます
                                break;
                            default: // 指定できない引数がある場合はエラーになります
                                Console.WriteLine("指定できない引数が指定されました : {}", args[1]);
                                MessageBox.Show(
                                    "指定できない引数が指定されました",
                                    "SimpleScreenSaver",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Asterisk
                                );
                                break;
                        }
                        break;
                    default: // 複数の引数がある場合はエラーになります
                        Console.WriteLine("複数の引数が指定されました");
                        for (int i = 1; i < args.Length; i++)
                            Console.WriteLine("args[{0}] : {1}", i, args[i]);
                        MessageBox.Show(
                            "指定できない複数の引数が指定されました",
                            "SimpleScreenSaver",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Asterisk
                        );
                        break;
                }
            }
        }
    }
}
