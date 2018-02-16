namespace ChupooTemplateEngine
{
    class Command
    {
        public static CommandType CurrentCommand;
        public enum CommandType
        {
            FILE_SYSTEM_WATCHER,
            RENDER_ALL,
            RENDER_FILE,
            RENDER_DIRECTORY,
            RENDER_TEMPORARILY,
            LAUNCH,
            CLEAR,
            BROWSE,
            EDIT,
            BACKUP,
            LOAD_PROJECT,
            CREATE_PROJECT,
            RENDER_BACKUP,
            LAUNCH_WORDPRESS
        }
    }
}
