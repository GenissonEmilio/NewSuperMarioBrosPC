namespace MarioPC;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            if (args.Length >= 2 && args[0] == "--verify") return Verification.Run(args[1..]);
            if (args.Length == 2 && args[0] == "--self-test") return Verification.SelfTest(args[1]);
            Application.Run(new MainWindow());
            return 0;
        }
        catch (Exception ex)
        {
            if (args.Length > 0)
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "verification-error.log"), ex.ToString());
                return 1;
            }
            MessageBox.Show("Não foi possível iniciar.\n" + ex.Message, "Mario PC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
