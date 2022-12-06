namespace UKHO.FSSDesktop.Modules.FSS.Search.Models
{
    using System;
    using System.Linq;
    using UKHO.FileShareClient.Models;
    using UKHO.FSSDesktop.Security;

    public class BatchDetailModel
    {
        private readonly BatchDetails _details;
        private readonly SecurityContext _securityContext;

        public BatchDetailModel(BatchDetails details, SecurityContext securityContext)
        {
            _details = details;
            _securityContext = securityContext;
        }

        public BatchDetails Details => _details;

        public string Type
        {
            get
            {
                var type = _details.Attributes.SingleOrDefault(a => a.Key.Equals("product type", StringComparison.InvariantCultureIgnoreCase));

                if (type == null)
                {
                    return "";
                }

                return type.Value;
            }
        }

        public bool CanExpire => _securityContext.Claims.Any(x => x.Name.Equals(WellKnownClaim.ExpireBatches));
    }
}