namespace DoAnCoSo.Configurations
{
    public class MqttSettings
    {
        public string Host { get; set; } = "broker.emqx.io";
        public int Port { get; set; } = 1883;
        public string ClientIdPrefix { get; set; } = "Server_NhaTro_";
        public string TopicPrefix { get; set; } = "nhatro";
    }
}


