namespace NetHealth;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Ensure single instance
        using var mutex = new Mutex(true, "NetHealth-SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("NetHealth is already running.", "NetHealth",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new NetHealthWidget());
    }
}
