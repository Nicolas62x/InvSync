
using System;
using System.IO;
using YamlDotNet.Serialization;

namespace InvSync;
class InvSyncConfig
{
    //Ips that are allowed to connect
    public string[] IPs = { "::ffff:127.0.0.1", "::1" };
    //tcp port to listen to
    public ushort Port = 7342;

    static string ConfigHeader = @"#
#  __  __  _____  _____ _                        __      
# |  \/  |/ ____|/ ____| |                      / _|     
# | \  / | |    | (___ | |__   __ _ _ __ _ __  | |_ _ __ 
# | |\/| | |     \___ \| '_ \ / _` | '__| '_ \ |  _| '__|
# | |  | | |____ ____) | | | | (_| | |  | |_) || | | |   
# |_|  |_|\_____|_____/|_| |_|\__,_|_|  | .__(_)_| |_|
#                                       | |              
#                                       |_|              
#
#IPs: is a list of autorized ips
#Port: is the tcp port on wich the InvSync will listen
#
";

    static Deserializer deserializer = new Deserializer();
    static Serializer serializer = new Serializer();

    public static InvSyncConfig LoadConfig()
    {
        InvSyncConfig conf = new InvSyncConfig();

        try
        {
            conf = deserializer.Deserialize<InvSyncConfig>(File.ReadAllText(InvSync.Path + "/settings.yaml"));
        }
        catch (Exception)
        {
            Logger.LogWarn("Failled to load config, Writting default one in: " + InvSync.Path + "/settings.yaml");

            File.WriteAllText(InvSync.Path + "/settings.yaml", ConfigHeader + serializer.Serialize(conf));
        }

        Logger.Log($"Using config:\n{serializer.Serialize(conf)}");

        return conf;
    }

}
