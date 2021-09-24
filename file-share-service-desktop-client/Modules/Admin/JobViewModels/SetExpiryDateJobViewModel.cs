using System;
using System.Globalization;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class SetExpiryDateJobViewModel : BaseBatchJobViewModel
    {
        private readonly SetExpiryDateJob job;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job) : base(job)
        {
            this.job = job;
        }
        
        public string BatchId => job.ActionParams.BatchId;
        public string ExpiryDate => job.ActionParams.ExpiryDate;
        protected internal override Task OnExecuteCommand()
        {
            throw new System.NotImplementedException();
        }
        protected override bool CanExecute()
        {
            return Validate();
        }

        private bool Validate()
        {
            if(string.IsNullOrWhiteSpace(BatchId))
            {
                ValidationErrors.Add("Invalid batch id attribute or value.");
            }

            if(string.IsNullOrWhiteSpace(ExpiryDate))
            {
                ValidationErrors.Add("Expiry date is not specified.");
            }
            else
            {
                if(!DateTime.TryParse(ExpiryDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                    out var result))
                {
                    ValidationErrors.Add("Invalid expiry date.");
                }
            }

            return ValidationErrors.Count == 0;
        }
    }
}