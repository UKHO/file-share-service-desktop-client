namespace UKHO.FSSDesktop.Messages.Security
{
    using FSSDesktop.Security;

    public class AuthenticationMessage
    {
        private readonly SecurityContext? _context;

        public AuthenticationMessage(SecurityContext? context)
        {
            _context = context;
        }

        public SecurityContext? Context => _context;
    }
}