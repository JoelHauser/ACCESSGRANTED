namespace ACCESSGRANTED
{
    /// <summary>
    /// Read by the F12 configuration manager by name, through reflection: only the fields used
    /// here. Advanced settings stay hidden until the player ticks "Advanced settings".
    /// </summary>
    internal sealed class ConfigurationManagerAttributes
    {
        public bool? IsAdvanced;
    }
}
