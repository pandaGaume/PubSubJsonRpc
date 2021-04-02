using System.Text.Json;

namespace Samples.Commons.Mqtt
{
    public class MqttClientOptions 
    {
        string _host;
        int? _port;
        string _idTemplate;
        string _userName;
        string _password;
        bool _secure; 


        public string Host
        {
            get => _host;
            set => _host = value;
        }
        public int? Port
        {
            get => _port;
            set => _port = value;
        }
        public string ClientIdTemplate
        {
            get => _idTemplate;
            set => _idTemplate = value;
        }
        public string UserName
        {
            get => _userName;
            set => _userName = value;
        }
        public string Password
        {
            get => _password;
            set => _password = value;
        }

        public bool IsSecure
        {
            get => _secure;
            set => _secure = value;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}

