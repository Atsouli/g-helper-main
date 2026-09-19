using System;
using System.Windows.Forms;
using GHelper;

class TestRunner {
    [STAThread]
    static void Main() {
        try {
            var form = new SettingsForm();
            Console.WriteLine("Success");
        } catch (Exception e) {
            Console.WriteLine("CRASH:");
            Console.WriteLine(e.ToString());
        }
    }
}
