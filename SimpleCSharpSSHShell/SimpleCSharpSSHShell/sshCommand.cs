using ManyConsole.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleCSharpSSHShell
{
    class sshCommand : ConsoleCommand
    {
        public sshCommand()
        {
            this.IsCommand("ssh", "Start ssh instance (ssh --hostname=\"8.8.8.8\" --port=22 --user=\"JohnDoe\" --keypath=\"C:\\Keys\\JohnDoesKey.pub\")");

            this.HasOption("hostname=", "Set Hostname e.g. 8.8.8.8", n => login_hostname = n);

            this.HasOption<int>("port=", "The port the ssh should connect to", p => login_port = p);

            this.HasOption("user=", "The ssh login user", u => login_user = u);

            this.HasOption("keypath=", "The path to the ssh keyfile for login", k => login_keypath = k);
        }

        public string login_hostname = "";
        public int login_port = 22;
        public string login_user = "root";
        public string login_keypath = "";

        public override int Run(string[] remainingArguments)
        {
            var mySshShell = new SshClientDuplexStreamHandler();
            
            mySshShell.StartShell(login_hostname, login_port, login_user, login_keypath);

            return 0;
        }
    }
}
