using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NLog;
using WindowsServerDnsUpdater.Data;
using WindowsServerDnsUpdater.Models;

namespace WindowsServerDnsUpdater.Pages
{
    public class SettingsModel : PageModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly LoggingDbContext _context;

        public SettingsModel(LoggingDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Settings Settings { get; set; } = GlobalOptions.Settings;

        public IActionResult OnGetAsync()
        {
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            GlobalOptions.Settings = Settings;

            try
            {
                _context.Update(Settings);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Logger.Error(e,"Ошибка при сохранении настроек в базу - {message}", e.Message);
                throw;
            }

            return RedirectToPage("./Index");
        }

    }
}
