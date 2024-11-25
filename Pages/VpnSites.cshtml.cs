using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WindowsServerDnsUpdater.Data;

namespace WindowsServerDnsUpdater.Pages
{
    public class VpnSitesModel : PageModel
    {
        [BindProperty]
        public string Domain { get; set; } = string.Empty;
        public Dictionary<string, List<string>> DomainList { get; set; } = [];
        public void OnGet()
        {
            //сортируем  
            var keys = DomainCacheOperations.GetDomains()
                .OrderBy(t=>t)
                .ToList();

            //формирум таблицу уникальных доменов
            foreach (var s in keys)
            {
                if (keys.Any(t =>s != t && s.Contains(t))) continue;
                DomainList.TryAdd(s, new());
            }

            foreach (var key in DomainList.Keys)
            {
                DomainList[key] = keys.Where(t => t.Contains(key)).OrderBy(t => t).ToList();
            }
        }

        public IActionResult OnGetDelete(string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Length < 4) return RedirectToPage("./VpnSites");
            DomainCacheOperations.TryRemoveDomain(domain);
            return RedirectToPage("./VpnSites");
        }
        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Domain) || Domain.Length < 4) return Page();

            //если передан url, то выделяем домен
            if (Domain.Contains("http"))
            {
                var domain = Toolbox.ExtractWebsiteAddress(Domain);
                if (!string.IsNullOrEmpty(domain)) Domain = domain;
            }

            DomainCacheOperations.TryAddDomain(Domain);
            return  RedirectToPage("./VpnSites");
        }
    }
}
