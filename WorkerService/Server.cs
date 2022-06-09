using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WorkerService
{
    public class Server
    {
        // Server UDP
        private static readonly int Port = 3004;
        private static UdpClient UDP = new(Port);
        private static IPEndPoint Ep = new(IPAddress.Any, Port);

        // Protocolo
        private static long bts = 0;
        private static long ind = 0;
        private static Stopwatch stopwatch = new Stopwatch();
        private static Int64 timer = 0;
        private static string msg = "";
        public static void Start()
        {
            Console.Clear();
            Console.WriteLine("Awaiting File...");

            do // Enquanto não receber mensagem com tamanho do arquivo e começar a transmissão
            {
                // Recebe mensagem do cliente
                byte[] bytes = UDP.Receive(ref Ep);

                //Converte bytes para string
                msg = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                // Se mensagem for de Start
                if (msg.Split(";")[0] == "S")
                {
                    bts = Convert.ToInt64(msg.Split(";")[1]);

                    // Envia mensagem solicitando inicio da transmissao dos bytes
                    byte[] sendbuf = Encoding.ASCII.GetBytes(msg);
                    UDP.Send(sendbuf, sendbuf.Length, Ep);
                }

            } while (bts == 0 || msg.Split(";")[0] == "S");

            // Instancia array do arquivo
            byte[] data = new byte[bts];

            // Thread para print do progresso
            new Thread(() =>
            {
                string time = "";

                while (ind != bts)
                {
                    Console.Clear();
                    Console.WriteLine($"Downloading File...");

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

            // Loop para receber byte a byte do arquivo
            for (ind = 0; ind < bts; ind++)
            {
                // Reset tempo do timer
                stopwatch.Reset();
                // Start timer para calculo de velocidade de download
                stopwatch.Start();

                do // Enquanto nao receber sequencia do byte igual ao indice 
                {
                    // Recebe mensagem
                    UDP.Client.ReceiveTimeout = 1;
                    try
                    {
                        byte[] bytes = UDP.Receive(ref Ep);

                        // Converte bytes para string
                        msg = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    }
                    catch { }

                    // Se sequencia do byte igual ao indice
                    if (Int32.Parse(msg.Split(";")[0]) == ind)
                    {
                        // Stop timer 
                        stopwatch.Stop();
                        // Set timer com tempo decorrido para download de 1 byte                        
                        timer = stopwatch.ElapsedTicks;

                        // Envia mensagem solicitando proximo byte
                        byte[] sendbuf = Encoding.ASCII.GetBytes(msg);
                        UDP.Send(sendbuf, sendbuf.Length, Ep);

                        data[ind] = (byte)Int32.Parse(msg.Split(";")[1]);

                        break;
                    }

                    // Envia mensagem solicitando proximo byte
                    byte[] sendbuf1 = Encoding.ASCII.GetBytes(msg);
                    UDP.Send(sendbuf1, sendbuf1.Length, Ep);

                } while (true);

            }

            File.WriteAllBytes(@"C:\Users\bruno.vechi\Desktop\Download.zip", data);

            Console.WriteLine("Download Finished!");
        }
    }
}
