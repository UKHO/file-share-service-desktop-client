namespace UKHO.FSSDesktop.Security
{
    public class SecurityClaim
    {
        private readonly string _name;

        public SecurityClaim(string name)
        {
            _name = name;
        }

        public string Name => _name;
    }
}