using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WorkerService
{
    public class Cliente
    {
        // Cliente UDP
        private static readonly int Port = 3004;
        private static readonly string Ip = "192.168.15.181";
        private static readonly IPAddress serverIp = IPAddress.Parse(Ip);
        private static readonly UdpClient UDP = new(Port);
        private static IPEndPoint Ep = new(serverIp, Port);


        // Protocolo
        private static long bts = 0;
        private static long ind = 0;
        private static string path = "";
        private static Stopwatch stopwatch = new Stopwatch();
        private static Int64 timer = 0;
        private static string msg = "";

        public static void Start()
        {
            // Recebe caminho do arquivo
            Console.WriteLine("Type the path of file zip you want send:");
            path = Console.ReadLine()!;

            // Carrega Arquivo em bytes
            byte[] file = File.ReadAllBytes(path);

            // Set variavel bts com quantidade de bytes do arquivo            
            bts = file.Length;

            // Constroi Mensagem de Start
            string start = $"S;{bts}";
            byte[] Start = Encoding.ASCII.GetBytes(start);

            Console.Clear();
            Console.WriteLine("Awaiting Server...");

            // Enquanto não receber mensagem de resposta com tamanho do arquivo
            while (true)
            {
                // Envia mensagem de start
                UDP.Send(Start, Start.Length, Ep);

                // Recebe resposta do servidor 
                UDP.Client.ReceiveTimeout = 1;
                try
                {
                    byte[] bytes = UDP.Receive(ref Ep);
                    // Converte bytes para string
                    msg = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                }
                catch { }

                // Se obteve Resposta
                if (msg == start)
                {
                    break;
                }
            }

            // Thread para print do progresso
            new Thread(() =>
            {
                string time = "";

                while (ind != bts)
                {
                    Console.Clear();
                    Console.WriteLine($"Sending File...");

                    // Calculo Velocidade de envio
                    if (timer != 0)
                        time = (1 / (timer / (float)10000)).ToString("N2");
                    else
                        time = "Ꝏ";

                    Console.WriteLine($"{time} Mb/s");

                    // Calculo de percentual de envio
                    Console.WriteLine($"{(((float)ind / (float)bts) * 100).ToString("N2")} %");
                    Thread.Sleep(100);
                }

            }).Start();

            // Loop para Enviar byte a byte o arquivo
            for (ind = 0; ind < bts; ind++)
            {
                // Reset tempo do timer
                stopwatch.Reset();
                // Start timer para calculo de velocidade de envio
                stopwatch.Start();

                // Mensagem com sequencia + byte 
                string byt = $"{ind};{file[ind]}";
                byte[] bytt = Encoding.ASCII.GetBytes(byt);

                // Envia Sequencia + byte
                UDP.Send(bytt, bytt.Length, Ep);

                do // Enquanto nao houver Resposta do Servidor
                {
                    // Recebe Retorno do Servidor
                    UDP.Client.ReceiveTimeout = 1;
                    try
                    {
                        byte[] bytes = UDP.Receive(ref Ep);

                        // Converte bytes para string
                        msg = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    }
                    catch { }

                    // Se obteve resposta sai do loop
                    if (msg == byt)
                    {
                        // Stop timer 
                        stopwatch.Stop();
                        // Set timer com tempo decorrido para envio de 1 byte                        
                        timer = stopwatch.ElapsedTicks;

                        break;
                    }

                    // Envia Sequencia + byte
                    UDP.Send(bytt, bytt.Length, Ep);

                } while (true);

            }

            Console.WriteLine("Send Finished!");
        }
    }
}
