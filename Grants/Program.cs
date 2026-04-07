try
{
    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
    using (var writer = new StreamWriter(logPath, append: true))
    {
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting game...");
        writer.Flush();
    }
    
    using var game = new Grants.Game1();
    
    using (var writer = new StreamWriter(logPath, append: true))
    {
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Game instance created, running...");
        writer.Flush();
    }
    
    game.Run();
    
    using (var writer = new StreamWriter(logPath, append: true))
    {
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Game completed normally");
        writer.Flush();
    }
}
catch (Exception ex)
{
    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
    using (var writer = new StreamWriter(logPath, append: true))
    {
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR");
        writer.WriteLine($"Type: {ex.GetType().FullName}");
        writer.WriteLine($"Message: {ex.Message}");
        writer.WriteLine($"Stack trace:\n{ex.StackTrace}");
        if (ex.InnerException != null)
        {
            writer.WriteLine($"\nInner exception type: {ex.InnerException.GetType().FullName}");
            writer.WriteLine($"Inner message: {ex.InnerException.Message}");
            writer.WriteLine($"Inner stack:\n{ex.InnerException.StackTrace}");
        }
        writer.Flush();
    }
}
