using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace SimpleCSharpSSHShell
{
    class SshClientDuplexStreamHandler
    {
        SshClient SshClientInstance = null;
        private EventWaitHandle _channelClosedWaitHandle = new ManualResetEvent(false);
        ShellStream SshClientStream = null;

        // ReSharper disable InconsistentNaming

        // ENABLE_WINDOW_INPUT

        private const uint ENABLE_ECHO_INPUT = 0x0004;

        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        private const uint ENABLE_INSERT_MODE = 0x0020;

        private const uint ENABLE_LINE_INPUT = 0x0002;

        private const uint ENABLE_MOUSE_INPUT = 0x0010;

        private const uint ENABLE_PROCESSED_INPUT = 0x0001;

        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

        private const uint ENABLE_WINDOW_INPUT = 0x0008;

        private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

        // Handles

        private const int STD_INPUT_HANDLE = -10;

        private const int STD_OUTPUT_HANDLE = -11;

        // ENABLE_WINDOW_OUTPUT

        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        

        // ReSharper restore InconsistentNaming

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        /// <summary>
        /// Constructor
        /// </summary>
        public SshClientDuplexStreamHandler()
        {

        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SshClientDuplexStreamHandler()
        {

        }

        private void SendAllSshConnectionOutput()
        {
            try
            {
                var buffer = new byte[512];

                while (SshClientInstance.IsConnected)
                {
                    try
                    {
                        var nextContent = Console.ReadKey(true);
                        if (nextContent.Key == ConsoleKey.Tab)
                        {
                            /* TODO */
                            SshClientStream.Write(nextContent.KeyChar);
                        }
                        else if(nextContent.Key == ConsoleKey.Enter)
                        {
                            SshClientStream.Write(nextContent.KeyChar);
                            //SshClientStream.Write('\r');
                        }
                        else
                        {
                            SshClientStream.Write(nextContent.KeyChar);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SSH Sender:{ex}");
            }
            finally
            {
                /* Nothing to do */
            }
        }

        private void GetAllSshConnectionInput()
        {
            try
            {
                var buffer = new byte[512];

                while (SshClientInstance.IsConnected)
                {
                    
                    if (SshClientStream.DataAvailable)
                    {
                        var newContent = SshClientStream.Read();
                        Console.Write(newContent);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SSH Listener:{ex}");
            }
            finally
            {
                /* Nothing to do */
            }
        }

        private int LastShellWidht = 0;
        private int LastShellHeight = 0;

        public void StartShell(string login_host, int login_ip, string login_user, string login_key)
        {
            var iStdIn = GetStdHandle(STD_INPUT_HANDLE);
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

            if (!GetConsoleMode(iStdIn, out uint inConsoleMode))
            {
                Console.WriteLine("failed to get input console mode");
                Console.ReadKey();
                return;
            }
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                Console.WriteLine("failed to get output console mode");
                Console.ReadKey();
                return;
            }

            inConsoleMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
            inConsoleMode &= ~ENABLE_ECHO_INPUT;
            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            outConsoleMode |= DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode(iStdIn, inConsoleMode))
            {
                Console.WriteLine($"failed to set input console mode, error code: {GetLastError()}");
                Console.ReadKey();
                return;
            }
            if (!SetConsoleMode(iStdOut, outConsoleMode))
            {
                Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                Console.ReadKey();
                return;
            }

            Console.TreatControlCAsInput = true;

            /*
            Console.WriteLine("\u001b[31mHelloWorld\u001b[0m");
            Console.Write("test");
            Console.Write(new[] { (char)0x1b, '[', '1', 'B' });
            Console.Write("test");

            Console.ReadKey();
            */

            var myPrivateKeyFile = new PrivateKeyFile(login_key);

            var connectionInfo = new ConnectionInfo(login_host, login_ip, login_user,
                                        new PrivateKeyAuthenticationMethod(login_user, myPrivateKeyFile)
                                        );

            this.SshClientInstance = new SshClient(connectionInfo);
            
            try
            {
                SshClientInstance.Connect();
                Console.WriteLine($"Server Version: {SshClientInstance.ConnectionInfo.ServerVersion}");
                Console.WriteLine($"Proxy username: {SshClientInstance.ConnectionInfo.ProxyUsername}");

                Console.Title = SshClientInstance.ConnectionInfo.Host + " - " + SshClientInstance.ConnectionInfo.Username;

                SshClientStream = SshClientInstance.CreateShellStream("xterm", Convert.ToUInt32(Console.WindowWidth), Convert.ToUInt32(Console.WindowHeight), Convert.ToUInt32(Console.WindowWidth), Convert.ToUInt32(Console.WindowHeight), 20000);
                LastShellWidht = Console.WindowWidth;
                LastShellHeight = Console.WindowHeight;


                //SshClientStream.WriteLine(" \n");

                //SshClientStream.WriteLine("msgcat --color=test");
                //SshClientStream.WriteLine("source .bashrc");
                //SshClientStream.WriteLine("export TERM=xterm");

                var myListenerThread = new Thread(GetAllSshConnectionInput);
                myListenerThread.Start();

                var myTalkerThread = new Thread(SendAllSshConnectionOutput);
                myTalkerThread.Start();

                while (SshClientInstance.IsConnected)/* Not working yet */
                {
                    /* This is used to close ssh console on logout */
                    try
                    {
                        /* Try to flush. It throws an exception if connection is disposed (e.g. due to logout) */
                        SshClientStream.Flush();

                        if ((LastShellWidht != Console.WindowWidth) || (LastShellHeight != Console.WindowHeight))
                        {
                            SshClientStream.SendWindowChangeRequest(Convert.ToUInt32(Console.WindowWidth), Convert.ToUInt32(Console.WindowHeight), Convert.ToUInt32(Console.WindowWidth), Convert.ToUInt32(Console.WindowHeight));
                            LastShellWidht = Console.WindowWidth;
                            LastShellHeight = Console.WindowHeight;
                        }
                    }
                    catch
                    {
                        Environment.Exit(Environment.ExitCode);
                    }
                    /* End */

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Issue with ssh connection: {ex}");
            }
        }
    }
}
