using VRage.Game;

namespace SEPC.Logging
{
    public static class Severity
    {
        public enum Level : byte
        {
            OFF, FATAL, ERROR, WARNING, INFO, DEBUG, TRACE, ALL
        }

        public static string FontForLevel(Level level)
        {
            switch (level)
            {
                case Level.TRACE:
                    return MyFontEnum.Blue;
                case Level.DEBUG:
                    return MyFontEnum.DarkBlue;
                case Level.INFO:
                    return MyFontEnum.Green;
                case Level.WARNING:
                    return MyFontEnum.Red;
                case Level.ERROR:
                    return MyFontEnum.Red;
                case Level.FATAL:
                    return MyFontEnum.Red;
                default:
                    return MyFontEnum.White;
            }
        }
    }
}
