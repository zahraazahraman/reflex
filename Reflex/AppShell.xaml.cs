using Reflex.Pages;

namespace Reflex;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(BriefingPage),   typeof(BriefingPage));
        Routing.RegisterRoute(nameof(StillnessPage),  typeof(StillnessPage));
        Routing.RegisterRoute(nameof(StrikePage),     typeof(StrikePage));
        Routing.RegisterRoute(nameof(AimPage),        typeof(AimPage));
        Routing.RegisterRoute(nameof(ChasePage),      typeof(ChasePage));
        Routing.RegisterRoute(nameof(PulsePage),      typeof(PulsePage));
        Routing.RegisterRoute(nameof(ProcessingPage), typeof(ProcessingPage));
        Routing.RegisterRoute(nameof(ResultPage),     typeof(ResultPage));
        Routing.RegisterRoute(nameof(HistoryPage),    typeof(HistoryPage));
    }
}
