using Reflex.Data;

namespace Reflex
{
    public partial class App : Application
    {
        public App(DatabaseService db)
        {
            InitializeComponent();

            MainPage = new AppShell();

            // Initialise DB and seed baselines before any page loads
            _ = db.InitAsync();
        }
    }
}
