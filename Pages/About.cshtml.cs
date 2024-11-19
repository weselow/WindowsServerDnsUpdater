using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using Markdig;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WindowsServerDnsUpdater.Pages
{
    public class AboutModel : PageModel
    {
        private static string? ReadmePath { get; set; }
        static AboutModel()
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, searchPattern: "readme.md");
            ReadmePath = files.FirstOrDefault();
        }
        public string MarkdownHtml { get; set; } = string.Empty;

        public void OnGet()
        {
            // Читаем содержимое Markdown-файла
            if (string.IsNullOrEmpty(ReadmePath))
            {
                var files = Directory.GetFiles(AppContext.BaseDirectory, 
                    searchPattern: "readme.md", 
                    SearchOption.AllDirectories);
                ReadmePath = files.FirstOrDefault();
                if (string.IsNullOrEmpty(ReadmePath)) return;
            }

            if (System.IO.File.Exists(ReadmePath))
            {
                var markdownText = System.IO.File.ReadAllText(ReadmePath);

                // Преобразуем Markdown в HTML
                MarkdownHtml = Markdown.ToHtml(markdownText);
            }
            else
            {
                MarkdownHtml = "<p>Файл не найден.</p>";
            }
        }
    }
}
