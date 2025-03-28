using CommandLine;

namespace AbyssIrc.Server.Data.Options;

public class AbyssIrcOptions
{
    [Option('r', "root", Required = false, HelpText = "Root directory for the server.")]
    public string RootDirectory { get; set; } = "";

    [Option('c', "config", Required = false, HelpText = "Configuration file for the server.")]
    public string ConfigFile { get; set; } = "config.yml";


    [Option('d', "debug", Required = false, HelpText = "Enable debug logging.")]
    public bool EnableDebug { get; set; } = false;


    [Option('h', "hostname", Required = false, HelpText = "Hostname for the server.")]
    public string HostName { get; set; } = string.Empty;


    [Option('s', "show-header", Required = false, HelpText = "Show header.")]
    public bool ShowHeader { get; set; } = true;
}
