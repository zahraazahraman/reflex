using Reflex.Data;

namespace Reflex
{
    public partial class App : Application
    {
        // Static accessor so pages can reach the singleton without DI plumbing.
        public static DatabaseService Database { get; private set; } = null!;

        public App(DatabaseService db)
        {
            InitializeComponent();

            Database  = db;
            MainPage  = new AppShell();

            // Initialise DB and seed baselines before any page loads
            _ = db.InitAsync();
        }
    }
}
