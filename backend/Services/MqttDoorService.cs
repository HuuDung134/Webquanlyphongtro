using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace DoAnCoSo.Services
{
    public interface IMqttDoorService
    {
        Task MoCuaAsync(string maPhong);
        Task DongCuaAsync(string maPhong);
    }

    public class MqttDoorService : IMqttDoorService
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _options;

        public MqttDoorService()
        {
            SetupMqtt();
        }

        private void SetupMqtt()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            // Đổi sang broker.emqx.io cho ổn định
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("broker.emqx.io", 1883)
                .WithClientId("Server_NhaTro_" + Guid.NewGuid())
                .Build();
        }

        private async Task ConnectAsync()
        {
            if (!_mqttClient.IsConnected)
                await _mqttClient.ConnectAsync(_options, CancellationToken.None);
        }

        public async Task MoCuaAsync(string maPhong)
        {
            await ConnectAsync();
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"nhatro/phong{maPhong}/lock")
                .WithPayload("OPEN")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }

        public async Task DongCuaAsync(string maPhong)
        {
            await ConnectAsync();
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"nhatro/phong{maPhong}/lock")
                .WithPayload("CLOSE")
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }
    }
}