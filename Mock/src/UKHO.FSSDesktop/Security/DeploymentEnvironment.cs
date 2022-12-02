namespace UKHO.FSSDesktop.Security
{
    using System;

    public class DeploymentEnvironment
    {
        private readonly string _name;
        private readonly Uri _baseEndpoint;
        private readonly string _description;

        public DeploymentEnvironment(string name, Uri baseEndpoint, string description)
        {
            _name = name;
            _baseEndpoint = baseEndpoint;
            _description = description;
        }

        public string Name => _name;

        public Uri BaseEndpoint => _baseEndpoint;

        public string Description => _description;
    }
}