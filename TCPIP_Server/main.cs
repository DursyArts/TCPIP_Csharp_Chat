using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyApp
{
    internal class Program
    {
        static async Task<IPAddress> ResolveLocalhostIP(){ // async Task<IPAddress> da für asynchrone Methoden Task<T> oder ein ähnlicher Typ als Rückgabe Wert verwendet werden muss
            string hostname = Dns.GetHostName();
            IPHostEntry localhost = await Dns.GetHostEntryAsync(hostname);
            IPAddress[] localIpAddresses = localhost.AddressList;
            System.Console.WriteLine("Following Local Network Interfaces were detected:");

            int tmp_counter = 1;
            string[] options = new string[localIpAddresses.Length];

            foreach(IPAddress localIP in localIpAddresses){
                System.Console.WriteLine($"{tmp_counter}. > {localIP.ToString()}");
                options[tmp_counter-1] = tmp_counter.ToString();
                tmp_counter++;
            }

            string option_userInput = WaitForValidInput("Choose a Network Interface", options);

            return localIpAddresses[int.Parse(option_userInput)-1];
        }

        static string WaitForValidInput(string message, string[] options){
            string tmp_answer;

            do{
                System.Console.Write($"{message} <");
                if(options.Length > 0){
                    System.Console.Write(string.Join("/", options));
                    System.Console.Write(">: ");
                }
                
                tmp_answer = System.Console.ReadLine() ?? "n"; // ?? Null-Koaleszent-Operator: welchen Wert soll tmp_answer (string) haben wenn System.Console.ReadLine() NULL zurück gibt->string.Empty? Datentyp string nicht NULLbar
            }while(!options.Contains(tmp_answer));

            return tmp_answer;
        }

        static async Task Main(string[] args)
        {
            System.Console.WriteLine("TCP/IP Server");
            
            /*
            Bind IP und Bind Port einlesen 
            */
            System.Console.WriteLine("Resolving Localhost IP... ");
            IPAddress localIP = await ResolveLocalhostIP();
            System.Console.WriteLine($"Using {localIP} as bind address");

            int localPort;
            do{
                System.Console.Write("Choose a Port to bind this Service to (1024-65535): ");
                localPort = int.Parse(System.Console.ReadLine() ?? "1337");
            }while(localPort < 1024 && localPort > 65535);

            /*
            IP Endpoint erstellen
            */
            System.Console.WriteLine($"Creating Endpoint under {localIP}:{localPort}");

            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.None, 0);
            
            while(ipEndPoint.Address == IPAddress.None){
                try{
                    ipEndPoint = new(localIP, localPort);
                }catch(ArgumentNullException ex){
                    System.Console.WriteLine($"Error: Check input values. Message: {ex.Message}");
                }catch(FormatException ex){
                    System.Console.WriteLine($"Error: Bind IP Address is not valid or in wrong format. Message: {ex.Message}");
                }catch(SocketException ex){
                    System.Console.WriteLine($"Error: A socket error occurred. Message: {ex.Message}");
                }catch(Exception ex){
                    System.Console.WriteLine($"Error: Unexpected error occurred. Message: {ex.Message}");
                }
            }

            System.Console.WriteLine($"Endpoint Created.");

            /*
            Socket listener erstellen
            */
            using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(ipEndPoint);
            listener.Listen(ipEndPoint.Port);

            var handler = await listener.AcceptAsync();

            while(true){
                byte[] buffer = new byte[1024];
                int received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                string response = Encoding.UTF8.GetString(buffer, 0, received);

                var eom = "<|EOM|>";
                if(response.IndexOf(eom) > -1){
                    System.Console.WriteLine($"Message received on Socket: \"{response.Replace(eom, "")}\"");
                }
            }
        }
    }
}