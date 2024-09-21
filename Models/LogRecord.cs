namespace WindowsServerDnsUpdater.Models
{
    public class LogRecord
    {
        /// <summary>
        /// Автоинкрементируемый первичный ключ
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Время записи лога
        /// </summary>
        public DateTime Time { get; set; } = DateTime.Now;

        /// <summary>
        /// Сообщение лога
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Имя логгера
        /// </summary>
        public string Logger { get; set; } = string.Empty;

        /// <summary>
        /// Уровень лога (Info, Error и т.д.)
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// Текст исключения, если есть
        /// </summary>
        public string Exception { get; set; } = string.Empty;
    }
}
