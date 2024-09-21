using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WindowsServerDnsUpdater.Pages
{
    public class IndexModel : PageModel
    {
        public List<string> LogLines { get; set; } = [];
        public void OnGet()
        {
            LogLines = DataBox.GetLast50LinesReversed();
        }
    }
}
