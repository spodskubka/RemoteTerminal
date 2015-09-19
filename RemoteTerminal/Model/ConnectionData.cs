namespace RemoteTerminal.Model
{
    public class ConnectionData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ConnectionType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public AuthenticationType Authentication { get; set; }
        public string PrivateKeyName { get; set; }
        public bool PrivateKeyAgentForwarding { get; set; }
        public char ImageChar
        {
            get
            {
                return (char)(this.Type == ConnectionType.Ssh ? 0xE131 : 0xE130);
            }
        }

        public string HostAndPort
        {
            get
            {
                return this.Host + ":" + this.Port.ToString();
            }
        }
    }
}
